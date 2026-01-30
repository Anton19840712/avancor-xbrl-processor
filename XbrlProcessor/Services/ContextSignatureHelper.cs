using XbrlProcessor.Models.Entities;
using XbrlProcessor.Configuration;

namespace XbrlProcessor.Services;

/// <summary>
/// Утилита для создания строковых подписей контекстов
/// </summary>
public static class ContextSignatureHelper
{
    /// <summary>
    /// Создает строковую подпись контекста для сравнения на дублирование
    /// </summary>
    /// <param name="context">Контекст для создания подписи</param>
    /// <param name="settings">Настройки приложения</param>
    /// <returns>Строковая подпись контекста</returns>
    public static string GetSignature(Context context, XbrlSettings settings)
    {
        var parts = new List<string>
        {
            context.EntityValue ?? "",
            context.EntityScheme ?? "",
            context.EntitySegment ?? "",
            context.PeriodInstant?.ToString(settings.DateFormat) ?? "",
            context.PeriodStartDate?.ToString(settings.DateFormat) ?? "",
            context.PeriodEndDate?.ToString(settings.DateFormat) ?? "",
            context.PeriodForever.ToString()
        };

        foreach (var scenario in context.Scenarios.OrderBy(s => s.DimensionName))
        {
            parts.Add($"{scenario.DimensionType.ToXmlName()}|{scenario.DimensionName}|{scenario.DimensionCode}|{scenario.DimensionValue}");
        }

        return string.Join(settings.ContextSignatureSeparator, parts);
    }
}
