using XbrlProcessor.Configuration;
using XbrlProcessor.Models.Entities;
using XbrlProcessor.Services;

namespace XbrlProcessor.Commands;

/// <summary>
/// Команда глобального сравнения N отчетов через единый индекс — O(n*m)
/// </summary>
/// <param name="reports">Список отчетов (имя файла, экземпляр)</param>
/// <param name="analyzer">Сервис анализа XBRL</param>
/// <param name="settings">Настройки приложения</param>
public class GlobalCompareCommand(IReadOnlyList<(string Name, Instance Instance)> reports,
    XbrlAnalyzer analyzer, XbrlSettings settings) : IXbrlCommand
{
    public void Execute()
    {
        Console.WriteLine("\n\n=== Задание 3: Глобальное сравнение отчетов ===\n");

        var result = analyzer.BuildGlobalIndex(reports);

        Console.WriteLine($"Файлов в сравнении: {result.TotalFiles}");
        Console.WriteLine($"Уникальных ключей фактов: {result.TotalUniqueFactKeys}");
        Console.WriteLine($"Консистентные факты (одинаковые во всех файлах): {result.ConsistentFacts.Count}");
        Console.WriteLine($"Модифицированные факты (разные значения): {result.ModifiedFacts.Count}");
        Console.WriteLine($"Частичные факты (не во всех файлах): {result.PartialFacts.Count}");

        if (result.ModifiedFacts.Count > 0)
        {
            var displayCount = Math.Min(result.ModifiedFacts.Count, settings.MaxDisplayedFacts);
            Console.WriteLine($"\nПримеры модифицированных фактов (первые {displayCount}):");
            foreach (var entry in result.ModifiedFacts.Take(displayCount))
            {
                Console.WriteLine($"  - {entry.FactKey}:");
                foreach (var (fileName, fact) in entry.ValuesByFile)
                {
                    Console.WriteLine($"      {fileName}: {fact.Value}");
                }
            }
        }

        if (result.PartialFacts.Count > 0)
        {
            var displayCount = Math.Min(result.PartialFacts.Count, settings.MaxDisplayedFacts);
            Console.WriteLine($"\nПримеры частичных фактов (первые {displayCount}):");
            foreach (var entry in result.PartialFacts.Take(displayCount))
            {
                Console.WriteLine($"  - {entry.FactKey}: присутствует в {entry.FileCount}/{result.TotalFiles} файлах");
            }
        }
    }

    public string GetName() => "GlobalCompare";

    public string GetDescription() => "Глобальное сравнение N отчетов через единый индекс O(n*m)";
}
