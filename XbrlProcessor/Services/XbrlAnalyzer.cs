using XbrlProcessor.Models.Entities;
using XbrlProcessor.Configuration;

namespace XbrlProcessor.Services
{
    public class XbrlAnalyzer
    {
        private readonly XbrlSettings _settings;

        public XbrlAnalyzer(XbrlSettings settings)
        {
            _settings = settings;
        }
        // Поиск дубликатов контекстов
        public List<List<Context>> FindDuplicateContexts(Instance instance)
        {
            var duplicates = new List<List<Context>>();
            var groups = instance.Contexts
                .GroupBy(c => GetContextSignature(c))
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var group in groups)
            {
                duplicates.Add(group.ToList());
            }

            return duplicates;
        }

        // Создание подписи контекста для сравнения
        private string GetContextSignature(Context context)
        {
            var parts = new List<string>
            {
                context.EntityValue ?? "",
                context.EntityScheme ?? "",
                context.EntitySegment ?? "",
                context.PeriodInstant?.ToString(_settings.DateFormat) ?? "",
                context.PeriodStartDate?.ToString(_settings.DateFormat) ?? "",
                context.PeriodEndDate?.ToString(_settings.DateFormat) ?? "",
                context.PeriodForever.ToString()
            };

            // Добавляем сценарии
            foreach (var scenario in context.Scenarios.OrderBy(s => s.DimensionName))
            {
                parts.Add($"{scenario.DimensionType}|{scenario.DimensionName}|{scenario.DimensionCode}|{scenario.DimensionValue}");
            }

            return string.Join(_settings.ContextSignatureSeparator, parts);
        }

        // Сравнение двух отчетов
        public ComparisonResult CompareInstances(Instance instance1, Instance instance2)
        {
            var result = new ComparisonResult();

            // Создаем словари для быстрого поиска
            var facts1 = instance1.Facts.ToDictionary(f => GetFactKey(f), f => f);
            var facts2 = instance2.Facts.ToDictionary(f => GetFactKey(f), f => f);

            // Факты, отсутствующие в report2
            foreach (var kvp in facts1)
            {
                if (!facts2.ContainsKey(kvp.Key))
                {
                    result.MissingFacts.Add(kvp.Value);
                }
            }

            // Новые факты в report2
            foreach (var kvp in facts2)
            {
                if (!facts1.ContainsKey(kvp.Key))
                {
                    result.NewFacts.Add(kvp.Value);
                }
            }

            // Факты с различающимися значениями
            foreach (var kvp in facts1)
            {
                if (facts2.TryGetValue(kvp.Key, out var fact2))
                {
                    if (kvp.Value.Value != fact2.Value)
                    {
                        result.ModifiedFacts.Add(new FactDifference
                        {
                            FactKey = kvp.Key,
                            Fact1 = kvp.Value,
                            Fact2 = fact2
                        });
                    }
                }
            }

            return result;
        }

        private string GetFactKey(Fact fact)
        {
            return $"{fact.Id}|{fact.ContextRef}";
        }
    }
}
