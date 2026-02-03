using System.Diagnostics;
using FluentAssertions;
using XbrlProcessor.Configuration;
using XbrlProcessor.Models.Entities;
using XbrlProcessor.Services;

namespace XbrlProcessor.Tests.Services;

/// <summary>
/// Сравнение peak memory: batch (все файлы в памяти) vs process-and-release (по одному).
/// </summary>
public class ProcessAndReleaseBenchmarkTest
{
    private readonly XbrlSettings _settings = new();

    [Fact]
    public void CompareMemoryUsage_BatchVsProcessAndRelease()
    {
        var reportsDir = Path.Combine(
            Directory.GetCurrentDirectory(), "..", "..", "..", "..",
            "XbrlProcessor", "Reports");

        var files = Directory.GetFiles(reportsDir, "*.xbrl", SearchOption.AllDirectories)
            .Where(f => !f.Contains("merged"))
            .OrderBy(f => f)
            .ToArray();

        files.Should().NotBeEmpty("need report files for benchmark");

        var parser = new XbrlStreamingParser(_settings);
        var analyzer = new XbrlAnalyzer(_settings);

        // === Batch approach (old): load all, then process ===
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var batchMemBefore = GC.GetTotalMemory(true);
        long batchPeakMem = 0;

        var sw = Stopwatch.StartNew();
        var allInstances = new List<(string Name, Instance Instance)>();
        foreach (var file in files)
        {
            allInstances.Add((Path.GetFileName(file), parser.ParseXbrlFile(file)));
        }

        // Peak = all instances loaded simultaneously
        batchPeakMem = GC.GetTotalMemory(true) - batchMemBefore;

        // Process: FindDuplicates + Merge + GlobalCompare
        var merger = new XbrlMerger(_settings);
        var merged1 = merger.MergeInstances(allInstances.Select(i => i.Instance).ToList());
        var compare1 = analyzer.BuildGlobalIndex(allInstances);
        var dupCount1 = 0;
        foreach (var (_, inst) in allInstances)
            dupCount1 += analyzer.FindDuplicateContexts(inst).Count;
        sw.Stop();
        var batchTime = sw.ElapsedMilliseconds;

        // Release batch data
        allInstances.Clear();
        allInstances = null!;
        GC.Collect();

        // === Process-and-release approach (new): one at a time ===
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var streamMemBefore = GC.GetTotalMemory(true);
        long streamPeakMem = 0;

        sw.Restart();
        var mergeAcc = new MergeAccumulator(_settings);
        var globalAcc = new GlobalIndexAccumulator(_settings);
        var dupCount2 = 0;

        foreach (var file in files)
        {
            var instance = parser.ParseXbrlFile(file);

            // FindDuplicates — immediate
            dupCount2 += analyzer.FindDuplicateContexts(instance).Count;

            // Accumulate
            mergeAcc.Add(instance);
            globalAcc.Add(Path.GetFileName(file), instance);

            // Track peak before GC reclaims the instance
            var currentMem = GC.GetTotalMemory(false) - streamMemBefore;
            if (currentMem > streamPeakMem) streamPeakMem = currentMem;

            // Instance released after loop iteration
        }

        // Also measure after all processing (accumulator-only state)
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var streamFinalMem = GC.GetTotalMemory(true) - streamMemBefore;
        var merged2 = mergeAcc.Build();
        var compare2 = globalAcc.Build();
        sw.Stop();
        var streamTime = sw.ElapsedMilliseconds;

        // Verify results are equivalent
        merged1.Contexts.Count.Should().Be(merged2.Contexts.Count, "merge context count must match");
        merged1.Units.Count.Should().Be(merged2.Units.Count, "merge unit count must match");
        merged1.Facts.Count.Should().Be(merged2.Facts.Count, "merge fact count must match");
        compare1.TotalUniqueFactKeys.Should().Be(compare2.TotalUniqueFactKeys, "global index key count must match");
        compare1.ConsistentFacts.Count.Should().Be(compare2.ConsistentFacts.Count);
        compare1.ModifiedFacts.Count.Should().Be(compare2.ModifiedFacts.Count);
        compare1.PartialFacts.Count.Should().Be(compare2.PartialFacts.Count);
        dupCount1.Should().Be(dupCount2, "duplicate count must match");

        // Output results
        var totalSize = files.Sum(f => new FileInfo(f).Length);
        Console.WriteLine();
        Console.WriteLine($"=== Process-and-Release Benchmark ({files.Length} files, {totalSize / 1024:N0} KB) ===");
        Console.WriteLine($"{"Metric",-25} {"Batch (old)",-20} {"Stream (new)",-20} {"Improvement",-15}");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine($"{"Time (ms)",-25} {batchTime,-20} {streamTime,-20} {(double)batchTime / Math.Max(1, streamTime):F1}x");
        Console.WriteLine($"{"Peak memory (KB)",-25} {batchPeakMem / 1024,-20:N0} {streamPeakMem / 1024,-20:N0} {(batchPeakMem > 0 && streamPeakMem > 0 ? $"{(1.0 - (double)streamPeakMem / batchPeakMem) * 100:F0}% less" : "N/A"),-15}");
        Console.WriteLine($"{"Final mem (acc only, KB)",-25} {"N/A",-20} {streamFinalMem / 1024,-20:N0} {"(acc state)",-15}");
        Console.WriteLine($"{"Merged contexts",-25} {merged1.Contexts.Count,-20} {merged2.Contexts.Count,-20}");
        Console.WriteLine($"{"Merged facts",-25} {merged1.Facts.Count,-20} {merged2.Facts.Count,-20}");
        Console.WriteLine($"{"Global unique keys",-25} {compare1.TotalUniqueFactKeys,-20} {compare2.TotalUniqueFactKeys,-20}");
        Console.WriteLine();
    }
}
