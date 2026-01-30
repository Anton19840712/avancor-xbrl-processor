using System.Globalization;

namespace XbrlProcessor.Models.Entities;

/// <summary>
/// Тип значения XBRL факта
/// </summary>
public enum XbrlValueType
{
    String,
    Numeric,
    Date,
    Boolean
}

/// <summary>
/// Типизированное значение XBRL факта.
/// Хранит исходную строку для сериализации и распарсенное значение для семантического сравнения.
/// </summary>
public sealed class XbrlValue : IEquatable<XbrlValue>
{
    /// <summary>Исходное строковое значение из XML</summary>
    public string RawValue { get; }

    /// <summary>Определённый тип значения</summary>
    public XbrlValueType Type { get; }

    /// <summary>Числовое значение (если тип Numeric)</summary>
    public decimal? NumericValue { get; }

    /// <summary>Значение даты (если тип Date)</summary>
    public DateTime? DateValue { get; }

    /// <summary>Логическое значение (если тип Boolean)</summary>
    public bool? BooleanValue { get; }

    private XbrlValue(string rawValue, XbrlValueType type, decimal? numeric, DateTime? date, bool? boolean)
    {
        RawValue = rawValue;
        Type = type;
        NumericValue = numeric;
        DateValue = date;
        BooleanValue = boolean;
    }

    /// <summary>
    /// Парсит строковое значение из XML в типизированный XbrlValue.
    /// Порядок проверки: boolean, decimal, date, string.
    /// </summary>
    public static XbrlValue Parse(string? rawValue)
    {
        if (string.IsNullOrEmpty(rawValue))
            return new XbrlValue(rawValue ?? "", XbrlValueType.String, null, null, null);

        // Boolean — проверяем первым, т.к. "true"/"false" не должны попасть в другие типы
        if (bool.TryParse(rawValue, out var boolResult))
            return new XbrlValue(rawValue, XbrlValueType.Boolean, null, null, boolResult);

        // Numeric — decimal с InvariantCulture
        if (decimal.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var numericResult))
            return new XbrlValue(rawValue, XbrlValueType.Numeric, numericResult, null, null);

        // Date — ISO 8601 форматы
        if (DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateResult))
            return new XbrlValue(rawValue, XbrlValueType.Date, null, dateResult, null);

        return new XbrlValue(rawValue, XbrlValueType.String, null, null, null);
    }

    /// <summary>
    /// Семантическое сравнение: "1000.00" == "1000" для числовых значений
    /// </summary>
    public bool SemanticallyEquals(XbrlValue? other)
    {
        if (other is null) return false;
        if (Type != other.Type) return false;

        return Type switch
        {
            XbrlValueType.Numeric => NumericValue == other.NumericValue,
            XbrlValueType.Date => DateValue == other.DateValue,
            XbrlValueType.Boolean => BooleanValue == other.BooleanValue,
            _ => string.Equals(RawValue, other.RawValue, StringComparison.Ordinal)
        };
    }

    public bool Equals(XbrlValue? other) => SemanticallyEquals(other);

    public override bool Equals(object? obj) => obj is XbrlValue other && SemanticallyEquals(other);

    public override int GetHashCode()
    {
        return Type switch
        {
            XbrlValueType.Numeric => HashCode.Combine(Type, NumericValue),
            XbrlValueType.Date => HashCode.Combine(Type, DateValue),
            XbrlValueType.Boolean => HashCode.Combine(Type, BooleanValue),
            _ => HashCode.Combine(Type, RawValue)
        };
    }

    public static bool operator ==(XbrlValue? left, XbrlValue? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(XbrlValue? left, XbrlValue? right) => !(left == right);

    /// <summary>Возвращает исходную строку для сериализации в XML</summary>
    public override string ToString() => RawValue;
}
