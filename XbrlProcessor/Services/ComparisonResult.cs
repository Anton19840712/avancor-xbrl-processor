using XbrlProcessor.Models.Entities;

namespace XbrlProcessor.Services;

/// <summary>
/// Результат сравнения двух XBRL отчетов
/// </summary>
public record ComparisonResult
{
    /// <summary>
    /// Факты, отсутствующие во втором отчете
    /// </summary>
    public List<Fact> MissingFacts { get; init; } = [];

    /// <summary>
    /// Новые факты, присутствующие только во втором отчете
    /// </summary>
    public List<Fact> NewFacts { get; init; } = [];

    /// <summary>
    /// Факты с различающимися значениями
    /// </summary>
    public List<FactDifference> ModifiedFacts { get; init; } = [];
}
