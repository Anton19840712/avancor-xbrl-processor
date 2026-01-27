using System.Xml.Linq;
using XbrlProcessor.Models.Entities;
using XbrlProcessor.Configuration;
using XbrlProcessor.Builders;

namespace XbrlProcessor.Services;

/// <summary>
/// Сервис для объединения XBRL отчетов
/// </summary>
/// <param name="settings">Настройки приложения</param>
public class XbrlMerger(XbrlSettings settings)
{
    #region Fields

    private readonly XNamespace _xbrli = settings.XmlNamespaces.Xbrli;
    private readonly XNamespace _xbrldi = settings.XmlNamespaces.Xbrldi;
    private readonly XNamespace _link = settings.XmlNamespaces.Link;
    private readonly XNamespace _xlink = settings.XmlNamespaces.Xlink;
    private readonly XNamespace _dimInt = settings.XmlNamespaces.DimInt;
    private readonly XNamespace _purcbDic = settings.XmlNamespaces.PurcbDic;

    #endregion

        #region Public Methods

        /// <summary>
        /// Объединяет два отчета в один, удаляя дубликаты контекстов, единиц и фактов
        /// </summary>
        /// <param name="instance1">Первый отчет</param>
        /// <param name="instance2">Второй отчет</param>
        /// <returns>Объединенный отчет с уникальными элементами</returns>
        public Instance MergeInstances(Instance instance1, Instance instance2)
        {
            var builder = new InstanceBuilder();

            // Объединяем контексты (удаляем дубликаты)
            var contextMap = new Dictionary<string, Context>();
            foreach (var context in instance1.Contexts.Concat(instance2.Contexts))
            {
                var signature = GetContextSignature(context);
                if (!contextMap.ContainsKey(signature))
                {
                    contextMap[signature] = context;
                    builder.AddContext(context);
                }
            }

            // Объединяем единицы измерения (удаляем дубликаты)
            var unitMap = new Dictionary<string, Unit>();
            foreach (var unit in instance1.Units.Concat(instance2.Units))
            {
                var signature = GetUnitSignature(unit);
                if (!unitMap.ContainsKey(signature))
                {
                    unitMap[signature] = unit;
                    builder.AddUnit(unit);
                }
            }

            // Объединяем факты
            var factMap = new Dictionary<string, Fact>();
            foreach (var fact in instance1.Facts.Concat(instance2.Facts))
            {
                var key = $"{fact.Id}|{fact.ContextRef}|{fact.UnitRef}";
                if (!factMap.ContainsKey(key))
                {
                    factMap[key] = fact;
                    builder.AddFact(fact);
                }
            }

            return builder.Build();
        }

        /// <summary>
        /// Сохраняет объект Instance в XBRL файл
        /// </summary>
        /// <param name="instance">Объект Instance для сохранения</param>
        /// <param name="filePath">Путь для сохранения файла</param>
        /// <param name="templatePath">Путь к шаблону XBRL файла</param>
        public void SaveToXbrl(Instance instance, string filePath, string templatePath)
        {
            // Загружаем шаблон из одного из исходных файлов
            var doc = XDocument.Load(templatePath);
            var root = doc.Root!;

            // Очищаем старые элементы
            root.Elements(_xbrli + "context").Remove();
            root.Elements(_xbrli + "unit").Remove();
            root.Elements().Where(e => e.Name.Namespace != _xbrli &&
                                       e.Name.Namespace != _link &&
                                       e.Attribute("contextRef") != null).Remove();

            // Добавляем контексты
            foreach (var context in instance.Contexts)
            {
                root.Add(CreateContextElement(context));
            }

            // Добавляем единицы
            foreach (var unit in instance.Units)
            {
                root.Add(CreateUnitElement(unit));
            }

            // Добавляем факты
            foreach (var fact in instance.Facts)
            {
                root.Add(CreateFactElement(fact));
            }

            doc.Save(filePath);
        }

        #endregion

        #region Create Methods

        private XElement CreateContextElement(Context context)
        {
            var element = new XElement(_xbrli + "context",
                new XAttribute("id", context.Id ?? ""));

            // Entity
            var entityElement = new XElement(_xbrli + "entity",
                new XElement(_xbrli + "identifier",
                    new XAttribute("scheme", context.EntityScheme ?? ""),
                    context.EntityValue ?? ""));

            element.Add(entityElement);

            // Period
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

            // Scenario
            if (context.Scenarios.Count > 0)
            {
                var scenarioElement = new XElement(_xbrli + "scenario");
                foreach (var scenario in context.Scenarios)
                {
                    scenarioElement.Add(CreateScenarioElement(scenario));
                }
                element.Add(scenarioElement);
            }

            return element;
        }

        private XElement CreateScenarioElement(Scenario scenario)
        {
            if (scenario.DimensionType == "explicitMember")
            {
                return new XElement(_xbrldi + "explicitMember",
                    new XAttribute("dimension", scenario.DimensionName ?? ""),
                    scenario.DimensionValue ?? "");
            }
            else if (scenario.DimensionType == "typedMember")
            {
                var typedMember = new XElement(_xbrldi + "typedMember",
                    new XAttribute("dimension", scenario.DimensionName ?? ""));

                var childName = XName.Get(scenario.DimensionCode ?? "", _dimInt.NamespaceName);
                typedMember.Add(new XElement(childName, scenario.DimensionValue ?? ""));

                return typedMember;
            }

            return new XElement(_xbrldi + (scenario.DimensionType ?? ""));
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
            var element = new XElement(name, fact.Value ?? "");

            if (!string.IsNullOrEmpty(fact.ContextRef))
            {
                element.Add(new XAttribute("contextRef", fact.ContextRef));
            }

            if (!string.IsNullOrEmpty(fact.UnitRef))
            {
                element.Add(new XAttribute("unitRef", fact.UnitRef));
            }

            if (fact.Decimals.HasValue)
            {
                element.Add(new XAttribute("decimals", fact.Decimals.Value));
            }

            if (fact.Precision.HasValue)
            {
                element.Add(new XAttribute("precision", fact.Precision.Value));
            }

            return element;
        }

        #endregion

        #region Helper Methods

        private string GetContextSignature(Context context)
        {
            var parts = new List<string>
            {
                context.EntityValue ?? "",
                context.EntityScheme ?? "",
                context.EntitySegment ?? "",
                context.PeriodInstant?.ToString(settings.DateFormat) ?? "",
                context.PeriodStartDate?.ToString(settings.DateFormat) ?? "",
                context.PeriodEndDate?.ToString(settings.DateFormat) ?? "",
                context.PeriodForever.ToString()
            };

            foreach (var scenario in context.Scenarios.OrderBy(s => s.DimensionName))
            {
                parts.Add($"{scenario.DimensionType}|{scenario.DimensionName}|{scenario.DimensionCode}|{scenario.DimensionValue}");
            }

            return string.Join(settings.ContextSignatureSeparator, parts);
        }

        private static string GetUnitSignature(Unit unit)
        {
            return $"{unit.Measure}|{unit.Numerator}|{unit.Denominator}";
        }

        #endregion
    }
