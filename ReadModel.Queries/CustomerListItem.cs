namespace ReadModel.Queries
{
    public class CustomerListItem
    {
        public string CustomerId { get; set; }
        public decimal AccountBalance { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Version { get; set; }
    }
}
