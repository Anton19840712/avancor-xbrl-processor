using XbrlProcessor.Services;

Console.WriteLine("=== XBRL Processor - Тестовое задание ===\n");

var parser = new XbrlParser();
var analyzer = new XbrlAnalyzer();
var merger = new XbrlMerger();

// Пути к файлам
var report1Path = "Reports/report1.xbrl";
var report2Path = "Reports/report2.xbrl";
var mergedPath = "Reports/merged_report.xbrl";

// Загружаем отчеты
Console.WriteLine("Загрузка отчетов...");
var instance1 = parser.ParseXbrlFile(report1Path);
var instance2 = parser.ParseXbrlFile(report2Path);

Console.WriteLine($"Report1: {instance1.Contexts.Count} контекстов, {instance1.Units.Count} единиц, {instance1.Facts.Count} фактов");
Console.WriteLine($"Report2: {instance2.Contexts.Count} контекстов, {instance2.Units.Count} единиц, {instance2.Facts.Count} фактов\n");

// Задание 1: Найти дубликаты контекстов
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

// Задание 2: Объединить отчеты
Console.WriteLine("\n\n=== Задание 2: Объединение отчетов ===\n");

var mergedInstance = merger.MergeInstances(instance1, instance2);
Console.WriteLine($"Объединенный отчет: {mergedInstance.Contexts.Count} контекстов, {mergedInstance.Units.Count} единиц, {mergedInstance.Facts.Count} фактов");

merger.SaveToXbrl(mergedInstance, mergedPath, report1Path);
Console.WriteLine($"Объединенный отчет сохранен: {mergedPath}");

// Задание 3: Выявить различия
Console.WriteLine("\n\n=== Задание 3: Выявление различий между отчетами ===\n");

var comparison = analyzer.CompareInstances(instance1, instance2);

Console.WriteLine($"Отсутствующие факты (в report1, но нет в report2): {comparison.MissingFacts.Count}");
if (comparison.MissingFacts.Count > 0 && comparison.MissingFacts.Count <= 10)
{
    foreach (var fact in comparison.MissingFacts)
    {
        Console.WriteLine($"  - {fact.Id} (context: {fact.ContextRef})");
    }
}

Console.WriteLine($"\nНовые факты (нет в report1, но есть в report2): {comparison.NewFacts.Count}");
if (comparison.NewFacts.Count > 0 && comparison.NewFacts.Count <= 10)
{
    foreach (var fact in comparison.NewFacts)
    {
        Console.WriteLine($"  - {fact.Id} (context: {fact.ContextRef})");
    }
}

Console.WriteLine($"\nИзмененные факты (различающиеся значения): {comparison.ModifiedFacts.Count}");
if (comparison.ModifiedFacts.Count > 0 && comparison.ModifiedFacts.Count <= 10)
{
    foreach (var diff in comparison.ModifiedFacts)
    {
        Console.WriteLine($"  - {diff.Fact1.Id} (context: {diff.Fact1.ContextRef})");
        Console.WriteLine($"    Report1: {diff.Fact1.Value}");
        Console.WriteLine($"    Report2: {diff.Fact2.Value}");
    }
}

// Задание 4: XPath запросы
Console.WriteLine("\n\n=== Задание 4: XPath запросы ===\n");

XPathQueries.PrintAllQueries();

Console.WriteLine("\nВыполнение XPath запросов на report1.xbrl:");
XPathQueries.ExecuteQueries(report1Path);

Console.WriteLine("\n=== Выполнение завершено ===");
