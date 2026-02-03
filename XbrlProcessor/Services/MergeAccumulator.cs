using XbrlProcessor.Models.Entities;
using XbrlProcessor.Configuration;

namespace XbrlProcessor.Services;

/// <summary>
/// Потоковый аккумулятор для объединения XBRL-отчетов.
/// Принимает Instance по одному, сохраняя только dedup-состояние.
/// После Add Instance может быть освобождён GC.
/// </summary>
public class MergeAccumulator : IMergeAccumulator
{
    private readonly XbrlSettings _settings;
    private readonly Instance _result = new();
    private readonly Dictionary<string, Context> _contextMap = new();
    private readonly HashSet<(string?, string?, string?)> _unitSet = new();
    private readonly HashSet<(string?, string?, string?)> _factSet = new();

    public MergeAccumulator(XbrlSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Добавляет один Instance в аккумулятор, дедуплицируя контексты/юниты/факты.
    /// После вызова Instance может быть освобождён.
    /// </summary>
    public void Add(Instance instance)
    {
        foreach (var context in instance.Contexts)
        {
            var signature = ContextSignatureHelper.GetSignature(context, _settings);
            if (_contextMap.TryAdd(signature, context))
                _result.Contexts.Add(context);
        }

        foreach (var unit in instance.Units)
        {
            if (_unitSet.Add((unit.Measure, unit.Numerator, unit.Denominator)))
                _result.Units.Add(unit);
        }

        foreach (var fact in instance.Facts)
        {
            if (_factSet.Add((fact.Id, fact.ContextRef, fact.UnitRef)))
                _result.Facts.Add(fact);
        }
    }

    /// <summary>
    /// Возвращает объединённый Instance со всеми уникальными элементами.
    /// </summary>
    public Instance Build() => _result;
}
