using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XbrlProcessor.Configuration;
using XbrlProcessor.Services;
using XbrlProcessor.Models.Entities;

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
services.AddScoped<XbrlAnalyzer>();
services.AddScoped<XbrlSerializer>();

var serviceProvider = services.BuildServiceProvider();

var totalSw = Stopwatch.StartNew();
var process = Process.GetCurrentProcess();
var memBefore = process.WorkingSet64;

// Определяем степень параллелизма
var parallelism = settings.Processing.MaxDegreeOfParallelism;
if (parallelism == 0)
    parallelism = Environment.ProcessorCount;
var isParallel = parallelism > 1;

Console.WriteLine("=== XBRL Processor - Тестовое задание ===\n");
Console.WriteLine($"Настройки: Parallelism={parallelism}\n");

// Получаем сервисы из DI контейнера
var analyzer = serviceProvider.GetRequiredService<XbrlAnalyzer>();
var serializer = serviceProvider.GetRequiredService<XbrlSerializer>();

// Находим все файлы отчетов
var reportPaths = settings.GetReportPaths();
var mergedPath = settings.GetMergedReportPath();

if (reportPaths.Length == 0)
{
    Console.WriteLine($"Файлы отчетов не найдены в папке '{settings.ReportsPath}'");
    return;
}

// --- Channel<T> pipeline ---
// Producer (парсинг, I/O-bound) → Channel → Consumer(s) (обработка, CPU-bound)
// Backpressure через BoundedCapacity: producer блокируется, если consumer не успевает.
// Пиковая память ≤ BoundedCapacity × Instance + состояние аккумуляторов.

var parser = new XbrlStreamingParser(settings);

var parseSw = Stopwatch.StartNew();
Console.WriteLine($"Загрузка и обработка отчетов (parallelism={parallelism})...\n");

// Bounded channel: максимум parallelism+1 Instance в буфере
var channel = Channel.CreateBounded<(string Path, Instance Instance)>(new BoundedChannelOptions(parallelism + 1)
{
    FullMode = BoundedChannelFullMode.Wait,
    SingleReader = !isParallel,
    SingleWriter = true
});

// Выбор аккумуляторов по степени параллелизма
// parallelism == 1 → обычные (без overhead)
// parallelism > 1  → concurrent/sharded (потокобезопасные)
IMergeAccumulator mergeAcc = isParallel
    ? new ConcurrentMergeAccumulator(settings)
    : new MergeAccumulator(settings);

IGlobalIndexAccumulator globalAcc = isParallel
    ? new ShardedGlobalIndexAccumulator(settings)
    : new GlobalIndexAccumulator(settings);

var xpathQueries = new XPathQueries();
var templatePath = reportPaths[0];
var consoleLock = new object();

// Producer: парсит файлы и пишет в channel
var producerTask = Task.Run(async () =>
{
    var writer = channel.Writer;
    foreach (var path in reportPaths)
    {
        var instance = parser.ParseXbrlFile(path);
        await writer.WriteAsync((path, instance));
    }
    writer.Complete();
});

// Consumer: обрабатывает Instance из channel
Console.WriteLine("=== Задание 1: Поиск дубликатов контекстов ===\n");

void ProcessInstance(string path, Instance instance)
{
    var fileName = Path.GetFileName(path);

    // Задание 1: FindDuplicates
    var duplicates = analyzer.FindDuplicateContexts(instance);

    lock (consoleLock)
    {
        Console.WriteLine($"  {fileName}: {instance.Contexts.Count} контекстов, {instance.Units.Count} единиц, {instance.Facts.Count} фактов");
        if (duplicates.Count > 0)
        {
            foreach (var group in duplicates)
            {
                Console.WriteLine($"    Дубликаты: {string.Join(", ", group.Select(c => c.Id))}");
            }
        }
    }

    // Задание 2: Накопление для merge
    mergeAcc.Add(instance);

    // Задание 3: Накопление для глобального индекса
    globalAcc.Add(fileName, instance);

    // Задание 4: C# фильтры (вместо XPath)
    xpathQueries.ExecuteQueriesOnInstance(instance);

    // Instance освобождается после итерации
}

Task consumerTask;
if (isParallel)
{
    // N consumer tasks читают из одного channel
    var consumers = new Task[parallelism];
    for (var i = 0; i < parallelism; i++)
    {
        consumers[i] = Task.Run(async () =>
        {
            var reader = channel.Reader;
            await foreach (var (path, instance) in reader.ReadAllAsync())
            {
                ProcessInstance(path, instance);
            }
        });
    }
    consumerTask = Task.WhenAll(consumers);
}
else
{
    consumerTask = Task.Run(async () =>
    {
        var reader = channel.Reader;
        await foreach (var (path, instance) in reader.ReadAllAsync())
        {
            ProcessInstance(path, instance);
        }
    });
}

// Ждём завершения обоих
await Task.WhenAll(producerTask, consumerTask);

parseSw.Stop();
Console.WriteLine($"\nОбработано отчетов: {reportPaths.Length} за {parseSw.ElapsedMilliseconds} мс\n");

// Задание 2: Финализация merge
Console.WriteLine("\n=== Задание 2: Объединение отчетов ===\n");
var mergedInstance = mergeAcc.Build();
Console.WriteLine($"Объединенный отчет: {mergedInstance.Contexts.Count} контекстов, {mergedInstance.Units.Count} единиц, {mergedInstance.Facts.Count} фактов");
serializer.SaveToXbrl(mergedInstance, mergedPath, templatePath);
Console.WriteLine($"Объединенный отчет сохранен: {mergedPath}");

// Задание 3: Финализация глобального сравнения
if (reportPaths.Length >= 2)
{
    Console.WriteLine("\n\n=== Задание 3: Глобальное сравнение отчетов ===\n");
    var result = globalAcc.Build();

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
