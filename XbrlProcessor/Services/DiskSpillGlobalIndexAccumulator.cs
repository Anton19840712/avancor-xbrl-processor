using System.Text.Json;
using XbrlProcessor.Models.Entities;
using XbrlProcessor.Configuration;

namespace XbrlProcessor.Services;

/// <summary>
/// Аккумулятор для глобального сравнения с поддержкой спилла на диск.
/// Когда in-memory индекс превышает заданный лимит записей, сбрасывает данные
/// во временные файлы (memory-mapped JSON) и при Build() агрегирует с диска.
/// Позволяет обрабатывать данные, превышающие доступную RAM.
/// </summary>
public class DiskSpillGlobalIndexAccumulator : IGlobalIndexAccumulator, IDisposable
{
    private readonly XbrlSettings _settings;
    private readonly int _spillThreshold;
    private readonly string _spillDir;
    private readonly List<string> _spillFiles = new();

    private Dictionary<CrossFileFactKey, Dictionary<string, FactSnapshot>> _index = new();
    private int _reportCount;
    private int _currentEntryCount;

    /// <summary>
    /// Снимок факта для сериализации на диск (без хранения полного Fact в памяти).
    /// </summary>
    private record struct FactSnapshot(string RawValue, XbrlValueType ValueType, decimal? NumericValue);

    public DiskSpillGlobalIndexAccumulator(XbrlSettings settings, int spillThreshold = 500_000, string? spillDir = null)
    {
        _settings = settings;
        _spillThreshold = spillThreshold;
        _spillDir = spillDir ?? Path.Combine(Path.GetTempPath(), $"xbrl-spill-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_spillDir);
    }

    public void Add(string reportName, Instance instance)
    {
        _reportCount++;

        var contextSignatures = instance.Contexts.ToDictionary(
            c => c.Id,
            c => ContextSignatureHelper.GetSignature(c, _settings));

        foreach (var fact in instance.Facts)
        {
            var key = GlobalIndexAccumulator.MakeCrossFileFactKey(fact, contextSignatures);
            if (!_index.TryGetValue(key, out var fileMap))
            {
                fileMap = new Dictionary<string, FactSnapshot>();
                _index[key] = fileMap;
                _currentEntryCount++;
            }
            fileMap.TryAdd(reportName, new FactSnapshot(
                fact.Value.RawValue,
                fact.Value.Type,
                fact.Value.NumericValue));
        }

        if (_currentEntryCount >= _spillThreshold)
            SpillToDisk();
    }

    public GlobalComparisonResult Build()
    {
        // Если есть spill-файлы, сначала сливаем текущий in-memory буфер
        if (_spillFiles.Count > 0 && _index.Count > 0)
            SpillToDisk();

        if (_spillFiles.Count == 0)
            return BuildFromMemory();

        return BuildFromDisk();
    }

    private void SpillToDisk()
    {
        var spillPath = Path.Combine(_spillDir, $"spill_{_spillFiles.Count:D4}.json");

        using (var stream = File.Create(spillPath))
        {
            var data = _index.Select(kv => new SpillEntry
            {
                ConceptName = kv.Key.ConceptName,
                ContextSignature = kv.Key.ContextSignature,
                FileValues = kv.Value.ToDictionary(f => f.Key, f => new SpillFactValue
                {
                    RawValue = f.Value.RawValue,
                    ValueType = f.Value.ValueType,
                    NumericValue = f.Value.NumericValue
                })
            }).ToArray();

            JsonSerializer.Serialize(stream, data);
        }

        _spillFiles.Add(spillPath);
        _index = new Dictionary<CrossFileFactKey, Dictionary<string, FactSnapshot>>();
        _currentEntryCount = 0;

        GC.Collect(2, GCCollectionMode.Aggressive, true, true);
    }

    private GlobalComparisonResult BuildFromMemory()
    {
        var consistent = new List<FactIndexEntry>();
        var modified = new List<FactIndexEntry>();
        var partial = new List<FactIndexEntry>();

        foreach (var (key, fileMap) in _index)
        {
            var factMap = fileMap.ToDictionary(
                kv => kv.Key,
                kv => SnapshotToFact(key, kv.Value));

            var entry = new FactIndexEntry { FactKey = key.ToString(), ValuesByFile = factMap };
            Classify(entry, _reportCount, consistent, modified, partial);
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

    private GlobalComparisonResult BuildFromDisk()
    {
        // Мержим все spill-файлы в единый индекс порциями
        var mergedIndex = new Dictionary<CrossFileFactKey, Dictionary<string, FactSnapshot>>();

        foreach (var spillPath in _spillFiles)
        {
            using var stream = File.OpenRead(spillPath);
            var entries = JsonSerializer.Deserialize<SpillEntry[]>(stream);
            if (entries == null) continue;

            foreach (var entry in entries)
            {
                var key = new CrossFileFactKey(entry.ConceptName, entry.ContextSignature);
                if (!mergedIndex.TryGetValue(key, out var fileMap))
                {
                    fileMap = new Dictionary<string, FactSnapshot>();
                    mergedIndex[key] = fileMap;
                }

                foreach (var (fileName, val) in entry.FileValues)
                {
                    fileMap.TryAdd(fileName, new FactSnapshot(val.RawValue, val.ValueType, val.NumericValue));
                }
            }
        }

        var consistent = new List<FactIndexEntry>();
        var modified = new List<FactIndexEntry>();
        var partial = new List<FactIndexEntry>();

        foreach (var (key, fileMap) in mergedIndex)
        {
            var factMap = fileMap.ToDictionary(
                kv => kv.Key,
                kv => SnapshotToFact(key, kv.Value));

            var entry = new FactIndexEntry { FactKey = key.ToString(), ValuesByFile = factMap };
            Classify(entry, _reportCount, consistent, modified, partial);
        }

        return new GlobalComparisonResult
        {
            TotalFiles = _reportCount,
            TotalUniqueFactKeys = mergedIndex.Count,
            ConsistentFacts = consistent,
            ModifiedFacts = modified,
            PartialFacts = partial
        };
    }

    private static void Classify(FactIndexEntry entry, int totalFiles,
        List<FactIndexEntry> consistent, List<FactIndexEntry> modified, List<FactIndexEntry> partial)
    {
        if (entry.FileCount < totalFiles)
            partial.Add(entry);
        else if (entry.IsConsistent)
            consistent.Add(entry);
        else
            modified.Add(entry);
    }

    private static Fact SnapshotToFact(CrossFileFactKey key, FactSnapshot snapshot)
    {
        return new Fact
        {
            Id = key.ConceptName,
            ConceptName = key.ConceptName,
            Value = XbrlValue.Parse(snapshot.RawValue)
        };
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_spillDir))
                Directory.Delete(_spillDir, recursive: true);
        }
        catch
        {
            // Cleanup is best-effort
        }
    }

    // JSON-сериализуемые модели для spill
    private class SpillEntry
    {
        public string ConceptName { get; set; } = "";
        public string ContextSignature { get; set; } = "";
        public Dictionary<string, SpillFactValue> FileValues { get; set; } = new();
    }

    private class SpillFactValue
    {
        public string RawValue { get; set; } = "";
        public XbrlValueType ValueType { get; set; }
        public decimal? NumericValue { get; set; }
    }
}
