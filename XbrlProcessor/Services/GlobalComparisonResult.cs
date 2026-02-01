using XbrlProcessor.Models.Entities;

namespace XbrlProcessor.Services;

/// <summary>
/// Запись глобального индекса: ключ факта → значения по файлам
/// </summary>
public record FactIndexEntry
{
    /// <summary>
    /// Ключ факта (Id|ContextRef)
    /// </summary>
    public required string FactKey { get; init; }

    /// <summary>
    /// Значения факта по файлам: имя файла → факт
    /// </summary>
    public required Dictionary<string, Fact> ValuesByFile { get; init; }

    /// <summary>
    /// В скольких файлах присутствует факт
    /// </summary>
    public int FileCount => ValuesByFile.Count;

    /// <summary>
    /// Все ли значения одинаковы (семантически)
    /// </summary>
    public bool IsConsistent
    {
        get
        {
            var values = ValuesByFile.Values.ToList();
            if (values.Count <= 1) return true;
            var first = values[0].Value;
            return values.Skip(1).All(f => first.SemanticallyEquals(f.Value));
        }
    }
}

/// <summary>
/// Результат глобального сравнения N отчетов через единый индекс — O(n*m)
/// </summary>
public record GlobalComparisonResult
{
    /// <summary>
    /// Общее количество файлов в сравнении
    /// </summary>
    public required int TotalFiles { get; init; }

    /// <summary>
    /// Общее количество уникальных ключей фактов
    /// </summary>
    public required int TotalUniqueFactKeys { get; init; }

    /// <summary>
    /// Факты, присутствующие во всех файлах с одинаковым значением
    /// </summary>
    public required IReadOnlyList<FactIndexEntry> ConsistentFacts { get; init; }

    /// <summary>
    /// Факты, присутствующие во всех файлах, но с различающимися значениями
    /// </summary>
    public required IReadOnlyList<FactIndexEntry> ModifiedFacts { get; init; }

    /// <summary>
    /// Факты, присутствующие не во всех файлах (частичное покрытие)
    /// </summary>
    public required IReadOnlyList<FactIndexEntry> PartialFacts { get; init; }
}
