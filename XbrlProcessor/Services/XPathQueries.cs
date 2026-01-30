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

    private record QueryDescriptor(string Name, string XPath);

    private QueryDescriptor[] GetQueries() =>
    [
        new("Контексты с периодом instant = \"2019-04-30\"", settings.XPathQueries.ContextsWithInstant),
        new("Контексты со сценарием dimension=\"dim-int:ID_sobstv_CZBTaxis\"", settings.XPathQueries.ContextsWithDimension),
        new("Контексты без сценария", settings.XPathQueries.ContextsWithoutScenario)
    ];

    /// <summary>
    /// Выполняет все настроенные XPath запросы к XBRL файлу
    /// </summary>
    /// <param name="filePath">Путь к XBRL файлу</param>
    public void ExecuteQueries(string filePath)
    {
        var doc = XDocument.Load(filePath);
        var navigator = doc.CreateNavigator();

        var manager = new System.Xml.XmlNamespaceManager(navigator.NameTable);
        manager.AddNamespace("xbrli", _xbrli.NamespaceName);
        manager.AddNamespace("xbrldi", _xbrldi.NamespaceName);
        manager.AddNamespace("dim-int", settings.XmlNamespaces.DimInt);

        Console.WriteLine("\n=== XPath Запросы ===\n");

        var queries = GetQueries();
        for (var i = 0; i < queries.Length; i++)
        {
            var query = queries[i];
            Console.WriteLine($"{i + 1}. {query.Name}:");
            Console.WriteLine($"XPath: {query.XPath}");

            var results = navigator.Select(query.XPath, manager);
            var count = 0;
            foreach (XPathNavigator result in results)
            {
                Console.WriteLine($"  - {result.Value}");
                count++;
            }
            Console.WriteLine($"Найдено: {count}\n");
        }
    }

    /// <summary>
    /// Выводит список всех настроенных XPath запросов в консоль
    /// </summary>
    public void PrintAllQueries()
    {
        Console.WriteLine("\n=== Список XPath запросов ===\n");

        var queries = GetQueries();
        for (var i = 0; i < queries.Length; i++)
        {
            var query = queries[i];
            Console.WriteLine($"{i + 1}. {query.Name}:");
            Console.WriteLine($"   {query.XPath}");
            Console.WriteLine();
        }
    }
}
