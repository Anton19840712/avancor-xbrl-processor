using XbrlProcessor.Models.Entities;
using XbrlProcessor.Services;

namespace XbrlProcessor.Tasks
{
    public static class Task2MergeReports
    {
        public static void Run(Instance instance1, Instance instance2, XbrlMerger merger, string mergedPath, string templatePath)
        {
            Console.WriteLine("\n\n=== Задание 2: Объединение отчетов ===\n");

            var mergedInstance = merger.MergeInstances(instance1, instance2);
            Console.WriteLine($"Объединенный отчет: {mergedInstance.Contexts.Count} контекстов, {mergedInstance.Units.Count} единиц, {mergedInstance.Facts.Count} фактов");

            merger.SaveToXbrl(mergedInstance, mergedPath, templatePath);
            Console.WriteLine($"Объединенный отчет сохранен: {mergedPath}");
        }
    }
}
