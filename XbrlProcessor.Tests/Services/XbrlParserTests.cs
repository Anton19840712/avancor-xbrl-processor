using System.Xml.Linq;
using FluentAssertions;
using XbrlProcessor.Configuration;
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
        fact.Value.Should().Be("1000000.50");
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
}
