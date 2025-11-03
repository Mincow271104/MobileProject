namespace be.Models.DTOs
{
    public class ChangeAccountStatusRequest
    {
        public string AccountId { get; set; }
        public string AccountStatus { get; set; }
    }
}