namespace XbrlProcessor.Commands;

/// <summary>
/// Интерфейс команды для выполнения операций над XBRL данными
/// </summary>
public interface IXbrlCommand
{
    /// <summary>
    /// Выполняет команду
    /// </summary>
    void Execute();

    /// <summary>
    /// Возвращает название команды
    /// </summary>
    string GetName();

    /// <summary>
    /// Возвращает описание команды
    /// </summary>
    string GetDescription();
}
