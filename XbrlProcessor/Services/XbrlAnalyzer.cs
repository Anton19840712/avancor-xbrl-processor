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
    /// Строит глобальный индекс по N отчетам за один проход — O(n*m).
    /// Ключ факта: ConceptName + сигнатура контекста (не Id + ContextRef,
    /// т.к. Id и ContextRef уникальны внутри файла и не совпадают между файлами).
    /// </summary>
    /// <param name="reports">Список (имя файла, экземпляр)</param>
    public GlobalComparisonResult BuildGlobalIndex(IReadOnlyList<(string Name, Instance Instance)> reports)
    {
        var index = new Dictionary<CrossFileFactKey, Dictionary<string, Fact>>();

        foreach (var (name, instance) in reports)
        {
            // Строим маппинг contextId → сигнатура для данного файла
            var contextSignatures = instance.Contexts.ToDictionary(
                c => c.Id,
                c => ContextSignatureHelper.GetSignature(c, settings));

            foreach (var fact in instance.Facts)
            {
                var key = GlobalIndexAccumulator.MakeCrossFileFactKey(fact, contextSignatures);
                if (!index.TryGetValue(key, out var fileMap))
                {
                    fileMap = new Dictionary<string, Fact>();
                    index[key] = fileMap;
                }
                fileMap.TryAdd(name, fact);
            }
        }

        var totalFiles = reports.Count;
        var consistent = new List<FactIndexEntry>();
        var modified = new List<FactIndexEntry>();
        var partial = new List<FactIndexEntry>();

        foreach (var (key, fileMap) in index)
        {
            var entry = new FactIndexEntry { FactKey = key.ToString(), ValuesByFile = fileMap };

            if (entry.FileCount < totalFiles)
                partial.Add(entry);
            else if (entry.IsConsistent)
                consistent.Add(entry);
            else
                modified.Add(entry);
        }

        return new GlobalComparisonResult
        {
            TotalFiles = totalFiles,
            TotalUniqueFactKeys = index.Count,
            ConsistentFacts = consistent,
            ModifiedFacts = modified,
            PartialFacts = partial
        };
    }
}
