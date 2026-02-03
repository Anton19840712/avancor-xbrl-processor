using System.Runtime.CompilerServices;
using XbrlProcessor.Models.Entities;
using XbrlProcessor.Configuration;

namespace XbrlProcessor.Services;

/// <summary>
/// Утилита для создания строковых подписей контекстов.
/// Сигнатуры кешируются по Context.Id для избежания повторных вычислений.
/// </summary>
public static class ContextSignatureHelper
{
    private static readonly ConditionalWeakTable<Context, string> Cache = new();

    /// <summary>
    /// Создает строковую подпись контекста для сравнения на дублирование.
    /// Результат кешируется по экземпляру Context (reference equality).
    /// </summary>
    public static string GetSignature(Context context, XbrlSettings settings)
    {
        return Cache.GetValue(context, c => ComputeSignature(c, settings));
    }

    private static string ComputeSignature(Context context, XbrlSettings settings)
    {
        var sep = settings.ContextSignatureSeparator;
        var sb = new System.Text.StringBuilder(256);

        sb.Append(context.EntityValue ?? "");
        sb.Append(sep);
        sb.Append(context.EntityScheme ?? "");
        sb.Append(sep);
        sb.Append(context.EntitySegment ?? "");
        sb.Append(sep);
        sb.Append(context.PeriodInstant?.ToString(settings.DateFormat) ?? "");
        sb.Append(sep);
        sb.Append(context.PeriodStartDate?.ToString(settings.DateFormat) ?? "");
        sb.Append(sep);
        sb.Append(context.PeriodEndDate?.ToString(settings.DateFormat) ?? "");
        sb.Append(sep);
        sb.Append(context.PeriodForever);

        foreach (var scenario in context.Scenarios.OrderBy(s => s.DimensionName))
        {
            sb.Append(sep);
            sb.Append(scenario.DimensionType.ToXmlName());
            sb.Append('|');
            sb.Append(scenario.DimensionName);
            sb.Append('|');
            sb.Append(scenario.DimensionCode);
            sb.Append('|');
            sb.Append(scenario.DimensionValue);
        }

        return sb.ToString();
    }
}
