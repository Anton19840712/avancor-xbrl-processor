using System.Globalization;
using System.Xml;
using XbrlProcessor.Models.Entities;
using XbrlProcessor.Configuration;

namespace XbrlProcessor.Services;

/// <summary>
/// Потоковый парсер XBRL — читает атрибуты и значения напрямую из XmlReader,
/// без промежуточных XElement-аллокаций (кроме segment, который сохраняется как XML-строка через ReadOuterXml).
/// </summary>
/// <param name="settings">Настройки приложения</param>
public class XbrlStreamingParser(XbrlSettings settings)
{
    private readonly string _xbrliNs = settings.XmlNamespaces.Xbrli;
    private readonly string _xbrldiNs = settings.XmlNamespaces.Xbrldi;

    /// <summary>
    /// Парсит XBRL файл потоково через XmlReader
    /// </summary>
    public Instance ParseXbrlFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}", filePath);

        var instance = new Instance();

        using var reader = XmlReader.Create(filePath, new XmlReaderSettings
        {
            IgnoreWhitespace = true,
            IgnoreComments = true
        });

        // consumed отслеживает, был ли текущий узел уже обработан через ReadElementContentAsString/ReadSubtree,
        // которые продвигают reader за прочитанный элемент.
        var consumed = false;

        while (consumed || reader.Read())
        {
            consumed = false;

            if (reader.NodeType != XmlNodeType.Element)
                continue;

            if (reader.NamespaceURI == _xbrliNs)
            {
                switch (reader.LocalName)
                {
                    case "context":
                        var ctxEmpty = reader.IsEmptyElement;
                        instance.Contexts.Add(ParseContext(reader, filePath));
                        consumed = !ctxEmpty;
                        continue;
                    case "unit":
                        var unitEmpty = reader.IsEmptyElement;
                        instance.Units.Add(ParseUnit(reader, filePath));
                        consumed = !unitEmpty;
                        continue;
                }
            }

            // Факт: не из xbrli namespace, не schemaRef, имеет contextRef
            if (reader.NamespaceURI != _xbrliNs
                && reader.LocalName != "schemaRef"
                && reader.GetAttribute("contextRef") != null)
            {
                instance.Facts.Add(ParseFact(reader));
                consumed = true;
            }
        }

        return instance;
    }

    private Context ParseContext(XmlReader reader, string filePath)
    {
        var contextId = reader.GetAttribute("id");
        if (string.IsNullOrEmpty(contextId))
            throw new XbrlParseException(filePath, "Context element is missing required 'id' attribute.");

        var context = new Context { Id = contextId };

        if (reader.IsEmptyElement)
            return context;

        // ReadSubtree создаёт обёртку над тем же XmlReader, ограниченную текущим поддеревом.
        // После Dispose оригинальный reader стоит на EndElement контекста.
        using (var sub = reader.ReadSubtree())
        {
            sub.Read(); // встать на <context>
            var subConsumed = false;

            while (subConsumed || sub.Read())
            {
                subConsumed = false;

                if (sub.NodeType != XmlNodeType.Element)
                    continue;

                if (sub.NamespaceURI == _xbrliNs)
                {
                    switch (sub.LocalName)
                    {
                        case "identifier":
                            context.EntityScheme = Intern(sub.GetAttribute("scheme"));
                            if (!sub.IsEmptyElement)
                            {
                                context.EntityValue = sub.ReadElementContentAsString();
                                subConsumed = true;
                            }
                            break;
                        case "segment":
                            context.EntitySegment = sub.ReadOuterXml();
                            subConsumed = true;
                            break;
                        case "instant":
                            context.PeriodInstant = ParseDate(sub.ReadElementContentAsString(), filePath,
                                $"Context '{contextId}', element 'instant'");
                            subConsumed = true;
                            break;
                        case "startDate":
                            context.PeriodStartDate = ParseDate(sub.ReadElementContentAsString(), filePath,
                                $"Context '{contextId}', element 'startDate'");
                            subConsumed = true;
                            break;
                        case "endDate":
                            context.PeriodEndDate = ParseDate(sub.ReadElementContentAsString(), filePath,
                                $"Context '{contextId}', element 'endDate'");
                            subConsumed = true;
                            break;
                        case "forever":
                            context.PeriodForever = true;
                            break;
                    }
                }
                else if (sub.NamespaceURI == _xbrldiNs)
                {
                    ReadScenarioMember(sub, context);
                    subConsumed = true;
                }
            }
        }

        return context;
    }

    private static void ReadScenarioMember(XmlReader reader, Context context)
    {
        var dimensionType = DimensionTypeExtensions.FromXmlName(reader.LocalName);
        var dimensionName = Intern(reader.GetAttribute("dimension"));
        string? dimensionCode = null;
        string? dimensionValue = null;

        if (dimensionType == DimensionType.ExplicitMember)
        {
            if (!reader.IsEmptyElement)
                dimensionValue = reader.ReadElementContentAsString();
        }
        else if (dimensionType == DimensionType.TypedMember && !reader.IsEmptyElement)
        {
            // Читаем первый дочерний элемент typedMember
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    dimensionCode = reader.LocalName;
                    if (!reader.IsEmptyElement)
                        dimensionValue = reader.ReadElementContentAsString();
                    break;
                }
                if (reader.NodeType == XmlNodeType.EndElement)
                    break;
            }
        }

        context.Scenarios.Add(new Scenario
        {
            DimensionType = dimensionType,
            DimensionName = dimensionName,
            DimensionCode = dimensionCode,
            DimensionValue = dimensionValue
        });
    }

    private Unit ParseUnit(XmlReader reader, string filePath)
    {
        var id = reader.GetAttribute("id");
        if (string.IsNullOrEmpty(id))
            throw new XbrlParseException(filePath, "Unit element is missing required 'id' attribute.");

        string? measure = null;
        string? numerator = null;
        string? denominator = null;

        if (!reader.IsEmptyElement)
        {
            using (var sub = reader.ReadSubtree())
            {
                sub.Read(); // встать на <unit>

                // section: 0 = top level, 1 = unitNumerator, 2 = unitDenominator
                var section = 0;

                while (sub.Read())
                {
                    if (sub.NodeType != XmlNodeType.Element || sub.NamespaceURI != _xbrliNs)
                        continue;

                    switch (sub.LocalName)
                    {
                        case "unitNumerator":
                            section = 1;
                            break;
                        case "unitDenominator":
                            section = 2;
                            break;
                        case "measure":
                            var val = sub.ReadElementContentAsString();
                            switch (section)
                            {
                                case 1: numerator = val; break;
                                case 2: denominator = val; break;
                                default: measure = val; break;
                            }
                            break;
                    }
                }
            }
        }

        return new Unit
        {
            Id = id,
            Measure = measure,
            Numerator = numerator,
            Denominator = denominator
        };
    }

    private static Fact ParseFact(XmlReader reader)
    {
        var prefix = reader.Prefix;
        var localName = reader.LocalName;
        var conceptName = string.IsNullOrEmpty(prefix)
            ? localName
            : $"{prefix}:{localName}";

        var id = reader.GetAttribute("id") ?? localName;
        var contextRef = reader.GetAttribute("contextRef");
        var unitRef = reader.GetAttribute("unitRef");
        var decimalsStr = reader.GetAttribute("decimals");
        var precisionStr = reader.GetAttribute("precision");

        // ReadElementContentAsString работает и для пустых элементов (<tag/>),
        // возвращая "" и продвигая reader за элемент.
        var value = reader.ReadElementContentAsString();

        var fact = new Fact
        {
            Id = id,
            ConceptName = Intern(conceptName),
            ContextRef = contextRef,
            UnitRef = Intern(unitRef),
            Value = XbrlValue.Parse(value)
        };

        if (decimalsStr != null && int.TryParse(decimalsStr, out int d))
            fact.Decimals = d;
        if (precisionStr != null && int.TryParse(precisionStr, out int p))
            fact.Precision = p;

        return fact;
    }

    private static string? Intern(string? value)
        => value is null ? null : string.Intern(value);

    private static DateTime ParseDate(string value, string filePath, string location)
    {
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
            return result;

        throw new XbrlParseException(filePath, $"Invalid date value '{value}' at {location}.");
    }
}
