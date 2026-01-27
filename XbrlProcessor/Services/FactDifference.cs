using XbrlProcessor.Models.Entities;

namespace XbrlProcessor.Services;

/// <summary>
/// Представляет различие между двумя фактами
/// </summary>
/// <param name="FactKey">Ключ факта (комбинация Id и ContextRef)</param>
/// <param name="Fact1">Факт из первого отчета</param>
/// <param name="Fact2">Факт из второго отчета</param>
public record FactDifference
    {
        /// <summary>
        /// Ключ факта (комбинация Id и ContextRef)
        /// </summary>
        public string? FactKey { get; init; }

        /// <summary>
        /// Факт из первого отчета
        /// </summary>
        public Fact? Fact1 { get; init; }

        /// <summary>
        /// Факт из второго отчета
        /// </summary>
        public Fact? Fact2 { get; init; }
    }
