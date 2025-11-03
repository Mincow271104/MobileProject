namespace be.Models.DTOs
{
    public class ChangePasswordRequest
    {
        public string AccountId { get; set; }
        public string Password { get; set; }
        public string NewPassword { get; set; }
    }
}