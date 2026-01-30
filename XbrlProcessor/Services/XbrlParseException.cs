namespace XbrlProcessor.Services;

/// <summary>
/// Ошибка парсинга XBRL документа с указанием файла и контекста проблемы
/// </summary>
public class XbrlParseException : Exception
{
    public string FilePath { get; }

    public XbrlParseException(string filePath, string message)
        : base($"XBRL parse error in '{filePath}': {message}")
    {
        FilePath = filePath;
    }

    public XbrlParseException(string filePath, string message, Exception innerException)
        : base($"XBRL parse error in '{filePath}': {message}", innerException)
    {
        FilePath = filePath;
    }
}
