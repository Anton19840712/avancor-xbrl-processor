using XbrlProcessor.Models.Entities;
using XbrlProcessor.Services;

namespace XbrlProcessor.Tasks
{
    public static class Task1DuplicateContexts
    {
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
