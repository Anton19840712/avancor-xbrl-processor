using Microsoft.Extensions.Configuration;
using XbrlProcessor.Configuration;
using XbrlProcessor.Services;
using XbrlProcessor.Tasks;

// Загрузка конфигурации
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var settings = new XbrlSettings();
configuration.GetSection("XbrlSettings").Bind(settings);

Console.WriteLine("=== XBRL Processor - Тестовое задание ===\n");

var parser = new XbrlParser(settings);
var analyzer = new XbrlAnalyzer(settings);
var merger = new XbrlMerger(settings);

// Пути к файлам
var report1Path = settings.GetReport1Path();
var report2Path = settings.GetReport2Path();
var mergedPath = settings.GetMergedReportPath();

// Загружаем отчеты
Console.WriteLine("Загрузка отчетов...");
var instance1 = parser.ParseXbrlFile(report1Path);
var instance2 = parser.ParseXbrlFile(report2Path);

Console.WriteLine($"Report1: {instance1.Contexts.Count} контекстов, {instance1.Units.Count} единиц, {instance1.Facts.Count} фактов");
Console.WriteLine($"Report2: {instance2.Contexts.Count} контекстов, {instance2.Units.Count} единиц, {instance2.Facts.Count} фактов\n");

// Задание 1: Найти дубликаты контекстов
Task1DuplicateContexts.Run(instance1, instance2, analyzer);

// Задание 2: Объединить отчеты
Task2MergeReports.Run(instance1, instance2, merger, mergedPath, report1Path);

// Задание 3: Выявить различия
Task3CompareReports.Run(instance1, instance2, analyzer, settings);

// Задание 4: XPath запросы
Task4XPathQueries.Run(report1Path, settings);

Console.WriteLine("\n=== Выполнение завершено ===");
