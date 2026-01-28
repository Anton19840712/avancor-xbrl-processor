using XbrlProcessor.Models.Entities;
using XbrlProcessor.Services;

namespace XbrlProcessor.Tasks
{
    /// <summary>
    /// Задание 2: Объединение двух XBRL отчетов
    /// </summary>
    public static class Task2MergeReports
    {
        /// <summary>
        /// Выполняет объединение двух отчетов и сохранение результата
        /// </summary>
        /// <param name="instance1">Первый отчет</param>
        /// <param name="instance2">Второй отчет</param>
        /// <param name="merger">Сервис объединения XBRL</param>
        /// <param name="mergedPath">Путь для сохранения объединенного отчета</param>
        /// <param name="templatePath">Путь к файлу шаблона</param>
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
