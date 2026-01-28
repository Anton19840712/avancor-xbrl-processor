using XbrlProcessor.Models.Entities;

namespace XbrlProcessor.Services
{
    /// <summary>
    /// Представляет различие между двумя фактами
    /// </summary>
    public class FactDifference
    {
        /// <summary>
        /// Ключ факта (комбинация Id и ContextRef)
        /// </summary>
        public string? FactKey { get; set; }

        /// <summary>
        /// Факт из первого отчета
        /// </summary>
        public Fact? Fact1 { get; set; }

        /// <summary>
        /// Факт из второго отчета
        /// </summary>
        public Fact? Fact2 { get; set; }
    }
}
