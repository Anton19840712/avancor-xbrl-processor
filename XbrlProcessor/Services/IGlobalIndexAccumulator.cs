using XbrlProcessor.Models.Entities;

namespace XbrlProcessor.Services;

/// <summary>
/// Интерфейс аккумулятора для глобального сравнения XBRL-отчетов.
/// Позволяет выбирать между обычной и шардированной реализациями.
/// </summary>
public interface IGlobalIndexAccumulator
{
    /// <summary>
    /// Добавляет один отчёт в глобальный индекс.
    /// </summary>
    void Add(string reportName, Instance instance);

    /// <summary>
    /// Формирует результат глобального сравнения.
    /// </summary>
    GlobalComparisonResult Build();
}
