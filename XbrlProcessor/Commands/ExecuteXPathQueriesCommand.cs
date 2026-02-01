using XbrlProcessor.Configuration;
using XbrlProcessor.Services;

namespace XbrlProcessor.Commands;

/// <summary>
/// Команда для выполнения XPath запросов ко всем XBRL файлам
/// </summary>
/// <param name="reportPaths">Пути к файлам отчетов</param>
/// <param name="settings">Настройки приложения</param>
public class ExecuteXPathQueriesCommand(IReadOnlyList<string> reportPaths, XbrlSettings settings) : IXbrlCommand
{
    #region IXbrlCommand Implementation

    public void Execute()
    {
        Console.WriteLine("\n\n=== Задание 4: XPath запросы ===\n");

        var xpathQueries = new XPathQueries(settings);
        xpathQueries.PrintAllQueries();

        foreach (var reportPath in reportPaths)
        {
            Console.WriteLine($"\n--- {Path.GetFileName(reportPath)} ---");
            xpathQueries.ExecuteQueries(reportPath);
        }
    }

    public string GetName() => "ExecuteXPathQueries";

    public string GetDescription() => "Выполнение XPath запросов ко всем XBRL файлам";

    #endregion
}
