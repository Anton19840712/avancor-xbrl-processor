using XbrlProcessor.Configuration;
using XbrlProcessor.Services;

namespace XbrlProcessor.Commands
{
    /// <summary>
    /// Команда для выполнения XPath запросов к XBRL файлу
    /// </summary>
    public class ExecuteXPathQueriesCommand : IXbrlCommand
    {
        #region Fields

        private readonly string _reportPath;
        private readonly XbrlSettings _settings;

        #endregion

        #region Constructor

        /// <summary>
        /// Конструктор команды выполнения XPath запросов
        /// </summary>
        /// <param name="reportPath">Путь к файлу отчета</param>
        /// <param name="settings">Настройки приложения</param>
        public ExecuteXPathQueriesCommand(string reportPath, XbrlSettings settings)
        {
            _reportPath = reportPath;
            _settings = settings;
        }

        #endregion

        #region IXbrlCommand Implementation

        public void Execute()
        {
            Console.WriteLine("\n\n=== Задание 4: XPath запросы ===\n");

            var xpathQueries = new XPathQueries(_settings);
            xpathQueries.PrintAllQueries();

            Console.WriteLine("\nВыполнение XPath запросов на report1.xbrl:");
            xpathQueries.ExecuteQueries(_reportPath);
        }

        public string GetName() => "ExecuteXPathQueries";

        public string GetDescription() => "Выполнение XPath запросов к XBRL файлу";

        #endregion
    }
}
