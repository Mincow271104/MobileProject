namespace be.Models.DTOs
{
    public class LoginRequest
    {
        public string AccountName { get; set; }
        public string Password { get; set; }
        public bool RememberLogin { get; set; }
    }
}