using System.Collections.Concurrent;
using XbrlProcessor.Models.Entities;
using XbrlProcessor.Configuration;

namespace XbrlProcessor.Services;

/// <summary>
/// Потокобезопасный аккумулятор для объединения XBRL-отчетов.
/// Использует ConcurrentDictionary для dedup-состояния и lock для Instance (не thread-safe).
/// </summary>
public class ConcurrentMergeAccumulator : IMergeAccumulator
{
    private readonly XbrlSettings _settings;
    private readonly Instance _result = new();
    private readonly object _resultLock = new();
    private readonly ConcurrentDictionary<string, Context> _contextMap = new();
    private readonly ConcurrentDictionary<(string?, string?, string?), byte> _unitSet = new();
    private readonly ConcurrentDictionary<(string?, string?, string?), byte> _factSet = new();

    public ConcurrentMergeAccumulator(XbrlSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Добавляет один Instance в аккумулятор. Потокобезопасно.
    /// </summary>
    public void Add(Instance instance)
    {
        foreach (var context in instance.Contexts)
        {
            var signature = ContextSignatureHelper.GetSignature(context, _settings);
            if (_contextMap.TryAdd(signature, context))
            {
                lock (_resultLock)
                {
                    _result.Contexts.Add(context);
                }
            }
        }

        foreach (var unit in instance.Units)
        {
            if (_unitSet.TryAdd((unit.Measure, unit.Numerator, unit.Denominator), 0))
            {
                lock (_resultLock)
                {
                    _result.Units.Add(unit);
                }
            }
        }

        foreach (var fact in instance.Facts)
        {
            if (_factSet.TryAdd((fact.Id, fact.ContextRef, fact.UnitRef), 0))
            {
                lock (_resultLock)
                {
                    _result.Facts.Add(fact);
                }
            }
        }
    }

    /// <summary>
    /// Возвращает объединённый Instance со всеми уникальными элементами.
    /// </summary>
    public Instance Build() => _result;
}
