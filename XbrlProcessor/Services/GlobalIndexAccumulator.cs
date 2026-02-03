using XbrlProcessor.Models.Entities;
using XbrlProcessor.Configuration;

namespace XbrlProcessor.Services;

/// <summary>
/// Потоковый аккумулятор для глобального сравнения XBRL-отчетов.
/// Принимает отчёты по одному, строя индекс инкрементально.
/// После Add Instance может быть освобождён GC.
/// Использует CrossFileFactKey (struct) вместо строковых ключей — без аллокаций на каждый факт.
/// </summary>
public class GlobalIndexAccumulator : IGlobalIndexAccumulator
{
    private readonly XbrlSettings _settings;
    private readonly Dictionary<CrossFileFactKey, Dictionary<string, Fact>> _index = new();
    private int _reportCount;

    public GlobalIndexAccumulator(XbrlSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Добавляет один отчёт в глобальный индекс.
    /// После вызова Instance может быть освобождён.
    /// </summary>
    public void Add(string reportName, Instance instance)
    {
        _reportCount++;

        // Строим маппинг contextId → сигнатура для данного файла
        var contextSignatures = instance.Contexts.ToDictionary(
            c => c.Id,
            c => ContextSignatureHelper.GetSignature(c, _settings));

        foreach (var fact in instance.Facts)
        {
            var key = MakeCrossFileFactKey(fact, contextSignatures);
            if (!_index.TryGetValue(key, out var fileMap))
            {
                fileMap = new Dictionary<string, Fact>();
                _index[key] = fileMap;
            }
            fileMap.TryAdd(reportName, fact);
        }
    }

    /// <summary>
    /// Формирует результат глобального сравнения.
    /// </summary>
    public GlobalComparisonResult Build()
    {
        var consistent = new List<FactIndexEntry>();
        var modified = new List<FactIndexEntry>();
        var partial = new List<FactIndexEntry>();

        foreach (var (key, fileMap) in _index)
        {
            var entry = new FactIndexEntry { FactKey = key.ToString(), ValuesByFile = fileMap };

            if (entry.FileCount < _reportCount)
                partial.Add(entry);
            else if (entry.IsConsistent)
                consistent.Add(entry);
            else
                modified.Add(entry);
        }

        return new GlobalComparisonResult
        {
            TotalFiles = _reportCount,
            TotalUniqueFactKeys = _index.Count,
            ConsistentFacts = consistent,
            ModifiedFacts = modified,
            PartialFacts = partial
        };
    }

    internal static CrossFileFactKey MakeCrossFileFactKey(Fact fact, Dictionary<string, string> contextSignatures)
    {
        var conceptName = fact.ConceptName ?? fact.Id;
        var contextSig = fact.ContextRef != null && contextSignatures.TryGetValue(fact.ContextRef, out var sig)
            ? sig
            : fact.ContextRef ?? "";
        return new CrossFileFactKey(conceptName, contextSig);
    }
}
