using XbrlProcessor.Models.Entities;

namespace XbrlProcessor.Services
{
    /// <summary>
    /// Результат сравнения двух XBRL отчетов
    /// </summary>
    public class ComparisonResult
    {
        /// <summary>
        /// Факты, отсутствующие во втором отчете
        /// </summary>
        public List<Fact> MissingFacts { get; set; } = new();

        /// <summary>
        /// Новые факты, присутствующие только во втором отчете
        /// </summary>
        public List<Fact> NewFacts { get; set; } = new();

        /// <summary>
        /// Факты с различающимися значениями
        /// </summary>
        public List<FactDifference> ModifiedFacts { get; set; } = new();
    }
}
