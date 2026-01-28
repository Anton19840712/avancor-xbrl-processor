using XbrlProcessor.Models.Entities;
using XbrlProcessor.Services;

namespace XbrlProcessor.Commands
{
    /// <summary>
    /// Команда для поиска дубликатов контекстов в XBRL отчетах
    /// </summary>
    public class FindDuplicateContextsCommand : IXbrlCommand
    {
        private readonly Instance _instance1;
        private readonly Instance _instance2;
        private readonly XbrlAnalyzer _analyzer;

        /// <summary>
        /// Конструктор команды поиска дубликатов
        /// </summary>
        /// <param name="instance1">Первый отчет</param>
        /// <param name="instance2">Второй отчет</param>
        /// <param name="analyzer">Сервис анализа XBRL</param>
        public FindDuplicateContextsCommand(Instance instance1, Instance instance2, XbrlAnalyzer analyzer)
        {
            _instance1 = instance1;
            _instance2 = instance2;
            _analyzer = analyzer;
        }

        public void Execute()
        {
            Console.WriteLine("=== Задание 1: Поиск дубликатов контекстов ===\n");

            Console.WriteLine("Report1:");
            var duplicates1 = _analyzer.FindDuplicateContexts(_instance1);
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
            var duplicates2 = _analyzer.FindDuplicateContexts(_instance2);
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

        public string GetName() => "FindDuplicateContexts";

        public string GetDescription() => "Поиск дубликатов контекстов в двух XBRL отчетах";
    }
}
