namespace XbrlProcessor.Services;

/// <summary>
/// Struct-ключ для кросс-файлового сравнения фактов.
/// Заменяет строковую конкатенацию $"{conceptName}|{contextSig}" —
/// избегает аллокации строки на каждый факт, использует HashCode.Combine.
/// </summary>
public readonly struct CrossFileFactKey : IEquatable<CrossFileFactKey>
{
    public readonly string ConceptName;
    public readonly string ContextSignature;

    public CrossFileFactKey(string conceptName, string contextSignature)
    {
        ConceptName = conceptName;
        ContextSignature = contextSignature;
    }

    public bool Equals(CrossFileFactKey other)
        => string.Equals(ConceptName, other.ConceptName, StringComparison.Ordinal)
        && string.Equals(ContextSignature, other.ContextSignature, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is CrossFileFactKey other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(ConceptName, ContextSignature);

    public override string ToString() => $"{ConceptName}|{ContextSignature}";
}

/// <summary>
/// IEqualityComparer для CrossFileFactKey — для использования в Dictionary.
/// </summary>
public sealed class CrossFileFactKeyComparer : IEqualityComparer<CrossFileFactKey>
{
    public static readonly CrossFileFactKeyComparer Instance = new();

    public bool Equals(CrossFileFactKey x, CrossFileFactKey y) => x.Equals(y);

    public int GetHashCode(CrossFileFactKey obj) => obj.GetHashCode();
}
