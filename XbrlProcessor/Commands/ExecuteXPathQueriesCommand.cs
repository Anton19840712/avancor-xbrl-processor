using XbrlProcessor.Configuration;
using XbrlProcessor.Services;

namespace XbrlProcessor.Commands;

/// <summary>
/// Команда для выполнения XPath запросов к XBRL файлу
/// </summary>
/// <param name="reportPath">Путь к файлу отчета</param>
/// <param name="settings">Настройки приложения</param>
public class ExecuteXPathQueriesCommand(string reportPath, XbrlSettings settings) : IXbrlCommand
{

        #region IXbrlCommand Implementation

        public void Execute()
        {
            Console.WriteLine("\n\n=== Задание 4: XPath запросы ===\n");

            var xpathQueries = new XPathQueries(settings);
            xpathQueries.PrintAllQueries();

            Console.WriteLine("\nВыполнение XPath запросов на report1.xbrl:");
            xpathQueries.ExecuteQueries(reportPath);
        }

        public string GetName() => "ExecuteXPathQueries";

        public string GetDescription() => "Выполнение XPath запросов к XBRL файлу";

        #endregion
    }
