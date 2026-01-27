using XbrlProcessor.Models.Entities;

namespace XbrlProcessor.Services
{
    public class ComparisonResult
    {
        public List<Fact> MissingFacts { get; set; } = new();
        public List<Fact> NewFacts { get; set; } = new();
        public List<FactDifference> ModifiedFacts { get; set; } = new();
    }
}
