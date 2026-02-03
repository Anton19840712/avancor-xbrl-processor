using FluentAssertions;
using XbrlProcessor.Configuration;
using XbrlProcessor.Models.Entities;
using XbrlProcessor.Services;

namespace XbrlProcessor.Tests.Services;

public class DiskSpillAccumulatorTests : IDisposable
{
    private readonly XbrlSettings _settings = new();
    private readonly string _spillDir;

    public DiskSpillAccumulatorTests()
    {
        _spillDir = Path.Combine(Path.GetTempPath(), $"xbrl-test-spill-{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        if (Directory.Exists(_spillDir))
            Directory.Delete(_spillDir, recursive: true);
    }

    private static Instance MakeInstance(string prefix, int factCount, int contextCount = 3)
    {
        var instance = new Instance();
        for (var c = 0; c < contextCount; c++)
        {
            instance.Contexts.Add(new Context
            {
                Id = $"{prefix}_C{c}",
                EntityValue = $"entity_{c}",
                EntityScheme = "http://test",
                PeriodInstant = new DateTime(2019, 4, 30).AddDays(c)
            });
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

    [Fact]
    public void DiskSpill_SmallData_NoSpill_MatchesSingleThreaded()
    {
        var reports = new List<(string Name, Instance Instance)>();
        for (var i = 0; i < 3; i++)
            reports.Add(($"report{i}.xbrl", MakeInstance($"R{i}", factCount: 20, contextCount: 3)));

        var single = new GlobalIndexAccumulator(_settings);
        foreach (var (name, inst) in reports)
            single.Add(name, inst);
        var expected = single.Build();

        using var spill = new DiskSpillGlobalIndexAccumulator(_settings, spillThreshold: 10_000, spillDir: _spillDir);
        foreach (var (name, inst) in reports)
            spill.Add(name, inst);
        var actual = spill.Build();

        actual.TotalFiles.Should().Be(expected.TotalFiles);
        actual.TotalUniqueFactKeys.Should().Be(expected.TotalUniqueFactKeys);
        actual.ConsistentFacts.Count.Should().Be(expected.ConsistentFacts.Count);
        actual.ModifiedFacts.Count.Should().Be(expected.ModifiedFacts.Count);
        actual.PartialFacts.Count.Should().Be(expected.PartialFacts.Count);
    }

    [Fact]
    public void DiskSpill_ForcedSpill_MatchesSingleThreaded()
    {
        var reports = new List<(string Name, Instance Instance)>();
        for (var i = 0; i < 5; i++)
            reports.Add(($"report{i}.xbrl", MakeInstance($"R{i}", factCount: 50, contextCount: 5)));

        var single = new GlobalIndexAccumulator(_settings);
        foreach (var (name, inst) in reports)
            single.Add(name, inst);
        var expected = single.Build();

        // spillThreshold=20 — принудительно спиллить после каждого отчёта
        using var spill = new DiskSpillGlobalIndexAccumulator(_settings, spillThreshold: 20, spillDir: _spillDir);
        foreach (var (name, inst) in reports)
            spill.Add(name, inst);
        var actual = spill.Build();

        actual.TotalFiles.Should().Be(expected.TotalFiles);
        actual.TotalUniqueFactKeys.Should().Be(expected.TotalUniqueFactKeys);
        actual.ConsistentFacts.Count.Should().Be(expected.ConsistentFacts.Count);
        actual.ModifiedFacts.Count.Should().Be(expected.ModifiedFacts.Count);
        actual.PartialFacts.Count.Should().Be(expected.PartialFacts.Count);
    }

    [Fact]
    public void DiskSpill_CreatesSpillFiles()
    {
        var instance = MakeInstance("R1", factCount: 100, contextCount: 5);

        // spillThreshold=10 — мгновенный спилл
        using var spill = new DiskSpillGlobalIndexAccumulator(_settings, spillThreshold: 10, spillDir: _spillDir);
        spill.Add("report1.xbrl", instance);

        // Спилл-файлы должны быть созданы
        var files = Directory.GetFiles(_spillDir, "spill_*.json");
        files.Should().NotBeEmpty();
    }

    [Fact]
    public void DiskSpill_CleanupOnDispose()
    {
        var instance = MakeInstance("R1", factCount: 50, contextCount: 3);

        var localSpillDir = Path.Combine(Path.GetTempPath(), $"xbrl-test-cleanup-{Guid.NewGuid():N}");
        var acc = new DiskSpillGlobalIndexAccumulator(_settings, spillThreshold: 10, spillDir: localSpillDir);
        acc.Add("report1.xbrl", instance);
        acc.Build();

        Directory.Exists(localSpillDir).Should().BeTrue();

        acc.Dispose();

        Directory.Exists(localSpillDir).Should().BeFalse();
    }

    [Fact]
    public void DiskSpill_ImplementsIGlobalIndexAccumulator()
    {
        using var acc = new DiskSpillGlobalIndexAccumulator(_settings, spillDir: _spillDir);
        IGlobalIndexAccumulator iface = acc;
        iface.Should().NotBeNull();
    }
}
