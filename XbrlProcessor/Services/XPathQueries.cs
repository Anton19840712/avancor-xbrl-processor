using System.Xml.Linq;
using System.Xml.XPath;
using XbrlProcessor.Configuration;

namespace XbrlProcessor.Services;

/// <summary>
/// Сервис для выполнения XPath запросов к XBRL файлам
/// </summary>
/// <param name="settings">Настройки приложения</param>
public class XPathQueries(XbrlSettings settings)
{
    private readonly XNamespace _xbrli = settings.XmlNamespaces.Xbrli;
    private readonly XNamespace _xbrldi = settings.XmlNamespaces.Xbrldi;

        /// <summary>
        /// Выполняет все настроенные XPath запросы к XBRL файлу
        /// </summary>
        /// <param name="filePath">Путь к XBRL файлу</param>
        public void ExecuteQueries(string filePath)
        {
            var doc = XDocument.Load(filePath);
            var navigator = doc.CreateNavigator();

            // Регистрируем namespace для XPath
            var manager = new System.Xml.XmlNamespaceManager(navigator.NameTable);
            manager.AddNamespace("xbrli", _xbrli.NamespaceName);
            manager.AddNamespace("xbrldi", _xbrldi.NamespaceName);
            manager.AddNamespace("dim-int", settings.XmlNamespaces.DimInt);

            Console.WriteLine("\n=== XPath Запросы ===\n");

            // 1. Контексты с периодом instant = "2019-04-30"
            Console.WriteLine("1. Контексты с периодом instant = \"2019-04-30\":");
            Console.WriteLine($"XPath: {settings.XPathQueries.ContextsWithInstant}");

            var results1 = navigator.Select(settings.XPathQueries.ContextsWithInstant, manager);

            int count1 = 0;
            foreach (XPathNavigator result in results1)
            {
                Console.WriteLine($"  - {result.Value}");
                count1++;
            }
            Console.WriteLine($"Найдено: {count1}\n");

            // 2. Контексты со сценарием dimension="dim-int:ID_sobstv_CZBTaxis"
            Console.WriteLine("2. Контексты со сценарием dimension=\"dim-int:ID_sobstv_CZBTaxis\":");
            Console.WriteLine($"XPath: {settings.XPathQueries.ContextsWithDimension}");

            var results2 = navigator.Select(settings.XPathQueries.ContextsWithDimension, manager);

            int count2 = 0;
            foreach (XPathNavigator result in results2)
            {
                Console.WriteLine($"  - {result.Value}");
                count2++;
            }
            Console.WriteLine($"Найдено: {count2}\n");

            // 3. Контексты без сценария
            Console.WriteLine("3. Контексты без сценария:");
            Console.WriteLine($"XPath: {settings.XPathQueries.ContextsWithoutScenario}");

            var results3 = navigator.Select(settings.XPathQueries.ContextsWithoutScenario, manager);

            int count3 = 0;
            foreach (XPathNavigator result in results3)
            {
                Console.WriteLine($"  - {result.Value}");
                count3++;
            }
            Console.WriteLine($"Найдено: {count3}\n");
        }

        /// <summary>
        /// Выводит список всех настроенных XPath запросов в консоль
        /// </summary>
        public void PrintAllQueries()
        {
            Console.WriteLine("\n=== Список XPath запросов ===\n");

            Console.WriteLine("1. Контексты с периодом instant = \"2019-04-30\":");
            Console.WriteLine($"   {settings.XPathQueries.ContextsWithInstant}");
            Console.WriteLine();

            Console.WriteLine("2. Контексты со сценарием dimension=\"dim-int:ID_sobstv_CZBTaxis\":");
            Console.WriteLine($"   {settings.XPathQueries.ContextsWithDimension}");
            Console.WriteLine();

            Console.WriteLine("3. Контексты без сценария:");
            Console.WriteLine($"   {settings.XPathQueries.ContextsWithoutScenario}");
            Console.WriteLine();
        }
    }
