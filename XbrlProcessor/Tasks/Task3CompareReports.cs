using XbrlProcessor.Configuration;
using XbrlProcessor.Models.Entities;
using XbrlProcessor.Services;

namespace XbrlProcessor.Tasks
{
    public static class Task3CompareReports
    {
        public static void Run(Instance instance1, Instance instance2, XbrlAnalyzer analyzer, XbrlSettings settings)
        {
            Console.WriteLine("\n\n=== Задание 3: Выявление различий между отчетами ===\n");

            var comparison = analyzer.CompareInstances(instance1, instance2);

            Console.WriteLine($"Отсутствующие факты (в report1, но нет в report2): {comparison.MissingFacts.Count}");
            if (comparison.MissingFacts.Count > 0 && comparison.MissingFacts.Count <= settings.MaxDisplayedFacts)
            {
                foreach (var fact in comparison.MissingFacts)
                {
                    Console.WriteLine($"  - {fact.Id} (context: {fact.ContextRef})");
                }
            }

            Console.WriteLine($"\nНовые факты (нет в report1, но есть в report2): {comparison.NewFacts.Count}");
            if (comparison.NewFacts.Count > 0 && comparison.NewFacts.Count <= settings.MaxDisplayedFacts)
            {
                foreach (var fact in comparison.NewFacts)
                {
                    Console.WriteLine($"  - {fact.Id} (context: {fact.ContextRef})");
                }
            }

            Console.WriteLine($"\nИзмененные факты (различающиеся значения): {comparison.ModifiedFacts.Count}");
            if (comparison.ModifiedFacts.Count > 0 && comparison.ModifiedFacts.Count <= settings.MaxDisplayedFacts)
            {
                foreach (var diff in comparison.ModifiedFacts)
                {
                    Console.WriteLine($"  - {diff.Fact1.Id} (context: {diff.Fact1.ContextRef})");
                    Console.WriteLine($"    Report1: {diff.Fact1.Value}");
                    Console.WriteLine($"    Report2: {diff.Fact2.Value}");
                }
            }
        }
    }
}
