namespace be.Models.DTOs
{
    public class RemoveFromCartRequest
    {
        public string AccountID { get; set; }
        public string ProductID { get; set; }
        public string ProductDetailID { get; set; }
    }
}