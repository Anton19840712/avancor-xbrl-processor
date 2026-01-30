using FluentAssertions;
using XbrlProcessor.Configuration;
using XbrlProcessor.Models.Entities;
using XbrlProcessor.Services;

namespace XbrlProcessor.Tests.Services;

public class XbrlAnalyzerTests
{
    private readonly XbrlAnalyzer _analyzer;

    public XbrlAnalyzerTests()
    {
        var settings = new XbrlSettings();
        _analyzer = new XbrlAnalyzer(settings);
    }

    #region FindDuplicateContexts

    [Fact]
    public void FindDuplicateContexts_WithNoDuplicates_ShouldReturnEmpty()
    {
        var instance = new Instance();
        instance.Contexts.Add(new Context
        {
            Id = "C1",
            EntityValue = "111",
            EntityScheme = "http://test",
            PeriodInstant = new DateTime(2019, 4, 30)
        });
        instance.Contexts.Add(new Context
        {
            Id = "C2",
            EntityValue = "222",
            EntityScheme = "http://test",
            PeriodInstant = new DateTime(2019, 4, 30)
        });

        var result = _analyzer.FindDuplicateContexts(instance);

        result.Should().BeEmpty();
    }

    [Fact]
    public void FindDuplicateContexts_WithDuplicates_ShouldReturnGroups()
    {
        var instance = new Instance();
        instance.Contexts.Add(new Context
        {
            Id = "C1",
            EntityValue = "111",
            EntityScheme = "http://test",
            PeriodInstant = new DateTime(2019, 4, 30)
        });
        instance.Contexts.Add(new Context
        {
            Id = "C2",
            EntityValue = "111",
            EntityScheme = "http://test",
            PeriodInstant = new DateTime(2019, 4, 30)
        });

        var result = _analyzer.FindDuplicateContexts(instance);

        result.Should().HaveCount(1);
        result[0].Should().HaveCount(2);
        result[0].Select(c => c.Id).Should().Contain("C1").And.Contain("C2");
    }

    [Fact]
    public void FindDuplicateContexts_WithEmptyInstance_ShouldReturnEmpty()
    {
        var instance = new Instance();

        var result = _analyzer.FindDuplicateContexts(instance);

        result.Should().BeEmpty();
    }

    #endregion

    #region CompareInstances

    [Fact]
    public void CompareInstances_WithIdenticalInstances_ShouldReturnNoDifferences()
    {
        var instance1 = new Instance();
        instance1.Facts.Add(new Fact { Id = "F1", ContextRef = "C1", Value = XbrlValue.Parse("100") });

        var instance2 = new Instance();
        instance2.Facts.Add(new Fact { Id = "F1", ContextRef = "C1", Value = XbrlValue.Parse("100") });

        var result = _analyzer.CompareInstances(instance1, instance2);

        result.MissingFacts.Should().BeEmpty();
        result.NewFacts.Should().BeEmpty();
        result.ModifiedFacts.Should().BeEmpty();
    }

    [Fact]
    public void CompareInstances_WithMissingFact_ShouldDetectMissing()
    {
        var instance1 = new Instance();
        instance1.Facts.Add(new Fact { Id = "F1", ContextRef = "C1", Value = XbrlValue.Parse("100") });
        instance1.Facts.Add(new Fact { Id = "F2", ContextRef = "C1", Value = XbrlValue.Parse("200") });

        var instance2 = new Instance();
        instance2.Facts.Add(new Fact { Id = "F1", ContextRef = "C1", Value = XbrlValue.Parse("100") });

        var result = _analyzer.CompareInstances(instance1, instance2);

        result.MissingFacts.Should().HaveCount(1);
        result.MissingFacts[0].Id.Should().Be("F2");
        result.NewFacts.Should().BeEmpty();
    }

    [Fact]
    public void CompareInstances_WithNewFact_ShouldDetectNew()
    {
        var instance1 = new Instance();
        instance1.Facts.Add(new Fact { Id = "F1", ContextRef = "C1", Value = XbrlValue.Parse("100") });

        var instance2 = new Instance();
        instance2.Facts.Add(new Fact { Id = "F1", ContextRef = "C1", Value = XbrlValue.Parse("100") });
        instance2.Facts.Add(new Fact { Id = "F3", ContextRef = "C1", Value = XbrlValue.Parse("300") });

        var result = _analyzer.CompareInstances(instance1, instance2);

        result.NewFacts.Should().HaveCount(1);
        result.NewFacts[0].Id.Should().Be("F3");
        result.MissingFacts.Should().BeEmpty();
    }

    [Fact]
    public void CompareInstances_WithModifiedFact_ShouldDetectModified()
    {
        var instance1 = new Instance();
        instance1.Facts.Add(new Fact { Id = "F1", ContextRef = "C1", Value = XbrlValue.Parse("100") });

        var instance2 = new Instance();
        instance2.Facts.Add(new Fact { Id = "F1", ContextRef = "C1", Value = XbrlValue.Parse("999") });

        var result = _analyzer.CompareInstances(instance1, instance2);

        result.ModifiedFacts.Should().HaveCount(1);
        result.ModifiedFacts[0].FactKey.Should().Be("F1|C1");
        result.ModifiedFacts[0].Fact1!.Value.NumericValue.Should().Be(100m);
        result.ModifiedFacts[0].Fact2!.Value.NumericValue.Should().Be(999m);
    }

    [Fact]
    public void CompareInstances_SemanticallyEqualValues_ShouldNotReportModified()
    {
        var instance1 = new Instance();
        instance1.Facts.Add(new Fact { Id = "F1", ContextRef = "C1", Value = XbrlValue.Parse("1000.00") });

        var instance2 = new Instance();
        instance2.Facts.Add(new Fact { Id = "F1", ContextRef = "C1", Value = XbrlValue.Parse("1000") });

        var result = _analyzer.CompareInstances(instance1, instance2);

        result.ModifiedFacts.Should().BeEmpty();
    }

    [Fact]
    public void CompareInstances_WithEmptyInstances_ShouldReturnNoDifferences()
    {
        var instance1 = new Instance();
        var instance2 = new Instance();

        var result = _analyzer.CompareInstances(instance1, instance2);

        result.MissingFacts.Should().BeEmpty();
        result.NewFacts.Should().BeEmpty();
        result.ModifiedFacts.Should().BeEmpty();
    }

    #endregion
}
