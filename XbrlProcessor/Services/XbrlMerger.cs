using System.Xml.Linq;
using XbrlProcessor.Models.Entities;

namespace XbrlProcessor.Services
{
    public class XbrlMerger
    {
        private static readonly XNamespace xbrli = "http://www.xbrl.org/2003/instance";
        private static readonly XNamespace xbrldi = "http://xbrl.org/2006/xbrldi";
        private static readonly XNamespace link = "http://www.xbrl.org/2003/linkbase";
        private static readonly XNamespace xlink = "http://www.w3.org/1999/xlink";
        private static readonly XNamespace dimInt = "http://www.cbr.ru/xbrl/udr/dim/dim-int";
        private static readonly XNamespace purcbDic = "http://www.cbr.ru/xbrl/nso/purcb/dic/purcb-dic";

        public Instance MergeInstances(Instance instance1, Instance instance2)
        {
            var merged = new Instance();

            // Объединяем контексты (удаляем дубликаты)
            var contextMap = new Dictionary<string, Context>();
            foreach (var context in instance1.Contexts.Concat(instance2.Contexts))
            {
                var signature = GetContextSignature(context);
                if (!contextMap.ContainsKey(signature))
                {
                    contextMap[signature] = context;
                    merged.Contexts.Add(context);
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
                    merged.Units.Add(unit);
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
                    merged.Facts.Add(fact);
                }
            }

            return merged;
        }

        public void SaveToXbrl(Instance instance, string filePath, string templatePath)
        {
            // Загружаем шаблон из одного из исходных файлов
            var doc = XDocument.Load(templatePath);
            var root = doc.Root!;

            // Очищаем старые элементы
            root.Elements(xbrli + "context").Remove();
            root.Elements(xbrli + "unit").Remove();
            root.Elements().Where(e => e.Name.Namespace != xbrli &&
                                       e.Name.Namespace != link &&
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

        private XElement CreateContextElement(Context context)
        {
            var element = new XElement(xbrli + "context",
                new XAttribute("id", context.Id ?? ""));

            // Entity
            var entityElement = new XElement(xbrli + "entity",
                new XElement(xbrli + "identifier",
                    new XAttribute("scheme", context.EntityScheme ?? ""),
                    context.EntityValue ?? ""));

            element.Add(entityElement);

            // Period
            var periodElement = new XElement(xbrli + "period");
            if (context.PeriodInstant.HasValue)
            {
                periodElement.Add(new XElement(xbrli + "instant",
                    context.PeriodInstant.Value.ToString("yyyy-MM-dd")));
            }
            else if (context.PeriodStartDate.HasValue && context.PeriodEndDate.HasValue)
            {
                periodElement.Add(new XElement(xbrli + "startDate",
                    context.PeriodStartDate.Value.ToString("yyyy-MM-dd")));
                periodElement.Add(new XElement(xbrli + "endDate",
                    context.PeriodEndDate.Value.ToString("yyyy-MM-dd")));
            }
            else if (context.PeriodForever)
            {
                periodElement.Add(new XElement(xbrli + "forever"));
            }

            element.Add(periodElement);

            // Scenario
            if (context.Scenarios.Count > 0)
            {
                var scenarioElement = new XElement(xbrli + "scenario");
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
                return new XElement(xbrldi + "explicitMember",
                    new XAttribute("dimension", scenario.DimensionName ?? ""),
                    scenario.DimensionValue ?? "");
            }
            else if (scenario.DimensionType == "typedMember")
            {
                var typedMember = new XElement(xbrldi + "typedMember",
                    new XAttribute("dimension", scenario.DimensionName ?? ""));

                var childName = XName.Get(scenario.DimensionCode ?? "", dimInt.NamespaceName);
                typedMember.Add(new XElement(childName, scenario.DimensionValue ?? ""));

                return typedMember;
            }

            return new XElement(xbrldi + scenario.DimensionType);
        }

        private XElement CreateUnitElement(Unit unit)
        {
            var element = new XElement(xbrli + "unit",
                new XAttribute("id", unit.Id ?? ""));

            if (!string.IsNullOrEmpty(unit.Measure))
            {
                element.Add(new XElement(xbrli + "measure", unit.Measure));
            }
            else if (!string.IsNullOrEmpty(unit.Numerator) && !string.IsNullOrEmpty(unit.Denominator))
            {
                var divide = new XElement(xbrli + "divide",
                    new XElement(xbrli + "unitNumerator",
                        new XElement(xbrli + "measure", unit.Numerator)),
                    new XElement(xbrli + "unitDenominator",
                        new XElement(xbrli + "measure", unit.Denominator)));
                element.Add(divide);
            }

            return element;
        }

        private XElement CreateFactElement(Fact fact)
        {
            var name = XName.Get(fact.Id, purcbDic.NamespaceName);
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

        private string GetContextSignature(Context context)
        {
            var parts = new List<string>
            {
                context.EntityValue ?? "",
                context.EntityScheme ?? "",
                context.EntitySegment ?? "",
                context.PeriodInstant?.ToString("yyyy-MM-dd") ?? "",
                context.PeriodStartDate?.ToString("yyyy-MM-dd") ?? "",
                context.PeriodEndDate?.ToString("yyyy-MM-dd") ?? "",
                context.PeriodForever.ToString()
            };

            foreach (var scenario in context.Scenarios.OrderBy(s => s.DimensionName))
            {
                parts.Add($"{scenario.DimensionType}|{scenario.DimensionName}|{scenario.DimensionCode}|{scenario.DimensionValue}");
            }

            return string.Join("||", parts);
        }

        private string GetUnitSignature(Unit unit)
        {
            return $"{unit.Measure}|{unit.Numerator}|{unit.Denominator}";
        }
    }
}
