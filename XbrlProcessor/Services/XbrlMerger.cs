using XbrlProcessor.Models.Entities;
using XbrlProcessor.Configuration;
using XbrlProcessor.Builders;

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
    {
        var builder = new InstanceBuilder();

        // Объединяем контексты (удаляем дубликаты)
        var contextMap = new Dictionary<string, Context>();
        foreach (var context in instance1.Contexts.Concat(instance2.Contexts))
        {
            var signature = ContextSignatureHelper.GetSignature(context, settings);
            if (!contextMap.ContainsKey(signature))
            {
                contextMap[signature] = context;
                builder.AddContext(context);
            }
        }

        // Объединяем единицы измерения (удаляем дубликаты)
        var unitMap = new Dictionary<string, Unit>();
        foreach (var unit in instance1.Units.Concat(instance2.Units))
        {
            var signature = GetUnitSignature(unit);
            if (!unitMap.ContainsKey(signature))
            {
                unitMap[signature] = unit;
                builder.AddUnit(unit);
            }
        }

        // Объединяем факты
        var factMap = new Dictionary<string, Fact>();
        foreach (var fact in instance1.Facts.Concat(instance2.Facts))
        {
            var key = $"{fact.Id}|{fact.ContextRef}|{fact.UnitRef}";
            if (!factMap.ContainsKey(key))
            {
                factMap[key] = fact;
                builder.AddFact(fact);
            }
        }

        return builder.Build();
    }

    private static string GetUnitSignature(Unit unit)
    {
        return $"{unit.Measure}|{unit.Numerator}|{unit.Denominator}";
    }
}
