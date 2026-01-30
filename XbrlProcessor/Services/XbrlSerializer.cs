using System.Xml.Linq;
using XbrlProcessor.Models.Entities;
using XbrlProcessor.Configuration;

namespace XbrlProcessor.Services;

/// <summary>
/// Сервис для сериализации доменных объектов в XBRL XML
/// </summary>
/// <param name="settings">Настройки приложения</param>
public class XbrlSerializer(XbrlSettings settings)
{
    #region Fields

    private readonly XNamespace _xbrli = settings.XmlNamespaces.Xbrli;
    private readonly XNamespace _xbrldi = settings.XmlNamespaces.Xbrldi;
    private readonly XNamespace _link = settings.XmlNamespaces.Link;
    private readonly XNamespace _dimInt = settings.XmlNamespaces.DimInt;
    private readonly XNamespace _purcbDic = settings.XmlNamespaces.PurcbDic;

    #endregion

    #region Public Methods

    /// <summary>
    /// Сохраняет объект Instance в XBRL файл
    /// </summary>
    /// <param name="instance">Объект Instance для сохранения</param>
    /// <param name="filePath">Путь для сохранения файла</param>
    /// <param name="templatePath">Путь к шаблону XBRL файла</param>
    public void SaveToXbrl(Instance instance, string filePath, string templatePath)
    {
        var doc = XDocument.Load(templatePath);
        var root = doc.Root!;

        // Очищаем старые элементы
        root.Elements(_xbrli + "context").Remove();
        root.Elements(_xbrli + "unit").Remove();
        root.Elements().Where(e => e.Name.Namespace != _xbrli &&
                                   e.Name.Namespace != _link &&
                                   e.Attribute("contextRef") != null).Remove();

        foreach (var context in instance.Contexts)
            root.Add(CreateContextElement(context));

        foreach (var unit in instance.Units)
            root.Add(CreateUnitElement(unit));

        foreach (var fact in instance.Facts)
            root.Add(CreateFactElement(fact));

        doc.Save(filePath);
    }

    #endregion

    #region Private Methods

    private XElement CreateContextElement(Context context)
    {
        var element = new XElement(_xbrli + "context",
            new XAttribute("id", context.Id ?? ""));

        var entityElement = new XElement(_xbrli + "entity",
            new XElement(_xbrli + "identifier",
                new XAttribute("scheme", context.EntityScheme ?? ""),
                context.EntityValue ?? ""));

        element.Add(entityElement);

        var periodElement = new XElement(_xbrli + "period");
        if (context.PeriodInstant.HasValue)
        {
            periodElement.Add(new XElement(_xbrli + "instant",
                context.PeriodInstant.Value.ToString(settings.DateFormat)));
        }
        else if (context.PeriodStartDate.HasValue && context.PeriodEndDate.HasValue)
        {
            periodElement.Add(new XElement(_xbrli + "startDate",
                context.PeriodStartDate.Value.ToString(settings.DateFormat)));
            periodElement.Add(new XElement(_xbrli + "endDate",
                context.PeriodEndDate.Value.ToString(settings.DateFormat)));
        }
        else if (context.PeriodForever)
        {
            periodElement.Add(new XElement(_xbrli + "forever"));
        }

        element.Add(periodElement);

        if (context.Scenarios.Count > 0)
        {
            var scenarioElement = new XElement(_xbrli + "scenario");
            foreach (var scenario in context.Scenarios)
                scenarioElement.Add(CreateScenarioElement(scenario));
            element.Add(scenarioElement);
        }

        return element;
    }

    private XElement CreateScenarioElement(Scenario scenario)
    {
        if (scenario.DimensionType == DimensionType.ExplicitMember)
        {
            return new XElement(_xbrldi + "explicitMember",
                new XAttribute("dimension", scenario.DimensionName ?? ""),
                scenario.DimensionValue ?? "");
        }

        if (scenario.DimensionType == DimensionType.TypedMember)
        {
            var typedMember = new XElement(_xbrldi + "typedMember",
                new XAttribute("dimension", scenario.DimensionName ?? ""));

            var childName = XName.Get(scenario.DimensionCode ?? "", _dimInt.NamespaceName);
            typedMember.Add(new XElement(childName, scenario.DimensionValue ?? ""));

            return typedMember;
        }

        return new XElement(_xbrldi + scenario.DimensionType.ToXmlName());
    }

    private XElement CreateUnitElement(Unit unit)
    {
        var element = new XElement(_xbrli + "unit",
            new XAttribute("id", unit.Id ?? ""));

        if (!string.IsNullOrEmpty(unit.Measure))
        {
            element.Add(new XElement(_xbrli + "measure", unit.Measure));
        }
        else if (!string.IsNullOrEmpty(unit.Numerator) && !string.IsNullOrEmpty(unit.Denominator))
        {
            var divide = new XElement(_xbrli + "divide",
                new XElement(_xbrli + "unitNumerator",
                    new XElement(_xbrli + "measure", unit.Numerator)),
                new XElement(_xbrli + "unitDenominator",
                    new XElement(_xbrli + "measure", unit.Denominator)));
            element.Add(divide);
        }

        return element;
    }

    private XElement CreateFactElement(Fact fact)
    {
        var name = XName.Get(fact.Id ?? "", _purcbDic.NamespaceName);
        var element = new XElement(name, fact.Value.ToString());

        if (!string.IsNullOrEmpty(fact.ContextRef))
            element.Add(new XAttribute("contextRef", fact.ContextRef));

        if (!string.IsNullOrEmpty(fact.UnitRef))
            element.Add(new XAttribute("unitRef", fact.UnitRef));

        if (fact.Decimals.HasValue)
            element.Add(new XAttribute("decimals", fact.Decimals.Value));

        if (fact.Precision.HasValue)
            element.Add(new XAttribute("precision", fact.Precision.Value));

        return element;
    }

    #endregion
}
