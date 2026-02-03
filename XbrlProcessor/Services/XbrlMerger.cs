using XbrlProcessor.Models.Entities;
using XbrlProcessor.Configuration;

namespace XbrlProcessor.Services;

/// <summary>
/// Сервис для объединения XBRL отчетов
/// </summary>
/// <param name="settings">Настройки приложения</param>
public class XbrlMerger(XbrlSettings settings)
{
    /// <summary>
    /// Объединяет два отчета в один, удаляя дубликаты контекстов, единиц и фактов
    /// </summary>
    public Instance MergeInstances(Instance instance1, Instance instance2)
        => MergeInstances([instance1, instance2]);

    /// <summary>
    /// Объединяет произвольное количество отчетов в один, удаляя дубликаты
    /// </summary>
    public Instance MergeInstances(IReadOnlyList<Instance> instances)
    {
        var result = new Instance();

        var totalContexts = instances.Sum(i => i.Contexts.Count);
        var totalUnits = instances.Sum(i => i.Units.Count);
        var totalFacts = instances.Sum(i => i.Facts.Count);

        var contextMap = new Dictionary<string, Context>(totalContexts);
        foreach (var instance in instances)
        {
            foreach (var context in instance.Contexts)
            {
                var signature = ContextSignatureHelper.GetSignature(context, settings);
                if (contextMap.TryAdd(signature, context))
                    result.Contexts.Add(context);
            }
        }

        var unitSet = new HashSet<(string?, string?, string?)>(totalUnits);
        foreach (var instance in instances)
        {
            foreach (var unit in instance.Units)
            {
                if (unitSet.Add((unit.Measure, unit.Numerator, unit.Denominator)))
                    result.Units.Add(unit);
            }
        }

        var factSet = new HashSet<(string?, string?, string?)>(totalFacts);
        foreach (var instance in instances)
        {
            foreach (var fact in instance.Facts)
            {
                if (factSet.Add((fact.Id, fact.ContextRef, fact.UnitRef)))
                    result.Facts.Add(fact);
            }
        }

        return result;
    }
}
