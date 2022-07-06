namespace Practical.Salesforce
{
    internal class Lead
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Company { get; set; }

        public string LeadSource { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string Description { get; set; }

        public Address Address { get; set; }

        public string Country { get; set; }

        public User Owner { get; set; }

        public string OwnerId { get; set; }

        public string IsConverted { get; set; }

        public string ConvertedDate { get; set; }

        public string ConvertedOpportunityId { get; set; }

        public Opportunity ConvertedOpportunity { get; set; }

        public string ConvertedContactId { get; set; }

        public Contact ConvertedContact { get; set; }

        public string ConvertedAccountId { get; set; }

        public Account ConvertedAccount { get; set; }
    }
}
