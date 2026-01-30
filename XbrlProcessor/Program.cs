using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XbrlProcessor.Configuration;
using XbrlProcessor.Services;
using XbrlProcessor.Commands;

// Загрузка конфигурации
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var settings = new XbrlSettings();
configuration.GetSection("XbrlSettings").Bind(settings);

// Настройка DI контейнера
var services = new ServiceCollection();

// Регистрация настроек
services.AddSingleton(settings);

// Регистрация сервисов
services.AddScoped<XbrlParser>();
services.AddScoped<XbrlAnalyzer>();
services.AddScoped<XbrlMerger>();
services.AddScoped<XbrlSerializer>();
services.AddScoped<XPathQueries>();

var serviceProvider = services.BuildServiceProvider();

Console.WriteLine("=== XBRL Processor - Тестовое задание ===\n");

// Получаем сервисы из DI контейнера
var parser = serviceProvider.GetRequiredService<XbrlParser>();
var analyzer = serviceProvider.GetRequiredService<XbrlAnalyzer>();
var merger = serviceProvider.GetRequiredService<XbrlMerger>();
var serializer = serviceProvider.GetRequiredService<XbrlSerializer>();

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

// Создаем команды используя Command Pattern
var invoker = new CommandInvoker();

// Задание 1: Найти дубликаты контекстов
invoker.AddCommand(new FindDuplicateContextsCommand(instance1, instance2, analyzer));

// Задание 2: Объединить отчеты
invoker.AddCommand(new MergeReportsCommand(instance1, instance2, merger, serializer, mergedPath, report1Path));

// Задание 3: Выявить различия
invoker.AddCommand(new CompareReportsCommand(instance1, instance2, analyzer, settings));

// Задание 4: XPath запросы
invoker.AddCommand(new ExecuteXPathQueriesCommand(report1Path, settings));

// Выполняем все команды
invoker.ExecuteAll();

Console.WriteLine("\n=== Выполнение завершено ===");
