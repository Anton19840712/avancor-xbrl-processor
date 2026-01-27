using XbrlProcessor.Models.Entities;

namespace XbrlProcessor.Services
{
    public class FactDifference
    {
        public string FactKey { get; set; }
        public Fact Fact1 { get; set; }
        public Fact Fact2 { get; set; }
    }
}
