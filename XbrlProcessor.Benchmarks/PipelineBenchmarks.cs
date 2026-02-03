using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using XbrlProcessor.Configuration;
using XbrlProcessor.Models.Entities;
using XbrlProcessor.Services;

namespace XbrlProcessor.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90, iterationCount: 3, warmupCount: 1)]
public class PipelineBenchmarks
{
    private XbrlSettings _settings = null!;
    private XbrlAnalyzer _analyzer = null!;
    private XbrlMerger _merger = null!;
    private Instance _smallInstance = null!;
    private Instance _mediumInstance = null!;
    private Instance _largeInstance = null!;
    private List<(string Name, Instance Instance)> _smallReports = null!;
    private List<(string Name, Instance Instance)> _mediumReports = null!;
    private List<(string Name, Instance Instance)> _largeReports = null!;

    [GlobalSetup]
    public void Setup()
    {
        _settings = new XbrlSettings();
        _analyzer = new XbrlAnalyzer(_settings);
        _merger = new XbrlMerger(_settings);

        // Small: 100 contexts, 500 facts
        _smallInstance = DataGenerator.GenerateInstance(100, 500);

        // Medium: 1_000 contexts, 10_000 facts
        _mediumInstance = DataGenerator.GenerateInstance(1_000, 10_000);

        // Large: 10_000 contexts, 100_000 facts
        _largeInstance = DataGenerator.GenerateInstance(10_000, 100_000);

        // Reports for GlobalCompare
        _smallReports = DataGenerator.GenerateReports(3, 100, 500);
        _mediumReports = DataGenerator.GenerateReports(5, 1_000, 10_000);

        // Large reports: 20 files × 10K contexts × 100K facts
        _largeReports = DataGenerator.GenerateReports(20, 10_000, 100_000);
    }

    // === FindDuplicateContexts ===

    [Benchmark]
    public List<List<Context>> FindDuplicates_Small()
        => _analyzer.FindDuplicateContexts(_smallInstance);

    [Benchmark]
    public List<List<Context>> FindDuplicates_Medium()
        => _analyzer.FindDuplicateContexts(_mediumInstance);

    [Benchmark]
    public List<List<Context>> FindDuplicates_Large()
        => _analyzer.FindDuplicateContexts(_largeInstance);

    // === Merge ===

    [Benchmark]
    public Instance Merge_Small()
        => _merger.MergeInstances(_smallReports.Select(r => r.Instance).ToList());

    [Benchmark]
    public Instance Merge_Medium()
        => _merger.MergeInstances(_mediumReports.Select(r => r.Instance).ToList());

    // === GlobalCompare ===

    [Benchmark]
    public GlobalComparisonResult GlobalCompare_Small()
        => _analyzer.BuildGlobalIndex(_smallReports);

    [Benchmark]
    public GlobalComparisonResult GlobalCompare_Medium()
        => _analyzer.BuildGlobalIndex(_mediumReports);

    // === ContextSignature (hot path) ===

    [Benchmark]
    public string ContextSignature_Single()
        => ContextSignatureHelper.GetSignature(_mediumInstance.Contexts[0], _settings);

    [Benchmark]
    public int ContextSignature_All_Medium()
    {
        var count = 0;
        foreach (var ctx in _mediumInstance.Contexts)
        {
            _ = ContextSignatureHelper.GetSignature(ctx, _settings);
            count++;
        }
        return count;
    }

    // === GlobalIndex: SingleThreaded vs Sharded+Parallel (Large) ===

    [Benchmark(Baseline = true)]
    public GlobalComparisonResult GlobalIndex_SingleThreaded_Large()
    {
        var acc = new GlobalIndexAccumulator(_settings);
        foreach (var (name, instance) in _largeReports)
            acc.Add(name, instance);
        return acc.Build();
    }

    [Benchmark]
    [Arguments(2)]
    [Arguments(4)]
    [Arguments(8)]
    public GlobalComparisonResult GlobalIndex_Sharded_Parallel_Large(int parallelism)
    {
        var acc = new ShardedGlobalIndexAccumulator(_settings, shardCount: parallelism * 2);
        Parallel.ForEach(_largeReports, new ParallelOptions { MaxDegreeOfParallelism = parallelism }, report =>
        {
            acc.Add(report.Name, report.Instance);
        });
        return acc.Build();
    }

    // === Merge: Concurrent+Parallel (Large) ===

    [Benchmark]
    public Instance Merge_Concurrent_Parallel_Large()
    {
        var acc = new ConcurrentMergeAccumulator(_settings);
        Parallel.ForEach(_largeReports, report =>
        {
            acc.Add(report.Instance);
        });
        return acc.Build();
    }

    [Benchmark]
    public Instance Merge_SingleThreaded_Large()
    {
        var acc = new MergeAccumulator(_settings);
        foreach (var (_, instance) in _largeReports)
            acc.Add(instance);
        return acc.Build();
    }
}
