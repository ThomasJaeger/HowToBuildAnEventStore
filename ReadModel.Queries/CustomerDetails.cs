using System;

namespace ReadModel.Queries
{
    public class CustomerDetails
    {
        public string CustomerId { get; set; }
        public decimal AccountBalance { get; set; }
        public DateTime Created { get; set; }
        public bool Delinquent { get; set; }
        public string Description { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Version { get; set; }
    }
}
