using System.Globalization;
using System.Xml.Linq;
using XbrlProcessor.Models.Entities;
using XbrlProcessor.Models.Collections;
using XbrlProcessor.Configuration;
using XbrlProcessor.Builders;

namespace XbrlProcessor.Services;

/// <summary>
/// Сервис для парсинга XBRL файлов
/// </summary>
/// <param name="settings">Настройки приложения</param>
public class XbrlParser(XbrlSettings settings)
{
    #region Fields

    private readonly XNamespace _xbrli = settings.XmlNamespaces.Xbrli;
    private readonly XNamespace _xbrldi = settings.XmlNamespaces.Xbrldi;
    private readonly XNamespace _dimInt = settings.XmlNamespaces.DimInt;

    #endregion

    #region Public Methods

    /// <summary>
    /// Парсит XBRL файл и возвращает объект Instance
    /// </summary>
    /// <param name="filePath">Путь к XBRL файлу</param>
    /// <returns>Объект Instance с данными из файла</returns>
    /// <exception cref="XbrlParseException">Ошибка парсинга структуры XBRL</exception>
    public Instance ParseXbrlFile(string filePath)
    {
        var doc = XDocument.Load(filePath);

        if (doc.Root is null)
            throw new XbrlParseException(filePath, "Document has no root element.");

        var builder = new InstanceBuilder();

        builder.AddContexts(doc.Descendants(_xbrli + "context")
            .Select(e => ParseContext(e, filePath)));

        builder.AddUnits(doc.Descendants(_xbrli + "unit")
            .Select(e => ParseUnit(e, filePath)));

        builder.AddFacts(doc.Root.Elements()
            .Where(e => e.Name.Namespace != _xbrli &&
                       e.Name.LocalName != "schemaRef" &&
                       e.Attribute("contextRef") != null)
            .Select(ParseFact));

        return builder.Build();
    }

    #endregion

    #region Private Methods

    private Context ParseContext(XElement element, string filePath)
    {
        var contextId = element.Attribute("id")?.Value;
        if (string.IsNullOrEmpty(contextId))
            throw new XbrlParseException(filePath, "Context element is missing required 'id' attribute.");

        var context = new Context { Id = contextId };

        // Entity
        var entity = element.Element(_xbrli + "entity");
        if (entity != null)
        {
            var identifier = entity.Element(_xbrli + "identifier");
            context.EntityValue = identifier?.Value;
            context.EntityScheme = identifier?.Attribute("scheme")?.Value;
            context.EntitySegment = entity.Element(_xbrli + "segment")?.ToString();
        }

        // Period
        var period = element.Element(_xbrli + "period");
        if (period != null)
        {
            var instant = period.Element(_xbrli + "instant");
            if (instant != null)
            {
                context.PeriodInstant = ParseDate(instant.Value, filePath,
                    $"Context '{contextId}', element 'instant'");
            }

            var startDate = period.Element(_xbrli + "startDate");
            if (startDate != null)
            {
                context.PeriodStartDate = ParseDate(startDate.Value, filePath,
                    $"Context '{contextId}', element 'startDate'");
            }

            var endDate = period.Element(_xbrli + "endDate");
            if (endDate != null)
            {
                context.PeriodEndDate = ParseDate(endDate.Value, filePath,
                    $"Context '{contextId}', element 'endDate'");
            }

            var forever = period.Element(_xbrli + "forever");
            if (forever != null)
            {
                context.PeriodForever = true;
            }
        }

        // Scenario
        var scenario = element.Element(_xbrli + "scenario");
        if (scenario != null)
        {
            foreach (var member in scenario.Elements())
            {
                context.Scenarios.Add(ParseScenario(member));
            }
        }

        return context;
    }

    private static Scenario ParseScenario(XElement element)
    {
        var dimensionType = DimensionTypeExtensions.FromXmlName(element.Name.LocalName);
        var dimensionName = element.Attribute("dimension")?.Value;
        string? dimensionCode = null;
        string? dimensionValue = null;

        if (dimensionType == DimensionType.ExplicitMember)
        {
            dimensionValue = element.Value;
        }
        else if (dimensionType == DimensionType.TypedMember)
        {
            var child = element.Elements().FirstOrDefault();
            if (child != null)
            {
                dimensionCode = child.Name.LocalName;
                dimensionValue = child.Value;
            }
        }

        return new Scenario
        {
            DimensionType = dimensionType,
            DimensionName = dimensionName,
            DimensionCode = dimensionCode,
            DimensionValue = dimensionValue
        };
    }

    private Unit ParseUnit(XElement element, string filePath)
    {
        var id = element.Attribute("id")?.Value;
        if (string.IsNullOrEmpty(id))
            throw new XbrlParseException(filePath, "Unit element is missing required 'id' attribute.");

        string? measure = null;
        string? numerator = null;
        string? denominator = null;

        var measureElement = element.Element(_xbrli + "measure");
        if (measureElement != null)
        {
            measure = measureElement.Value;
        }

        var divide = element.Element(_xbrli + "divide");
        if (divide != null)
        {
            numerator = divide.Element(_xbrli + "unitNumerator")?.Element(_xbrli + "measure")?.Value;
            denominator = divide.Element(_xbrli + "unitDenominator")?.Element(_xbrli + "measure")?.Value;
        }

        return new Unit
        {
            Id = id,
            Measure = measure,
            Numerator = numerator,
            Denominator = denominator
        };
    }

    private static Fact ParseFact(XElement element)
    {
        var prefix = element.GetPrefixOfNamespace(element.Name.Namespace);
        var conceptName = string.IsNullOrEmpty(prefix)
            ? element.Name.LocalName
            : $"{prefix}:{element.Name.LocalName}";

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
        {
            fact.Decimals = d;
        }

        var precision = element.Attribute("precision")?.Value;
        if (precision != null && int.TryParse(precision, out int p))
        {
            fact.Precision = p;
        }

        return fact;
    }

    private static DateTime ParseDate(string value, string filePath, string location)
    {
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
            return result;

        throw new XbrlParseException(filePath,
            $"Invalid date value '{value}' at {location}.");
    }

    #endregion
}
