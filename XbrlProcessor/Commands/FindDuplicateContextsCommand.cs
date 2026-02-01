using XbrlProcessor.Models.Entities;
using XbrlProcessor.Services;

namespace XbrlProcessor.Commands;

/// <summary>
/// Команда для поиска дубликатов контекстов в XBRL отчетах
/// </summary>
/// <param name="reports">Список отчетов (имя файла, экземпляр)</param>
/// <param name="analyzer">Сервис анализа XBRL</param>
public class FindDuplicateContextsCommand(IReadOnlyList<(string Name, Instance Instance)> reports, XbrlAnalyzer analyzer) : IXbrlCommand
{
    #region IXbrlCommand Implementation

    public void Execute()
    {
        Console.WriteLine("=== Задание 1: Поиск дубликатов контекстов ===\n");

        foreach (var (name, instance) in reports)
        {
            Console.WriteLine($"{name}:");
            var duplicates = analyzer.FindDuplicateContexts(instance);
            if (duplicates.Count > 0)
            {
                foreach (var group in duplicates)
                {
                    Console.WriteLine($"Найдены дубликаты: {string.Join(", ", group.Select(c => c.Id))}");
                }
            }
            else
            {
                Console.WriteLine("Дубликаты не найдены");
            }
            Console.WriteLine();
        }
    }

    public string GetName() => "FindDuplicateContexts";

    public string GetDescription() => "Поиск дубликатов контекстов в XBRL отчетах";

    #endregion
}
