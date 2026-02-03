using XbrlProcessor.Models.Entities;

namespace XbrlProcessor.Services;

/// <summary>
/// Интерфейс аккумулятора для объединения XBRL-отчетов.
/// Позволяет выбирать между обычной и потокобезопасной реализациями.
/// </summary>
public interface IMergeAccumulator
{
    /// <summary>
    /// Добавляет один Instance в аккумулятор.
    /// </summary>
    void Add(Instance instance);

    /// <summary>
    /// Возвращает объединённый Instance.
    /// </summary>
    Instance Build();
}
