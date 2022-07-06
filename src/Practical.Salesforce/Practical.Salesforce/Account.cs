namespace Practical.Salesforce
{
    internal class Account
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public User Owner { get; set; }

        public string OwnerId { get; set; }
    }
}
