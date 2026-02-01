using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XbrlProcessor.Configuration;
using XbrlProcessor.Services;
using XbrlProcessor.Models.Entities;
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
services.AddScoped<XbrlStreamingParser>();
services.AddScoped<XbrlAnalyzer>();
services.AddScoped<XbrlMerger>();
services.AddScoped<XbrlSerializer>();
services.AddScoped<XPathQueries>();

var serviceProvider = services.BuildServiceProvider();

var totalSw = Stopwatch.StartNew();
var process = Process.GetCurrentProcess();
var memBefore = process.WorkingSet64;

Console.WriteLine("=== XBRL Processor - Тестовое задание ===\n");
Console.WriteLine($"Настройки: ParserMode={settings.Processing.ParserMode}, Parallelism={settings.Processing.MaxDegreeOfParallelism}, BatchSize={settings.Processing.BatchSize}\n");

// Получаем сервисы из DI контейнера
var parser = serviceProvider.GetRequiredService<XbrlParser>();
var analyzer = serviceProvider.GetRequiredService<XbrlAnalyzer>();
var merger = serviceProvider.GetRequiredService<XbrlMerger>();
var serializer = serviceProvider.GetRequiredService<XbrlSerializer>();

// Находим все файлы отчетов
var reportPaths = settings.GetReportPaths();
var mergedPath = settings.GetMergedReportPath();

if (reportPaths.Length == 0)
{
    Console.WriteLine($"Файлы отчетов не найдены в папке '{settings.ReportsPath}'");
    return;
}

// Загружаем все отчеты
var parseSw = Stopwatch.StartNew();
Console.WriteLine($"Загрузка отчетов ({settings.Processing.ParserMode})...");
var instances = new List<(string Path, Instance Instance)>();

var parallelism = settings.Processing.MaxDegreeOfParallelism == 0
    ? Environment.ProcessorCount
    : settings.Processing.MaxDegreeOfParallelism;

Instance ParseFile(string path) => settings.Processing.ParserMode switch
{
    ParserMode.Streaming => new XbrlStreamingParser(settings).ParseXbrlFile(path),
    ParserMode.XDocument => new XbrlParser(settings).ParseXbrlFile(path),
    _ => throw new InvalidOperationException($"Unknown ParserMode: {settings.Processing.ParserMode}")
};

var batchSize = settings.Processing.BatchSize > 0
    ? settings.Processing.BatchSize
    : reportPaths.Length;

for (var offset = 0; offset < reportPaths.Length; offset += batchSize)
{
    var batch = reportPaths[offset..Math.Min(offset + batchSize, reportPaths.Length)];

    if (parallelism > 1)
    {
        var parsed = new (string Path, Instance Instance)[batch.Length];
        Parallel.For(0, batch.Length, new ParallelOptions { MaxDegreeOfParallelism = parallelism }, i =>
        {
            parsed[i] = (batch[i], ParseFile(batch[i]));
        });
        instances.AddRange(parsed);
    }
    else
    {
        foreach (var path in batch)
        {
            instances.Add((path, ParseFile(path)));
        }
    }
}

foreach (var (path, instance) in instances)
{
    Console.WriteLine($"  {Path.GetFileName(path)}: {instance.Contexts.Count} контекстов, {instance.Units.Count} единиц, {instance.Facts.Count} фактов");
}
parseSw.Stop();
Console.WriteLine($"Загружено отчетов: {instances.Count} за {parseSw.ElapsedMilliseconds} мс\n");

// Создаем команды используя Command Pattern
var invoker = new CommandInvoker();

// Задание 1: Найти дубликаты контекстов
invoker.AddCommand(new FindDuplicateContextsCommand(instances.Select(i => (Path.GetFileName(i.Path), i.Instance)).ToList(), analyzer));

// Задание 2: Объединить отчеты
invoker.AddCommand(new MergeReportsCommand(instances.Select(i => i.Instance).ToList(), merger, serializer, mergedPath, instances[0].Path));

// Задание 3: Сравнение отчетов (глобальный индекс)
if (instances.Count >= 2)
{
    invoker.AddCommand(new GlobalCompareCommand(instances.Select(i => (Path.GetFileName(i.Path), i.Instance)).ToList(), analyzer, settings));
}

// Задание 4: XPath запросы (на всех отчетах)
invoker.AddCommand(new ExecuteXPathQueriesCommand(instances.Select(i => i.Path).ToList(), settings));

// Выполняем все команды
invoker.ExecuteAll();

totalSw.Stop();
process.Refresh();
var memAfter = process.WorkingSet64;

Console.WriteLine("\n=== METRICS ===");
Console.WriteLine($"PARSE_MS={parseSw.ElapsedMilliseconds}");
Console.WriteLine($"TOTAL_MS={totalSw.ElapsedMilliseconds}");
Console.WriteLine($"MEM_BEFORE_MB={memBefore / 1024.0 / 1024.0:F1}");
Console.WriteLine($"MEM_AFTER_MB={memAfter / 1024.0 / 1024.0:F1}");
Console.WriteLine($"MEM_DELTA_MB={(memAfter - memBefore) / 1024.0 / 1024.0:F1}");
Console.WriteLine("\n=== Выполнение завершено ===");
