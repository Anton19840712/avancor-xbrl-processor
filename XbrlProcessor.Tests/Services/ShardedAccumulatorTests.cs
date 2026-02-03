using FluentAssertions;
using XbrlProcessor.Configuration;
using XbrlProcessor.Models.Entities;
using XbrlProcessor.Services;

namespace XbrlProcessor.Tests.Services;

public class ShardedAccumulatorTests
{
    private readonly XbrlSettings _settings = new();

    #region Helpers

    private static Context MakeContext(string id, string entity = "111", DateTime? instant = null)
        => new()
        {
            Id = id,
            EntityValue = entity,
            EntityScheme = "http://test",
            PeriodInstant = instant ?? new DateTime(2019, 4, 30)
        };

    private static Instance MakeInstance(string prefix, int factCount, int contextCount = 3)
    {
        var instance = new Instance();
        for (var c = 0; c < contextCount; c++)
        {
            instance.Contexts.Add(MakeContext($"{prefix}_C{c}",
                entity: $"entity_{c}",
                instant: new DateTime(2019, 4, 30).AddDays(c)));
        }
        instance.Units.Add(new Unit { Id = "RUB", Measure = "iso4217:RUB" });

        for (var f = 0; f < factCount; f++)
        {
            instance.Facts.Add(new Fact
            {
                Id = $"{prefix}_F{f}",
                ConceptName = $"dic:Concept{f % 10}",
                ContextRef = $"{prefix}_C{f % contextCount}",
                UnitRef = "RUB",
                Value = XbrlValue.Parse($"{f * 100}")
            });
        }
        return instance;
    }

    #endregion

    #region ShardedGlobalIndexAccumulator — Correctness

    [Fact]
    public void ShardedGlobalIndex_SingleReport_MatchesSingleThreaded()
    {
        var instance = MakeInstance("R1", factCount: 50, contextCount: 5);

        var single = new GlobalIndexAccumulator(_settings);
        single.Add("report1.xbrl", instance);
        var expected = single.Build();

        var sharded = new ShardedGlobalIndexAccumulator(_settings, shardCount: 4);
        sharded.Add("report1.xbrl", instance);
        var actual = sharded.Build();

        actual.TotalFiles.Should().Be(expected.TotalFiles);
        actual.TotalUniqueFactKeys.Should().Be(expected.TotalUniqueFactKeys);
        actual.ConsistentFacts.Count.Should().Be(expected.ConsistentFacts.Count);
        actual.ModifiedFacts.Count.Should().Be(expected.ModifiedFacts.Count);
        actual.PartialFacts.Count.Should().Be(expected.PartialFacts.Count);
    }

    [Fact]
    public void ShardedGlobalIndex_MultipleReports_MatchesSingleThreaded()
    {
        var reports = new List<(string Name, Instance Instance)>();
        for (var i = 0; i < 5; i++)
            reports.Add(($"report{i}.xbrl", MakeInstance($"R{i}", factCount: 20, contextCount: 3)));

        // Single-threaded baseline
        var single = new GlobalIndexAccumulator(_settings);
        foreach (var (name, inst) in reports)
            single.Add(name, inst);
        var expected = single.Build();

        // Sharded
        var sharded = new ShardedGlobalIndexAccumulator(_settings, shardCount: 4);
        foreach (var (name, inst) in reports)
            sharded.Add(name, inst);
        var actual = sharded.Build();

        actual.TotalFiles.Should().Be(expected.TotalFiles);
        actual.TotalUniqueFactKeys.Should().Be(expected.TotalUniqueFactKeys);
        actual.ConsistentFacts.Count.Should().Be(expected.ConsistentFacts.Count);
        actual.ModifiedFacts.Count.Should().Be(expected.ModifiedFacts.Count);
        actual.PartialFacts.Count.Should().Be(expected.PartialFacts.Count);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(16)]
    public void ShardedGlobalIndex_DifferentShardCounts_SameResults(int shardCount)
    {
        var reports = new List<(string Name, Instance Instance)>();
        for (var i = 0; i < 3; i++)
            reports.Add(($"report{i}.xbrl", MakeInstance($"R{i}", factCount: 30, contextCount: 4)));

        // Baseline with shard=1
        var baseline = new ShardedGlobalIndexAccumulator(_settings, shardCount: 1);
        foreach (var (name, inst) in reports)
            baseline.Add(name, inst);
        var expected = baseline.Build();

        var sharded = new ShardedGlobalIndexAccumulator(_settings, shardCount: shardCount);
        foreach (var (name, inst) in reports)
            sharded.Add(name, inst);
        var actual = sharded.Build();

        actual.TotalFiles.Should().Be(expected.TotalFiles);
        actual.TotalUniqueFactKeys.Should().Be(expected.TotalUniqueFactKeys);
        actual.ConsistentFacts.Count.Should().Be(expected.ConsistentFacts.Count);
        actual.ModifiedFacts.Count.Should().Be(expected.ModifiedFacts.Count);
        actual.PartialFacts.Count.Should().Be(expected.PartialFacts.Count);
    }

    #endregion

    #region ShardedGlobalIndexAccumulator — Thread Safety

    [Fact]
    public void ShardedGlobalIndex_ParallelAdd_IsThreadSafe()
    {
        var reports = new List<(string Name, Instance Instance)>();
        for (var i = 0; i < 50; i++)
            reports.Add(($"report{i}.xbrl", MakeInstance($"R{i}", factCount: 20, contextCount: 3)));

        // Sequential baseline
        var single = new GlobalIndexAccumulator(_settings);
        foreach (var (name, inst) in reports)
            single.Add(name, inst);
        var expected = single.Build();

        // Parallel with sharded accumulator
        var sharded = new ShardedGlobalIndexAccumulator(_settings, shardCount: 8);
        Parallel.ForEach(reports, report =>
        {
            sharded.Add(report.Name, report.Instance);
        });
        var actual = sharded.Build();

        actual.TotalFiles.Should().Be(expected.TotalFiles);
        actual.TotalUniqueFactKeys.Should().Be(expected.TotalUniqueFactKeys);
        actual.ConsistentFacts.Count.Should().Be(expected.ConsistentFacts.Count);
        actual.ModifiedFacts.Count.Should().Be(expected.ModifiedFacts.Count);
        actual.PartialFacts.Count.Should().Be(expected.PartialFacts.Count);
    }

    #endregion

    #region ConcurrentMergeAccumulator — Correctness

    [Fact]
    public void ConcurrentMerge_SingleReport_MatchesSingleThreaded()
    {
        var instance = MakeInstance("R1", factCount: 50, contextCount: 5);

        var single = new MergeAccumulator(_settings);
        single.Add(instance);
        var expected = single.Build();

        var concurrent = new ConcurrentMergeAccumulator(_settings);
        concurrent.Add(instance);
        var actual = concurrent.Build();

        actual.Contexts.Count.Should().Be(expected.Contexts.Count);
        actual.Units.Count.Should().Be(expected.Units.Count);
        actual.Facts.Count.Should().Be(expected.Facts.Count);
    }

    [Fact]
    public void ConcurrentMerge_MultipleReports_MatchesSingleThreaded()
    {
        var instances = new List<Instance>();
        for (var i = 0; i < 5; i++)
            instances.Add(MakeInstance($"R{i}", factCount: 20, contextCount: 3));

        var single = new MergeAccumulator(_settings);
        foreach (var inst in instances)
            single.Add(inst);
        var expected = single.Build();

        var concurrent = new ConcurrentMergeAccumulator(_settings);
        foreach (var inst in instances)
            concurrent.Add(inst);
        var actual = concurrent.Build();

        actual.Contexts.Count.Should().Be(expected.Contexts.Count);
        actual.Units.Count.Should().Be(expected.Units.Count);
        actual.Facts.Count.Should().Be(expected.Facts.Count);
    }

    #endregion

    #region ConcurrentMergeAccumulator — Thread Safety

    [Fact]
    public void ConcurrentMerge_ParallelAdd_IsThreadSafe()
    {
        var instances = new List<Instance>();
        for (var i = 0; i < 50; i++)
            instances.Add(MakeInstance($"R{i}", factCount: 20, contextCount: 3));

        // Sequential baseline
        var single = new MergeAccumulator(_settings);
        foreach (var inst in instances)
            single.Add(inst);
        var expected = single.Build();

        // Parallel
        var concurrent = new ConcurrentMergeAccumulator(_settings);
        Parallel.ForEach(instances, inst =>
        {
            concurrent.Add(inst);
        });
        var actual = concurrent.Build();

        actual.Contexts.Count.Should().Be(expected.Contexts.Count);
        actual.Units.Count.Should().Be(expected.Units.Count);
        actual.Facts.Count.Should().Be(expected.Facts.Count);
    }

    #endregion

    #region Interface correctness

    [Fact]
    public void MergeAccumulator_ImplementsIMergeAccumulator()
    {
        IMergeAccumulator acc = new MergeAccumulator(_settings);
        acc.Should().NotBeNull();
    }

    [Fact]
    public void ConcurrentMergeAccumulator_ImplementsIMergeAccumulator()
    {
        IMergeAccumulator acc = new ConcurrentMergeAccumulator(_settings);
        acc.Should().NotBeNull();
    }

    [Fact]
    public void GlobalIndexAccumulator_ImplementsIGlobalIndexAccumulator()
    {
        IGlobalIndexAccumulator acc = new GlobalIndexAccumulator(_settings);
        acc.Should().NotBeNull();
    }

    [Fact]
    public void ShardedGlobalIndexAccumulator_ImplementsIGlobalIndexAccumulator()
    {
        IGlobalIndexAccumulator acc = new ShardedGlobalIndexAccumulator(_settings);
        acc.Should().NotBeNull();
    }

    #endregion
}
