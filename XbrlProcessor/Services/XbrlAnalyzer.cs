using XbrlProcessor.Models.Entities;
using XbrlProcessor.Configuration;

namespace XbrlProcessor.Services;

/// <summary>
/// Сервис для анализа XBRL данных
/// </summary>
/// <param name="settings">Настройки приложения</param>
public class XbrlAnalyzer(XbrlSettings settings)
{

        /// <summary>
        /// Находит дубликаты контекстов в отчете
        /// </summary>
        /// <param name="instance">Объект Instance для анализа</param>
        /// <returns>Список групп дублирующихся контекстов</returns>
        public List<List<Context>> FindDuplicateContexts(Instance instance)
        {
            var duplicates = new List<List<Context>>();
            var groups = instance.Contexts
                .GroupBy(c => GetContextSignature(c))
                .Where(g => g.Count() > 1);

            foreach (var group in groups)
            {
                duplicates.Add([..group]);
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
                context.PeriodInstant?.ToString(settings.DateFormat) ?? "",
                context.PeriodStartDate?.ToString(settings.DateFormat) ?? "",
                context.PeriodEndDate?.ToString(settings.DateFormat) ?? "",
                context.PeriodForever.ToString()
            };

            // Добавляем сценарии
            foreach (var scenario in context.Scenarios.OrderBy(s => s.DimensionName))
            {
                parts.Add($"{scenario.DimensionType}|{scenario.DimensionName}|{scenario.DimensionCode}|{scenario.DimensionValue}");
            }

            return string.Join(settings.ContextSignatureSeparator, parts);
        }

        /// <summary>
        /// Сравнивает два отчета и выявляет различия в фактах
        /// </summary>
        /// <param name="instance1">Первый отчет для сравнения</param>
        /// <param name="instance2">Второй отчет для сравнения</param>
        /// <returns>Результат сравнения с отсутствующими, новыми и измененными фактами</returns>
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
                    if (!kvp.Value.Value.SemanticallyEquals(fact2.Value))
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

        private static string GetFactKey(Fact fact)
        {
            return $"{fact.Id}|{fact.ContextRef}";
        }
    }
