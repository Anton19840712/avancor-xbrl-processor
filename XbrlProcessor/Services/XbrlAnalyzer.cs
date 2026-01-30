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
            .GroupBy(c => ContextSignatureHelper.GetSignature(c, settings))
            .Where(g => g.Count() > 1);

        foreach (var group in groups)
        {
            duplicates.Add([..group]);
        }

        return duplicates;
    }

    /// <summary>
    /// Сравнивает два отчета и выявляет различия в фактах
    /// </summary>
    /// <param name="instance1">Первый отчет для сравнения</param>
    /// <param name="instance2">Второй отчет для сравнения</param>
    /// <returns>Результат сравнения с отсутствующими, новыми и измененными фактами</returns>
    public ComparisonResult CompareInstances(Instance instance1, Instance instance2)
    {
        // Создаем словари для быстрого поиска
        var facts1 = instance1.Facts.ToDictionary(f => GetFactKey(f), f => f);
        var facts2 = instance2.Facts.ToDictionary(f => GetFactKey(f), f => f);

        var missingFacts = new List<Fact>();
        var newFacts = new List<Fact>();
        var modifiedFacts = new List<FactDifference>();

        // Факты, отсутствующие в report2
        foreach (var kvp in facts1)
        {
            if (!facts2.ContainsKey(kvp.Key))
            {
                missingFacts.Add(kvp.Value);
            }
        }

        // Новые факты в report2
        foreach (var kvp in facts2)
        {
            if (!facts1.ContainsKey(kvp.Key))
            {
                newFacts.Add(kvp.Value);
            }
        }

        // Факты с различающимися значениями
        foreach (var kvp in facts1)
        {
            if (facts2.TryGetValue(kvp.Key, out var fact2))
            {
                if (!kvp.Value.Value.SemanticallyEquals(fact2.Value))
                {
                    modifiedFacts.Add(new FactDifference
                    {
                        FactKey = kvp.Key,
                        Fact1 = kvp.Value,
                        Fact2 = fact2
                    });
                }
            }
        }

        return new ComparisonResult
        {
            MissingFacts = missingFacts,
            NewFacts = newFacts,
            ModifiedFacts = modifiedFacts
        };
    }

    private static string GetFactKey(Fact fact)
    {
        return $"{fact.Id}|{fact.ContextRef}";
    }
}
