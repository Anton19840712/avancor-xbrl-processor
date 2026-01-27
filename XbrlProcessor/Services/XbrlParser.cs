using System.Xml.Linq;
using XbrlProcessor.Models.Entities;
using XbrlProcessor.Models.Collections;
using XbrlProcessor.Configuration;
using XbrlProcessor.Builders;

namespace XbrlProcessor.Services
{
    /// <summary>
    /// Сервис для парсинга XBRL файлов
    /// </summary>
    public class XbrlParser
    {
        #region Fields

        private readonly XNamespace _xbrli;
        private readonly XNamespace _xbrldi;
        private readonly XNamespace _dimInt;
        private readonly XbrlSettings _settings;

        #endregion

        #region Constructor

        /// <summary>
        /// Конструктор парсера XBRL
        /// </summary>
        /// <param name="settings">Настройки приложения</param>
        public XbrlParser(XbrlSettings settings)
        {
            _settings = settings;
            _xbrli = settings.XmlNamespaces.Xbrli;
            _xbrldi = settings.XmlNamespaces.Xbrldi;
            _dimInt = settings.XmlNamespaces.DimInt;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Парсит XBRL файл и возвращает объект Instance
        /// </summary>
        /// <param name="filePath">Путь к XBRL файлу</param>
        /// <returns>Объект Instance с данными из файла</returns>
        public Instance ParseXbrlFile(string filePath)
        {
            var doc = XDocument.Load(filePath);
            var builder = new InstanceBuilder();

            // Парсинг контекстов
            var contexts = doc.Descendants(_xbrli + "context")
                .Select(ParseContext)
                .ToList();
            builder.AddContexts(contexts);

            // Парсинг единиц измерения
            var units = doc.Descendants(_xbrli + "unit")
                .Select(ParseUnit)
                .ToList();
            builder.AddUnits(units);

            // Парсинг фактов
            var facts = doc.Root!.Elements()
                .Where(e => e.Name.Namespace != _xbrli &&
                           e.Name.LocalName != "schemaRef" &&
                           e.Attribute("contextRef") != null)
                .Select(ParseFact)
                .ToList();
            builder.AddFacts(facts);

            return builder.Build();
        }

        #endregion

        #region Private Methods

        private Context ParseContext(XElement element)
        {
            var context = new Context
            {
                Id = element.Attribute("id")?.Value
            };

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
                    context.PeriodInstant = DateTime.Parse(instant.Value);
                }

                var startDate = period.Element(_xbrli + "startDate");
                if (startDate != null)
                {
                    context.PeriodStartDate = DateTime.Parse(startDate.Value);
                }

                var endDate = period.Element(_xbrli + "endDate");
                if (endDate != null)
                {
                    context.PeriodEndDate = DateTime.Parse(endDate.Value);
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

            var measure = element.Element(_xbrli + "measure");
            if (measure != null)
            {
                unit.Measure = measure.Value;
            }

            var divide = element.Element(_xbrli + "divide");
            if (divide != null)
            {
                var numerator = divide.Element(_xbrli + "unitNumerator")?.Element(_xbrli + "measure");
                if (numerator != null)
                {
                    unit.Numerator = numerator.Value;
                }

                var denominator = divide.Element(_xbrli + "unitDenominator")?.Element(_xbrli + "measure");
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

        #endregion
    }
}
