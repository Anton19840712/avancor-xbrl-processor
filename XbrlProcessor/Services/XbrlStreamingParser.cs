using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using XbrlProcessor.Models.Entities;
using XbrlProcessor.Configuration;
using XbrlProcessor.Builders;

namespace XbrlProcessor.Services;

/// <summary>
/// Потоковый парсер XBRL на базе XmlReader — не загружает весь документ в память.
/// Для каждого элемента (context, unit, fact) считывает поддерево через XElement.ReadFrom,
/// что позволяет переиспользовать логику парсинга из XbrlParser.
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

        var builder = new InstanceBuilder();

        using var reader = XmlReader.Create(filePath, new XmlReaderSettings
        {
            IgnoreWhitespace = true,
            IgnoreComments = true
        });

        // XNode.ReadFrom продвигает reader за прочитанный элемент,
        // поэтому после него нельзя вызывать reader.Read() — иначе пропустим следующий элемент.
        // Флаг consumed отслеживает, был ли текущий узел уже обработан через ReadFrom.
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
                        builder.AddContext(ParseContext(reader, filePath));
                        consumed = true;
                        continue;
                    case "unit":
                        builder.AddUnit(ParseUnit(reader, filePath));
                        consumed = true;
                        continue;
                }
            }

            // Факт: не из xbrli namespace, не schemaRef, имеет contextRef
            if (reader.NamespaceURI != _xbrliNs
                && reader.LocalName != "schemaRef"
                && reader.GetAttribute("contextRef") != null)
            {
                builder.AddFact(ParseFact(reader));
                consumed = true;
            }
        }

        return builder.Build();
    }

    private Context ParseContext(XmlReader reader, string filePath)
    {
        // Считываем поддерево в XElement для удобного парсинга вложенных элементов
        var element = (XElement)XNode.ReadFrom(reader);
        XNamespace xbrli = _xbrliNs;
        XNamespace xbrldi = _xbrldiNs;

        var contextId = element.Attribute("id")?.Value;
        if (string.IsNullOrEmpty(contextId))
            throw new XbrlParseException(filePath, "Context element is missing required 'id' attribute.");

        var context = new Context { Id = contextId };

        var entity = element.Element(xbrli + "entity");
        if (entity != null)
        {
            var identifier = entity.Element(xbrli + "identifier");
            context.EntityValue = identifier?.Value;
            context.EntityScheme = identifier?.Attribute("scheme")?.Value;
            context.EntitySegment = entity.Element(xbrli + "segment")?.ToString();
        }

        var period = element.Element(xbrli + "period");
        if (period != null)
        {
            var instant = period.Element(xbrli + "instant");
            if (instant != null)
                context.PeriodInstant = ParseDate(instant.Value, filePath, $"Context '{contextId}', element 'instant'");

            var startDate = period.Element(xbrli + "startDate");
            if (startDate != null)
                context.PeriodStartDate = ParseDate(startDate.Value, filePath, $"Context '{contextId}', element 'startDate'");

            var endDate = period.Element(xbrli + "endDate");
            if (endDate != null)
                context.PeriodEndDate = ParseDate(endDate.Value, filePath, $"Context '{contextId}', element 'endDate'");

            if (period.Element(xbrli + "forever") != null)
                context.PeriodForever = true;
        }

        var scenario = element.Element(xbrli + "scenario");
        if (scenario != null)
        {
            foreach (var member in scenario.Elements())
            {
                var dimensionType = DimensionTypeExtensions.FromXmlName(member.Name.LocalName);
                var dimensionName = member.Attribute("dimension")?.Value;
                string? dimensionCode = null;
                string? dimensionValue = null;

                if (dimensionType == DimensionType.ExplicitMember)
                {
                    dimensionValue = member.Value;
                }
                else if (dimensionType == DimensionType.TypedMember)
                {
                    var child = member.Elements().FirstOrDefault();
                    if (child != null)
                    {
                        dimensionCode = child.Name.LocalName;
                        dimensionValue = child.Value;
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
        }

        return context;
    }

    private Unit ParseUnit(XmlReader reader, string filePath)
    {
        var element = (XElement)XNode.ReadFrom(reader);
        XNamespace xbrli = _xbrliNs;

        var id = element.Attribute("id")?.Value;
        if (string.IsNullOrEmpty(id))
            throw new XbrlParseException(filePath, "Unit element is missing required 'id' attribute.");

        string? measure = null;
        string? numerator = null;
        string? denominator = null;

        var measureElement = element.Element(xbrli + "measure");
        if (measureElement != null)
            measure = measureElement.Value;

        var divide = element.Element(xbrli + "divide");
        if (divide != null)
        {
            numerator = divide.Element(xbrli + "unitNumerator")?.Element(xbrli + "measure")?.Value;
            denominator = divide.Element(xbrli + "unitDenominator")?.Element(xbrli + "measure")?.Value;
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
        // Захватываем prefix из XmlReader до ReadFrom, т.к. XElement
        // из ReadFrom не наследует namespace declarations корневого элемента
        var readerPrefix = reader.Prefix;
        var localName = reader.LocalName;

        var element = (XElement)XNode.ReadFrom(reader);

        var prefix = !string.IsNullOrEmpty(readerPrefix)
            ? readerPrefix
            : element.GetPrefixOfNamespace(element.Name.Namespace);
        var conceptName = string.IsNullOrEmpty(prefix)
            ? localName
            : $"{prefix}:{localName}";

        var fact = new Fact
        {
            Id = element.Attribute("id")?.Value ?? element.Name.LocalName,
            ConceptName = conceptName,
            ContextRef = element.Attribute("contextRef")?.Value,
            UnitRef = element.Attribute("unitRef")?.Value,
            Value = XbrlValue.Parse(element.Value)
        };

        var decimals = element.Attribute("decimals")?.Value;
        if (decimals != null && int.TryParse(decimals, out int d))
            fact.Decimals = d;

        var precision = element.Attribute("precision")?.Value;
        if (precision != null && int.TryParse(precision, out int p))
            fact.Precision = p;

        return fact;
    }

    private static DateTime ParseDate(string value, string filePath, string location)
    {
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
            return result;

        throw new XbrlParseException(filePath, $"Invalid date value '{value}' at {location}.");
    }
}
