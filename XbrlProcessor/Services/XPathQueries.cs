using System.Xml.Linq;
using System.Xml.XPath;

namespace XbrlProcessor.Services
{
    public class XPathQueries
    {
        private static readonly XNamespace xbrli = "http://www.xbrl.org/2003/instance";
        private static readonly XNamespace xbrldi = "http://xbrl.org/2006/xbrldi";

        public static void ExecuteQueries(string filePath)
        {
            var doc = XDocument.Load(filePath);
            var navigator = doc.CreateNavigator();

            // Регистрируем namespace для XPath
            var manager = new System.Xml.XmlNamespaceManager(navigator.NameTable);
            manager.AddNamespace("xbrli", xbrli.NamespaceName);
            manager.AddNamespace("xbrldi", xbrldi.NamespaceName);
            manager.AddNamespace("dim-int", "http://www.cbr.ru/xbrl/udr/dim/dim-int");

            Console.WriteLine("\n=== XPath Запросы ===\n");

            // 1. Контексты с периодом instant = "2019-04-30"
            Console.WriteLine("1. Контексты с периодом instant = \"2019-04-30\":");
            Console.WriteLine("XPath: //xbrli:context[xbrli:period/xbrli:instant='2019-04-30']/@id");

            var query1 = "//xbrli:context[xbrli:period/xbrli:instant='2019-04-30']/@id";
            var results1 = navigator.Select(query1, manager);

            int count1 = 0;
            foreach (XPathNavigator result in results1)
            {
                Console.WriteLine($"  - {result.Value}");
                count1++;
            }
            Console.WriteLine($"Найдено: {count1}\n");

            // 2. Контексты со сценарием dimension="dim-int:ID_sobstv_CZBTaxis"
            Console.WriteLine("2. Контексты со сценарием dimension=\"dim-int:ID_sobstv_CZBTaxis\":");
            Console.WriteLine("XPath: //xbrli:context[xbrli:scenario/xbrldi:typedMember[@dimension='dim-int:ID_sobstv_CZBTaxis']]/@id");

            var query2 = "//xbrli:context[xbrli:scenario/xbrldi:typedMember[@dimension='dim-int:ID_sobstv_CZBTaxis']]/@id";
            var results2 = navigator.Select(query2, manager);

            int count2 = 0;
            foreach (XPathNavigator result in results2)
            {
                Console.WriteLine($"  - {result.Value}");
                count2++;
            }
            Console.WriteLine($"Найдено: {count2}\n");

            // 3. Контексты без сценария
            Console.WriteLine("3. Контексты без сценария:");
            Console.WriteLine("XPath: //xbrli:context[not(xbrli:scenario)]/@id");

            var query3 = "//xbrli:context[not(xbrli:scenario)]/@id";
            var results3 = navigator.Select(query3, manager);

            int count3 = 0;
            foreach (XPathNavigator result in results3)
            {
                Console.WriteLine($"  - {result.Value}");
                count3++;
            }
            Console.WriteLine($"Найдено: {count3}\n");
        }

        public static void PrintAllQueries()
        {
            Console.WriteLine("\n=== Список XPath запросов ===\n");

            Console.WriteLine("1. Контексты с периодом instant = \"2019-04-30\":");
            Console.WriteLine("   //xbrli:context[xbrli:period/xbrli:instant='2019-04-30']/@id");
            Console.WriteLine();

            Console.WriteLine("2. Контексты со сценарием dimension=\"dim-int:ID_sobstv_CZBTaxis\":");
            Console.WriteLine("   //xbrli:context[xbrli:scenario/xbrldi:typedMember[@dimension='dim-int:ID_sobstv_CZBTaxis']]/@id");
            Console.WriteLine();

            Console.WriteLine("3. Контексты без сценария:");
            Console.WriteLine("   //xbrli:context[not(xbrli:scenario)]/@id");
            Console.WriteLine();
        }
    }
}
