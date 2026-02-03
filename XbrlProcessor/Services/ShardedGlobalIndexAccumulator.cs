using System.Collections.Concurrent;
using XbrlProcessor.Models.Entities;
using XbrlProcessor.Configuration;

namespace XbrlProcessor.Services;

/// <summary>
/// Потокобезопасный шардированный аккумулятор для глобального сравнения XBRL-отчетов.
/// Каждый шард имеет свой словарь и lock — минимальная контентность между потоками.
/// Shard count = power of 2, маршрутизация через bitwise AND (без деления).
/// Использует CrossFileFactKey (struct) вместо строковых ключей — без аллокаций на каждый факт.
/// </summary>
public class ShardedGlobalIndexAccumulator : IGlobalIndexAccumulator
{
    private readonly XbrlSettings _settings;
    private readonly Shard[] _shards;
    private readonly int _shardMask; // shardCount - 1, для быстрой маршрутизации
    private int _reportCount;

    private sealed class Shard
    {
        public readonly object Lock = new();
        public readonly Dictionary<CrossFileFactKey, Dictionary<string, Fact>> Index = new();
    }

    public ShardedGlobalIndexAccumulator(XbrlSettings settings, int? shardCount = null)
    {
        _settings = settings;

        // Дефолт: Environment.ProcessorCount * 2, округлённый до power of 2
        var count = shardCount ?? Environment.ProcessorCount * 2;
        count = RoundUpToPowerOf2(count);
        if (count < 1) count = 1;

        _shardMask = count - 1;
        _shards = new Shard[count];
        for (var i = 0; i < count; i++)
            _shards[i] = new Shard();
    }

    /// <summary>
    /// Добавляет один отчёт в глобальный индекс. Потокобезопасно.
    /// </summary>
    public void Add(string reportName, Instance instance)
    {
        Interlocked.Increment(ref _reportCount);

        var contextSignatures = instance.Contexts.ToDictionary(
            c => c.Id,
            c => ContextSignatureHelper.GetSignature(c, _settings));

        foreach (var fact in instance.Facts)
        {
            var key = GlobalIndexAccumulator.MakeCrossFileFactKey(fact, contextSignatures);
            var shardIndex = key.GetHashCode() & _shardMask;
            var shard = _shards[shardIndex];

            lock (shard.Lock)
            {
                if (!shard.Index.TryGetValue(key, out var fileMap))
                {
                    fileMap = new Dictionary<string, Fact>();
                    shard.Index[key] = fileMap;
                }
                fileMap.TryAdd(reportName, fact);
            }
        }
    }

    /// <summary>
    /// Формирует результат глобального сравнения, агрегируя все шарды.
    /// </summary>
    public GlobalComparisonResult Build()
    {
        var consistent = new List<FactIndexEntry>();
        var modified = new List<FactIndexEntry>();
        var partial = new List<FactIndexEntry>();
        var totalKeys = 0;

        foreach (var shard in _shards)
        {
            totalKeys += shard.Index.Count;
            foreach (var (key, fileMap) in shard.Index)
            {
                var entry = new FactIndexEntry { FactKey = key.ToString(), ValuesByFile = fileMap };

                if (entry.FileCount < _reportCount)
                    partial.Add(entry);
                else if (entry.IsConsistent)
                    consistent.Add(entry);
                else
                    modified.Add(entry);
            }
        }

        return new GlobalComparisonResult
        {
            TotalFiles = _reportCount,
            TotalUniqueFactKeys = totalKeys,
            ConsistentFacts = consistent,
            ModifiedFacts = modified,
            PartialFacts = partial
        };
    }

    private static int RoundUpToPowerOf2(int value)
    {
        if (value <= 1) return 1;
        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        return value + 1;
    }
}
