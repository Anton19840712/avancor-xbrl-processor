using FluentAssertions;
using XbrlProcessor.Configuration;
using XbrlProcessor.Models.Entities;
using XbrlProcessor.Services;

namespace XbrlProcessor.Tests.Services;

public class XbrlMergerTests
{
    private readonly XbrlMerger _merger;

    public XbrlMergerTests()
    {
        var settings = new XbrlSettings();
        _merger = new XbrlMerger(settings);
    }

    [Fact]
    public void MergeInstances_WithDistinctContexts_ShouldIncludeAll()
    {
        var instance1 = new Instance();
        instance1.Contexts.Add(new Context
        {
            Id = "C1",
            EntityValue = "111",
            EntityScheme = "http://test",
            PeriodInstant = new DateTime(2019, 4, 30)
        });

        var instance2 = new Instance();
        instance2.Contexts.Add(new Context
        {
            Id = "C2",
            EntityValue = "222",
            EntityScheme = "http://test",
            PeriodInstant = new DateTime(2019, 4, 30)
        });

        var result = _merger.MergeInstances(instance1, instance2);

        result.Contexts.Should().HaveCount(2);
    }

    [Fact]
    public void MergeInstances_WithDuplicateContexts_ShouldDedup()
    {
        var instance1 = new Instance();
        instance1.Contexts.Add(new Context
        {
            Id = "C1",
            EntityValue = "111",
            EntityScheme = "http://test",
            PeriodInstant = new DateTime(2019, 4, 30)
        });

        var instance2 = new Instance();
        instance2.Contexts.Add(new Context
        {
            Id = "C1_dup",
            EntityValue = "111",
            EntityScheme = "http://test",
            PeriodInstant = new DateTime(2019, 4, 30)
        });

        var result = _merger.MergeInstances(instance1, instance2);

        result.Contexts.Should().HaveCount(1);
        result.Contexts[0].Id.Should().Be("C1");
    }

    [Fact]
    public void MergeInstances_WithDuplicateUnits_ShouldDedup()
    {
        var instance1 = new Instance();
        instance1.Units.Add(new Unit { Id = "RUB", Measure = "iso4217:RUB" });

        var instance2 = new Instance();
        instance2.Units.Add(new Unit { Id = "RUB2", Measure = "iso4217:RUB" });

        var result = _merger.MergeInstances(instance1, instance2);

        result.Units.Should().HaveCount(1);
        result.Units[0].Id.Should().Be("RUB");
    }

    [Fact]
    public void MergeInstances_WithDistinctUnits_ShouldIncludeAll()
    {
        var instance1 = new Instance();
        instance1.Units.Add(new Unit { Id = "RUB", Measure = "iso4217:RUB" });

        var instance2 = new Instance();
        instance2.Units.Add(new Unit { Id = "USD", Measure = "iso4217:USD" });

        var result = _merger.MergeInstances(instance1, instance2);

        result.Units.Should().HaveCount(2);
    }

    [Fact]
    public void MergeInstances_WithDuplicateFacts_ShouldDedup()
    {
        var instance1 = new Instance();
        instance1.Facts.Add(new Fact { Id = "F1", ContextRef = "C1", UnitRef = "RUB", Value = XbrlValue.Parse("100") });

        var instance2 = new Instance();
        instance2.Facts.Add(new Fact { Id = "F1", ContextRef = "C1", UnitRef = "RUB", Value = XbrlValue.Parse("200") });

        var result = _merger.MergeInstances(instance1, instance2);

        result.Facts.Should().HaveCount(1);
        result.Facts[0].Value.ToString().Should().Be("100");
    }

    [Fact]
    public void MergeInstances_WithDistinctFacts_ShouldIncludeAll()
    {
        var instance1 = new Instance();
        instance1.Facts.Add(new Fact { Id = "F1", ContextRef = "C1", UnitRef = "RUB", Value = XbrlValue.Parse("100") });

        var instance2 = new Instance();
        instance2.Facts.Add(new Fact { Id = "F2", ContextRef = "C1", UnitRef = "RUB", Value = XbrlValue.Parse("200") });

        var result = _merger.MergeInstances(instance1, instance2);

        result.Facts.Should().HaveCount(2);
    }

    [Fact]
    public void MergeInstances_WithEmptyInstances_ShouldReturnEmpty()
    {
        var instance1 = new Instance();
        var instance2 = new Instance();

        var result = _merger.MergeInstances(instance1, instance2);

        result.Contexts.Should().BeEmpty();
        result.Units.Should().BeEmpty();
        result.Facts.Should().BeEmpty();
    }
}
