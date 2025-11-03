namespace be.Models.DTOs
{
    public class AddToCartRequest
    {
        public string AccountID { get; set; }
        public CartInfoRequest[] CartInfo { get; set; }
    }
}