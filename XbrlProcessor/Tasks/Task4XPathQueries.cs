using XbrlProcessor.Configuration;
using XbrlProcessor.Services;

namespace XbrlProcessor.Tasks
{
    /// <summary>
    /// Задание 4: Выполнение XPath запросов к XBRL файлу
    /// </summary>
    public static class Task4XPathQueries
    {
        /// <summary>
        /// Выполняет XPath запросы к указанному отчету
        /// </summary>
        /// <param name="report1Path">Путь к файлу отчета</param>
        /// <param name="settings">Настройки приложения</param>
        public static void Run(string report1Path, XbrlSettings settings)
        {
            Console.WriteLine("\n\n=== Задание 4: XPath запросы ===\n");

            var xpathQueries = new XPathQueries(settings);
            xpathQueries.PrintAllQueries();

            Console.WriteLine("\nВыполнение XPath запросов на report1.xbrl:");
            xpathQueries.ExecuteQueries(report1Path);
        }
    }
}
