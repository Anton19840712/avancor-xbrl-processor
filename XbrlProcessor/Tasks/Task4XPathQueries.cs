using XbrlProcessor.Configuration;
using XbrlProcessor.Services;

namespace XbrlProcessor.Tasks
{
    public static class Task4XPathQueries
    {
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
