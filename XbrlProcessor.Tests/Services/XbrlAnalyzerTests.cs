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

    #region BuildGlobalIndex

    // Контекст, используемый во всех тестах BuildGlobalIndex
    private static Context MakeContext(string id, string entity = "111", string scheme = "http://test") =>
        new() { Id = id, EntityValue = entity, EntityScheme = scheme, PeriodInstant = new DateTime(2019, 4, 30) };

    private static Context MakeContext2(string id) =>
        new() { Id = id, EntityValue = "222", EntityScheme = "http://test", PeriodInstant = new DateTime(2020, 1, 1) };

    [Fact]
    public void BuildGlobalIndex_WithSingleReport_ShouldReturnAllConsistent()
    {
        var instance = new Instance();
        instance.Contexts.Add(MakeContext("C1"));
        instance.Facts.Add(new Fact { Id = "id1", ConceptName = "dic:Amount", ContextRef = "C1", Value = XbrlValue.Parse("100") });
        instance.Facts.Add(new Fact { Id = "id2", ConceptName = "dic:Name", ContextRef = "C1", Value = XbrlValue.Parse("200") });

        var reports = new List<(string Name, Instance Instance)> { ("file1.xbrl", instance) };

        var result = _analyzer.BuildGlobalIndex(reports);

        result.TotalFiles.Should().Be(1);
        result.TotalUniqueFactKeys.Should().Be(2);
        result.ConsistentFacts.Should().HaveCount(2);
        result.ModifiedFacts.Should().BeEmpty();
        result.PartialFacts.Should().BeEmpty();
    }

    [Fact]
    public void BuildGlobalIndex_WithIdenticalReports_DifferentIds_ShouldReturnConsistent()
    {
        // Два файла с одним и тем же концептом и контекстом, но разными Id — должны сматчиться
        var instance1 = new Instance();
        instance1.Contexts.Add(MakeContext("C1"));
        instance1.Facts.Add(new Fact { Id = "V1_f1", ConceptName = "dic:Amount", ContextRef = "C1", Value = XbrlValue.Parse("100") });

        var instance2 = new Instance();
        instance2.Contexts.Add(MakeContext("X1")); // другой ID контекста, но та же сигнатура
        instance2.Facts.Add(new Fact { Id = "V2_f1", ConceptName = "dic:Amount", ContextRef = "X1", Value = XbrlValue.Parse("100") });

        var reports = new List<(string Name, Instance Instance)>
        {
            ("file1.xbrl", instance1),
            ("file2.xbrl", instance2)
        };

        var result = _analyzer.BuildGlobalIndex(reports);

        result.TotalFiles.Should().Be(2);
        result.TotalUniqueFactKeys.Should().Be(1);
        result.ConsistentFacts.Should().HaveCount(1);
        result.ModifiedFacts.Should().BeEmpty();
        result.PartialFacts.Should().BeEmpty();
    }

    [Fact]
    public void BuildGlobalIndex_WithModifiedValues_ShouldDetectModified()
    {
        var instance1 = new Instance();
        instance1.Contexts.Add(MakeContext("C1"));
        instance1.Facts.Add(new Fact { Id = "V1_f1", ConceptName = "dic:Amount", ContextRef = "C1", Value = XbrlValue.Parse("100") });

        var instance2 = new Instance();
        instance2.Contexts.Add(MakeContext("C1"));
        instance2.Facts.Add(new Fact { Id = "V2_f1", ConceptName = "dic:Amount", ContextRef = "C1", Value = XbrlValue.Parse("999") });

        var reports = new List<(string Name, Instance Instance)>
        {
            ("file1.xbrl", instance1),
            ("file2.xbrl", instance2)
        };

        var result = _analyzer.BuildGlobalIndex(reports);

        result.TotalFiles.Should().Be(2);
        result.TotalUniqueFactKeys.Should().Be(1);
        result.ConsistentFacts.Should().BeEmpty();
        result.ModifiedFacts.Should().HaveCount(1);
        result.PartialFacts.Should().BeEmpty();

        var modified = result.ModifiedFacts[0];
        modified.ValuesByFile["file1.xbrl"].Value.NumericValue.Should().Be(100m);
        modified.ValuesByFile["file2.xbrl"].Value.NumericValue.Should().Be(999m);
    }

    [Fact]
    public void BuildGlobalIndex_WithPartialFacts_ShouldDetectPartial()
    {
        var instance1 = new Instance();
        instance1.Contexts.Add(MakeContext("C1"));
        instance1.Contexts.Add(MakeContext2("C2"));
        instance1.Facts.Add(new Fact { Id = "id1", ConceptName = "dic:Amount", ContextRef = "C1", Value = XbrlValue.Parse("100") });
        instance1.Facts.Add(new Fact { Id = "id2", ConceptName = "dic:Extra", ContextRef = "C2", Value = XbrlValue.Parse("200") });

        var instance2 = new Instance();
        instance2.Contexts.Add(MakeContext("C1"));
        instance2.Facts.Add(new Fact { Id = "id1", ConceptName = "dic:Amount", ContextRef = "C1", Value = XbrlValue.Parse("100") });

        var reports = new List<(string Name, Instance Instance)>
        {
            ("file1.xbrl", instance1),
            ("file2.xbrl", instance2)
        };

        var result = _analyzer.BuildGlobalIndex(reports);

        result.ConsistentFacts.Should().HaveCount(1);
        result.PartialFacts.Should().HaveCount(1);
        result.PartialFacts[0].FileCount.Should().Be(1);
    }

    [Fact]
    public void BuildGlobalIndex_WithThreeReports_MixedCategories()
    {
        var ctx = MakeContext("C1");
        var ctx2 = MakeContext2("C2");

        var instance1 = new Instance();
        instance1.Contexts.Add(MakeContext("C1"));
        instance1.Contexts.Add(MakeContext2("C2"));
        instance1.Facts.Add(new Fact { Id = "a1", ConceptName = "dic:Same", ContextRef = "C1", Value = XbrlValue.Parse("100") });
        instance1.Facts.Add(new Fact { Id = "a2", ConceptName = "dic:Diff", ContextRef = "C1", Value = XbrlValue.Parse("200") });
        instance1.Facts.Add(new Fact { Id = "a3", ConceptName = "dic:OnlyFirst", ContextRef = "C2", Value = XbrlValue.Parse("only-in-1") });

        var instance2 = new Instance();
        instance2.Contexts.Add(MakeContext("C1"));
        instance2.Facts.Add(new Fact { Id = "b1", ConceptName = "dic:Same", ContextRef = "C1", Value = XbrlValue.Parse("100") });
        instance2.Facts.Add(new Fact { Id = "b2", ConceptName = "dic:Diff", ContextRef = "C1", Value = XbrlValue.Parse("222") });

        var instance3 = new Instance();
        instance3.Contexts.Add(MakeContext("C1"));
        instance3.Facts.Add(new Fact { Id = "c1", ConceptName = "dic:Same", ContextRef = "C1", Value = XbrlValue.Parse("100") });
        instance3.Facts.Add(new Fact { Id = "c2", ConceptName = "dic:Diff", ContextRef = "C1", Value = XbrlValue.Parse("200") });

        var reports = new List<(string Name, Instance Instance)>
        {
            ("file1.xbrl", instance1),
            ("file2.xbrl", instance2),
            ("file3.xbrl", instance3)
        };

        var result = _analyzer.BuildGlobalIndex(reports);

        result.TotalFiles.Should().Be(3);
        result.TotalUniqueFactKeys.Should().Be(3);

        // dic:Same — одинаковое значение во всех трех
        result.ConsistentFacts.Should().HaveCount(1);

        // dic:Diff — присутствует во всех, но значения разные (200, 222, 200)
        result.ModifiedFacts.Should().HaveCount(1);

        // dic:OnlyFirst — только в первом файле
        result.PartialFacts.Should().HaveCount(1);
        result.PartialFacts[0].FileCount.Should().Be(1);
    }

    [Fact]
    public void BuildGlobalIndex_WithEmptyReports_ShouldReturnEmptyResult()
    {
        var reports = new List<(string Name, Instance Instance)>
        {
            ("file1.xbrl", new Instance()),
            ("file2.xbrl", new Instance())
        };

        var result = _analyzer.BuildGlobalIndex(reports);

        result.TotalFiles.Should().Be(2);
        result.TotalUniqueFactKeys.Should().Be(0);
        result.ConsistentFacts.Should().BeEmpty();
        result.ModifiedFacts.Should().BeEmpty();
        result.PartialFacts.Should().BeEmpty();
    }

    [Fact]
    public void BuildGlobalIndex_SemanticallyEqualValues_ShouldBeConsistent()
    {
        var instance1 = new Instance();
        instance1.Contexts.Add(MakeContext("C1"));
        instance1.Facts.Add(new Fact { Id = "id1", ConceptName = "dic:Amount", ContextRef = "C1", Value = XbrlValue.Parse("1000.00") });

        var instance2 = new Instance();
        instance2.Contexts.Add(MakeContext("C1"));
        instance2.Facts.Add(new Fact { Id = "id2", ConceptName = "dic:Amount", ContextRef = "C1", Value = XbrlValue.Parse("1000") });

        var reports = new List<(string Name, Instance Instance)>
        {
            ("file1.xbrl", instance1),
            ("file2.xbrl", instance2)
        };

        var result = _analyzer.BuildGlobalIndex(reports);

        result.ConsistentFacts.Should().HaveCount(1);
        result.ModifiedFacts.Should().BeEmpty();
    }

    #endregion
}
