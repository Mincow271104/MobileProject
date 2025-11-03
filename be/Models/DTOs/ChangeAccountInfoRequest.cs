namespace be.Models.DTOs
{
    public class ChangeAccountInfoRequest
    {
        public string AccountId { get; set; }
        public string AccountName { get; set; }
        public string UserName { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Gender { get; set; }
        public string UserImage { get; set; }
        public string AccountType { get; set; }
    }
}