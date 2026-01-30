using FluentAssertions;
using XbrlProcessor.Configuration;
using XbrlProcessor.Models.Entities;
using XbrlProcessor.Services;

namespace XbrlProcessor.Tests.Services;

public class XbrlParserTests
{
    private readonly XbrlParser _parser;
    private readonly XbrlSettings _settings;
    private readonly string _testDataPath;

    public XbrlParserTests()
    {
        _settings = new XbrlSettings();
        _parser = new XbrlParser(_settings);
        _testDataPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "TestData");
    }

    [Fact]
    public void ParseXbrlFile_WithInstantPeriod_ShouldParseContextCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "test-context-instant.xml");

        // Act
        var instance = _parser.ParseXbrlFile(filePath);

        // Assert
        instance.Should().NotBeNull();
        instance.Contexts.Should().HaveCount(1);

        var context = instance.Contexts.First();
        context.Id.Should().Be("C1");
        context.EntityValue.Should().Be("1234567890");
        context.EntityScheme.Should().Be("http://www.cbr.ru");
        context.PeriodInstant.Should().Be(new DateTime(2019, 4, 30));
        context.PeriodStartDate.Should().BeNull();
        context.PeriodEndDate.Should().BeNull();
        context.PeriodForever.Should().BeFalse();
    }

    [Fact]
    public void ParseXbrlFile_WithDurationPeriod_ShouldParseContextCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "test-context-duration.xml");

        // Act
        var instance = _parser.ParseXbrlFile(filePath);

        // Assert
        instance.Should().NotBeNull();
        instance.Contexts.Should().HaveCount(1);

        var context = instance.Contexts.First();
        context.Id.Should().Be("C2");
        context.PeriodInstant.Should().BeNull();
        context.PeriodStartDate.Should().Be(new DateTime(2019, 1, 1));
        context.PeriodEndDate.Should().Be(new DateTime(2019, 4, 30));
        context.PeriodForever.Should().BeFalse();
    }

    [Fact]
    public void ParseXbrlFile_WithScenario_ShouldParseContextAndScenariosCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "test-context-scenario.xml");

        // Act
        var instance = _parser.ParseXbrlFile(filePath);

        // Assert
        instance.Should().NotBeNull();
        instance.Contexts.Should().HaveCount(1);

        var context = instance.Contexts.First();
        context.Id.Should().Be("C3");
        context.Scenarios.Should().HaveCount(2);

        // Test explicit member
        var explicitMember = context.Scenarios.First(s => s.DimensionType == "explicitMember");
        explicitMember.DimensionName.Should().Be("dim-int:TestDimension");
        explicitMember.DimensionValue.Should().Be("purcb-dic:TestMember");
        explicitMember.DimensionCode.Should().BeNull();

        // Test typed member
        var typedMember = context.Scenarios.First(s => s.DimensionType == "typedMember");
        typedMember.DimensionName.Should().Be("dim-int:ID_sobstv_CZBTaxis");
        typedMember.DimensionCode.Should().Be("ID_sobstv_CZB");
        typedMember.DimensionValue.Should().Be("12345");
    }

    [Fact]
    public void ParseXbrlFile_WithSimpleUnit_ShouldParseUnitCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "test-unit-simple.xml");

        // Act
        var instance = _parser.ParseXbrlFile(filePath);

        // Assert
        instance.Should().NotBeNull();
        instance.Units.Should().HaveCount(1);

        var unit = instance.Units.First();
        unit.Id.Should().Be("RUB");
        unit.Measure.Should().Be("iso4217:RUB");
        unit.Numerator.Should().BeNull();
        unit.Denominator.Should().BeNull();
    }

    [Fact]
    public void ParseXbrlFile_WithDivideUnit_ShouldParseUnitCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "test-unit-divide.xml");

        // Act
        var instance = _parser.ParseXbrlFile(filePath);

        // Assert
        instance.Should().NotBeNull();
        instance.Units.Should().HaveCount(1);

        var unit = instance.Units.First();
        unit.Id.Should().Be("RUB_PURE");
        unit.Measure.Should().BeNull();
        unit.Numerator.Should().Be("iso4217:RUB");
        unit.Denominator.Should().Be("xbrli-pure:pure");
    }

    [Fact]
    public void ParseXbrlFile_WithFact_ShouldParseFactCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "test-fact.xml");

        // Act
        var instance = _parser.ParseXbrlFile(filePath);

        // Assert
        instance.Should().NotBeNull();
        instance.Contexts.Should().HaveCount(1);
        instance.Units.Should().HaveCount(1);
        instance.Facts.Should().HaveCount(1);

        var fact = instance.Facts.First();
        fact.Id.Should().Be("TestFact");
        fact.ContextRef.Should().Be("C1");
        fact.UnitRef.Should().Be("RUB");
        fact.Value.Type.Should().Be(XbrlValueType.Numeric);
        fact.Value.NumericValue.Should().Be(1000000.50m);
        fact.Value.ToString().Should().Be("1000000.50");
        fact.Decimals.Should().Be(2);
        fact.Precision.Should().BeNull();
    }

    [Fact]
    public void ParseXbrlFile_WithRealReportFile_ShouldParseSuccessfully()
    {
        // Arrange
        var reportsPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "XbrlProcessor", "Reports");
        var filePath = Path.Combine(reportsPath, "report1.xbrl");

        if (!File.Exists(filePath))
        {
            // Skip test if report file doesn't exist
            return;
        }

        // Act
        var instance = _parser.ParseXbrlFile(filePath);

        // Assert
        instance.Should().NotBeNull();
        instance.Contexts.Should().NotBeEmpty();
        instance.Units.Should().NotBeEmpty();
        instance.Facts.Should().NotBeEmpty();

        // Verify all contexts have IDs
        instance.Contexts.Should().AllSatisfy(c => c.Id.Should().NotBeNullOrEmpty());

        // Verify all units have IDs
        instance.Units.Should().AllSatisfy(u => u.Id.Should().NotBeNullOrEmpty());

        // Verify all facts have required properties
        instance.Facts.Should().AllSatisfy(f =>
        {
            f.Id.Should().NotBeNullOrEmpty();
            f.ContextRef.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public void ParseXbrlFile_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var filePath = "non-existent-file.xml";

        // Act
        Action act = () => _parser.ParseXbrlFile(filePath);

        // Assert
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void ParseXbrlFile_WithInvalidXml_ShouldThrowXmlException()
    {
        // Arrange
        var invalidXmlPath = Path.Combine(_testDataPath, "invalid.xml");
        File.WriteAllText(invalidXmlPath, "This is not valid XML");

        try
        {
            // Act
            Action act = () => _parser.ParseXbrlFile(invalidXmlPath);

            // Assert
            act.Should().Throw<System.Xml.XmlException>();
        }
        finally
        {
            // Cleanup
            if (File.Exists(invalidXmlPath))
            {
                File.Delete(invalidXmlPath);
            }
        }
    }

    #region Edge Case Tests

    [Fact]
    public void ParseXbrlFile_WithContextMissingId_ShouldThrowXbrlParseException()
    {
        var filePath = Path.Combine(_testDataPath, "test-context-no-id.xml");

        Action act = () => _parser.ParseXbrlFile(filePath);

        act.Should().Throw<XbrlParseException>()
            .WithMessage("*missing required 'id' attribute*");
    }

    [Fact]
    public void ParseXbrlFile_WithBadDate_ShouldThrowXbrlParseException()
    {
        var filePath = Path.Combine(_testDataPath, "test-context-bad-date.xml");

        Action act = () => _parser.ParseXbrlFile(filePath);

        act.Should().Throw<XbrlParseException>()
            .WithMessage("*Invalid date value 'not-a-date'*");
    }

    [Fact]
    public void ParseXbrlFile_WithUnitMissingId_ShouldThrowXbrlParseException()
    {
        var filePath = Path.Combine(_testDataPath, "test-unit-no-id.xml");

        Action act = () => _parser.ParseXbrlFile(filePath);

        act.Should().Throw<XbrlParseException>()
            .WithMessage("*Unit*missing required 'id' attribute*");
    }

    [Fact]
    public void ParseXbrlFile_WithMissingEntity_ShouldParseWithNullEntityFields()
    {
        var filePath = Path.Combine(_testDataPath, "test-context-missing-entity.xml");

        var instance = _parser.ParseXbrlFile(filePath);

        instance.Contexts.Should().HaveCount(1);
        var context = instance.Contexts.First();
        context.Id.Should().Be("C1");
        context.EntityValue.Should().BeNull();
        context.EntityScheme.Should().BeNull();
        context.PeriodInstant.Should().Be(new DateTime(2019, 4, 30));
    }

    [Fact]
    public void ParseXbrlFile_WithMissingPeriod_ShouldParseWithNullPeriodFields()
    {
        var filePath = Path.Combine(_testDataPath, "test-context-missing-period.xml");

        var instance = _parser.ParseXbrlFile(filePath);

        instance.Contexts.Should().HaveCount(1);
        var context = instance.Contexts.First();
        context.Id.Should().Be("C1");
        context.EntityValue.Should().Be("1234567890");
        context.PeriodInstant.Should().BeNull();
        context.PeriodStartDate.Should().BeNull();
        context.PeriodEndDate.Should().BeNull();
        context.PeriodForever.Should().BeFalse();
    }

    [Fact]
    public void ParseXbrlFile_WithEmptyFactValue_ShouldParseAsEmptyString()
    {
        var filePath = Path.Combine(_testDataPath, "test-fact-empty-value.xml");

        var instance = _parser.ParseXbrlFile(filePath);

        instance.Facts.Should().HaveCount(1);
        var fact = instance.Facts.First();
        fact.Value.Type.Should().Be(XbrlValueType.String);
        fact.Value.ToString().Should().BeEmpty();
    }

    [Fact]
    public void ParseXbrlFile_WithEmptyReport_ShouldReturnEmptyInstance()
    {
        var filePath = Path.Combine(_testDataPath, "test-empty-report.xml");

        var instance = _parser.ParseXbrlFile(filePath);

        instance.Should().NotBeNull();
        instance.Contexts.Should().BeEmpty();
        instance.Units.Should().BeEmpty();
        instance.Facts.Should().BeEmpty();
    }

    [Fact]
    public void ParseXbrlFile_WithForeverPeriod_ShouldParseForeverFlag()
    {
        var filePath = Path.Combine(_testDataPath, "test-context-forever.xml");

        var instance = _parser.ParseXbrlFile(filePath);

        instance.Contexts.Should().HaveCount(1);
        var context = instance.Contexts.First();
        context.PeriodForever.Should().BeTrue();
        context.PeriodInstant.Should().BeNull();
        context.PeriodStartDate.Should().BeNull();
        context.PeriodEndDate.Should().BeNull();
    }

    #endregion
}
