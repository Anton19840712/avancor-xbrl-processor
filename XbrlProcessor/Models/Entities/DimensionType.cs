namespace XbrlProcessor.Models.Entities;

/// <summary>
/// Тип измерения XBRL сценария
/// </summary>
public enum DimensionType
{
    /// <summary>Явный член измерения</summary>
    ExplicitMember,

    /// <summary>Типизированный член измерения</summary>
    TypedMember,

    /// <summary>Неизвестный тип</summary>
    Unknown
}

/// <summary>
/// Расширения для конвертации DimensionType в/из XML-имени
/// </summary>
public static class DimensionTypeExtensions
{
    /// <summary>
    /// Конвертирует enum в XML-имя элемента
    /// </summary>
    public static string ToXmlName(this DimensionType type) => type switch
    {
        DimensionType.ExplicitMember => "explicitMember",
        DimensionType.TypedMember => "typedMember",
        _ => ""
    };

    /// <summary>
    /// Конвертирует XML-имя элемента в enum
    /// </summary>
    public static DimensionType FromXmlName(string? name) => name switch
    {
        "explicitMember" => DimensionType.ExplicitMember,
        "typedMember" => DimensionType.TypedMember,
        _ => DimensionType.Unknown
    };
}
