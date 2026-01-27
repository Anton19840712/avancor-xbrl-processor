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
        public Instance ParseXbrlFile(string filePath)
        {
            var doc = XDocument.Load(filePath);
            var builder = new InstanceBuilder();

            // Парсинг контекстов
            builder.AddContexts(doc.Descendants(_xbrli + "context")
                .Select(ParseContext));

            // Парсинг единиц измерения
            builder.AddUnits(doc.Descendants(_xbrli + "unit")
                .Select(ParseUnit));

            // Парсинг фактов
            builder.AddFacts(doc.Root!.Elements()
                .Where(e => e.Name.Namespace != _xbrli &&
                           e.Name.LocalName != "schemaRef" &&
                           e.Attribute("contextRef") != null)
                .Select(ParseFact));

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
            var dimensionType = element.Name.LocalName;
            var dimensionName = element.Attribute("dimension")?.Value;
            string? dimensionCode = null;
            string? dimensionValue = null;

            if (dimensionType == "explicitMember")
            {
                dimensionValue = element.Value;
            }
            else if (dimensionType == "typedMember")
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

        private Unit ParseUnit(XElement element)
        {
            var id = element.Attribute("id")?.Value;
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
                var numeratorElement = divide.Element(_xbrli + "unitNumerator")?.Element(_xbrli + "measure");
                if (numeratorElement != null)
                {
                    numerator = numeratorElement.Value;
                }

                var denominatorElement = divide.Element(_xbrli + "unitDenominator")?.Element(_xbrli + "measure");
                if (denominatorElement != null)
                {
                    denominator = denominatorElement.Value;
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
