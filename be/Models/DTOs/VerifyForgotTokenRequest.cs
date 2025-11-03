namespace be.Models.DTOs
{
    public class VerifyForgotTokenRequest
    {
        public string AccountId { get; set; }
        public string Token { get; set; }
    }
}