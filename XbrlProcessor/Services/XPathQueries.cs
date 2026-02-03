using XbrlProcessor.Models.Entities;

namespace XbrlProcessor.Services;

/// <summary>
/// Сервис для выполнения запросов к XBRL данным как C# фильтры на распарсенном Instance.
/// </summary>
public class XPathQueries
{
    /// <summary>
    /// Выполняет запросы как C# фильтры на уже распарсенном Instance.
    /// O(n) по контекстам, 0 аллокаций DOM.
    /// </summary>
    public void ExecuteQueriesOnInstance(Instance instance)
    {
        // Query 1: контексты с instant = "2019-04-30"
        var instantDate = new DateTime(2019, 4, 30);
        var instantResults = instance.Contexts
            .Where(c => c.PeriodInstant == instantDate)
            .Select(c => c.Id)
            .ToList();

        Console.WriteLine("1. Контексты с периодом instant = \"2019-04-30\":");
        foreach (var id in instantResults)
            Console.WriteLine($"  - {id}");
        Console.WriteLine($"Найдено: {instantResults.Count}\n");

        // Query 2: контексты со сценарием dimension="dim-int:ID_sobstv_CZBTaxis"
        const string targetDimension = "dim-int:ID_sobstv_CZBTaxis";
        var dimensionResults = instance.Contexts
            .Where(c => c.Scenarios.Any(s =>
                s.DimensionType == DimensionType.TypedMember &&
                s.DimensionName == targetDimension))
            .Select(c => c.Id)
            .ToList();

        Console.WriteLine("2. Контексты со сценарием dimension=\"dim-int:ID_sobstv_CZBTaxis\":");
        foreach (var id in dimensionResults)
            Console.WriteLine($"  - {id}");
        Console.WriteLine($"Найдено: {dimensionResults.Count}\n");

        // Query 3: контексты без сценария
        var noScenarioResults = instance.Contexts
            .Where(c => c.Scenarios.Count == 0)
            .Select(c => c.Id)
            .ToList();

        Console.WriteLine("3. Контексты без сценария:");
        foreach (var id in noScenarioResults)
            Console.WriteLine($"  - {id}");
        Console.WriteLine($"Найдено: {noScenarioResults.Count}\n");
    }
}
