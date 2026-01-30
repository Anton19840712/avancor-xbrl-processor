using XbrlProcessor.Models.Entities;
using XbrlProcessor.Services;

namespace XbrlProcessor.Commands;

/// <summary>
/// Команда для объединения двух XBRL отчетов
/// </summary>
/// <param name="instance1">Первый отчет</param>
/// <param name="instance2">Второй отчет</param>
/// <param name="merger">Сервис объединения XBRL</param>
/// <param name="serializer">Сервис сериализации XBRL</param>
/// <param name="mergedPath">Путь для сохранения объединенного отчета</param>
/// <param name="templatePath">Путь к файлу шаблона</param>
public class MergeReportsCommand(Instance instance1, Instance instance2, XbrlMerger merger,
    XbrlSerializer serializer, string mergedPath, string templatePath) : IXbrlCommand
{
    public void Execute()
    {
        Console.WriteLine("\n\n=== Задание 2: Объединение отчетов ===\n");

        var mergedInstance = merger.MergeInstances(instance1, instance2);
        Console.WriteLine($"Объединенный отчет: {mergedInstance.Contexts.Count} контекстов, {mergedInstance.Units.Count} единиц, {mergedInstance.Facts.Count} фактов");

        serializer.SaveToXbrl(mergedInstance, mergedPath, templatePath);
        Console.WriteLine($"Объединенный отчет сохранен: {mergedPath}");
    }

    public string GetName() => "MergeReports";

    public string GetDescription() => "Объединение двух XBRL отчетов с удалением дубликатов";
}
