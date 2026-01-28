using XbrlProcessor.Models.Entities;
using XbrlProcessor.Services;

namespace XbrlProcessor.Tasks
{
    /// <summary>
    /// Задание 1: Поиск дубликатов контекстов в XBRL отчетах
    /// </summary>
    public static class Task1DuplicateContexts
    {
        /// <summary>
        /// Выполняет поиск дубликатов контекстов в двух отчетах
        /// </summary>
        /// <param name="instance1">Первый отчет</param>
        /// <param name="instance2">Второй отчет</param>
        /// <param name="analyzer">Сервис анализа XBRL</param>
        public static void Run(Instance instance1, Instance instance2, XbrlAnalyzer analyzer)
        {
            Console.WriteLine("=== Задание 1: Поиск дубликатов контекстов ===\n");

            Console.WriteLine("Report1:");
            var duplicates1 = analyzer.FindDuplicateContexts(instance1);
            if (duplicates1.Count > 0)
            {
                foreach (var group in duplicates1)
                {
                    Console.WriteLine($"Найдены дубликаты: {string.Join(", ", group.Select(c => c.Id))}");
                }
            }
            else
            {
                Console.WriteLine("Дубликаты не найдены");
            }

            Console.WriteLine("\nReport2:");
            var duplicates2 = analyzer.FindDuplicateContexts(instance2);
            if (duplicates2.Count > 0)
            {
                foreach (var group in duplicates2)
                {
                    Console.WriteLine($"Найдены дубликаты: {string.Join(", ", group.Select(c => c.Id))}");
                }
            }
            else
            {
                Console.WriteLine("Дубликаты не найдены");
            }
        }
    }
}
