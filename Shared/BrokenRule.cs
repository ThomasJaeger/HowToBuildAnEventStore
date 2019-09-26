namespace Shared
{
    public class BrokenRule
    {
        public BrokenRuleType Type { get; set; }
        string CorrelationId { get; set; }
        string CausationId { get; set; }

        public BrokenRule(BrokenRuleType type, string correlationId, string causationId)
        {
            Type = type;
            CorrelationId = correlationId;
            CausationId = causationId;
        }
    }
}
