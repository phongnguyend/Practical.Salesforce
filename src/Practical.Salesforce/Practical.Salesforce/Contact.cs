namespace Practical.Salesforce
{
    internal class Contact
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public User Owner { get; set; }

        public string OwnerId { get; set; }
    }
}
