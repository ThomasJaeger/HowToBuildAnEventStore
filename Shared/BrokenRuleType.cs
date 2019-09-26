namespace Shared
{
    public class BrokenRuleType : Enumeration
    {
        public static readonly BrokenRuleType None = new BrokenRuleType(0, "None", "All rules passed.");
        public static readonly BrokenRuleType NoSubscriptionAssignedToAccount = new BrokenRuleType(1000, "No Subscription Assigned To Account", "Pausible cause could be that no subscription was assigned to an account while creating the account with an initial subscription.");

        private BrokenRuleType(int value, string displayName, string description) : base(value, displayName, description)
        {
        }

        public BrokenRuleType()
        {
        }
    }
}
