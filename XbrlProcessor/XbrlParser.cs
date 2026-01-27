using System.Xml.Linq;
using Models.XBRL;

namespace XbrlProcessor
{
    public class XbrlParser
    {
        private static readonly XNamespace xbrli = "http://www.xbrl.org/2003/instance";
        private static readonly XNamespace xbrldi = "http://xbrl.org/2006/xbrldi";
        private static readonly XNamespace dimInt = "http://www.cbr.ru/xbrl/udr/dim/dim-int";

        public Instance ParseXbrlFile(string filePath)
        {
            var instance = new Instance();
            var doc = XDocument.Load(filePath);

            // Парсинг контекстов
            var contexts = doc.Descendants(xbrli + "context");
            foreach (var contextElement in contexts)
            {
                var context = ParseContext(contextElement);
                instance.Contexts.Add(context);
            }

            // Парсинг единиц измерения
            var units = doc.Descendants(xbrli + "unit");
            foreach (var unitElement in units)
            {
                var unit = ParseUnit(unitElement);
                instance.Units.Add(unit);
            }

            // Парсинг фактов
            var facts = doc.Root!.Elements()
                .Where(e => e.Name.Namespace != xbrli &&
                           e.Name.LocalName != "schemaRef" &&
                           e.Attribute("contextRef") != null);

            foreach (var factElement in facts)
            {
                var fact = ParseFact(factElement);
                instance.Facts.Add(fact);
            }

            return instance;
        }

        private Context ParseContext(XElement element)
        {
            var context = new Context
            {
                Id = element.Attribute("id")?.Value
            };

            // Entity
            var entity = element.Element(xbrli + "entity");
            if (entity != null)
            {
                var identifier = entity.Element(xbrli + "identifier");
                context.EntityValue = identifier?.Value;
                context.EntityScheme = identifier?.Attribute("scheme")?.Value;
                context.EntitySegment = entity.Element(xbrli + "segment")?.ToString();
            }

            // Period
            var period = element.Element(xbrli + "period");
            if (period != null)
            {
                var instant = period.Element(xbrli + "instant");
                if (instant != null)
                {
                    context.PeriodInstant = DateTime.Parse(instant.Value);
                }

                var startDate = period.Element(xbrli + "startDate");
                if (startDate != null)
                {
                    context.PeriodStartDate = DateTime.Parse(startDate.Value);
                }

                var endDate = period.Element(xbrli + "endDate");
                if (endDate != null)
                {
                    context.PeriodEndDate = DateTime.Parse(endDate.Value);
                }

                var forever = period.Element(xbrli + "forever");
                if (forever != null)
                {
                    context.PeriodForever = true;
                }
            }

            // Scenario
            var scenario = element.Element(xbrli + "scenario");
            if (scenario != null)
            {
                var members = scenario.Elements();
                foreach (var member in members)
                {
                    var scenarioItem = ParseScenario(member);
                    context.Scenarios.Add(scenarioItem);
                }
            }

            return context;
        }

        private Scenario ParseScenario(XElement element)
        {
            var scenario = new Scenario
            {
                DimensionType = element.Name.LocalName,
                DimensionName = element.Attribute("dimension")?.Value
            };

            if (element.Name.LocalName == "explicitMember")
            {
                scenario.DimensionValue = element.Value;
            }
            else if (element.Name.LocalName == "typedMember")
            {
                var child = element.Elements().FirstOrDefault();
                if (child != null)
                {
                    scenario.DimensionCode = child.Name.LocalName;
                    scenario.DimensionValue = child.Value;
                }
            }

            return scenario;
        }

        private Unit ParseUnit(XElement element)
        {
            var unit = new Unit
            {
                Id = element.Attribute("id")?.Value
            };

            var measure = element.Element(xbrli + "measure");
            if (measure != null)
            {
                unit.Measure = measure.Value;
            }

            var divide = element.Element(xbrli + "divide");
            if (divide != null)
            {
                var numerator = divide.Element(xbrli + "unitNumerator")?.Element(xbrli + "measure");
                if (numerator != null)
                {
                    unit.Numerator = numerator.Value;
                }

                var denominator = divide.Element(xbrli + "unitDenominator")?.Element(xbrli + "measure");
                if (denominator != null)
                {
                    unit.Denominator = denominator.Value;
                }
            }

            return unit;
        }

        private Fact ParseFact(XElement element)
        {
            var fact = new Fact
            {
                Id = element.Attribute("id")?.Value ?? element.Name.LocalName,
                ContextRef = element.Attribute("contextRef")?.Value,
                UnitRef = element.Attribute("unitRef")?.Value,
                Value = element.Value
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
    }
}
