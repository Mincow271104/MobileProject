namespace be.Models.DTOs
{
    public class UpdateQuantityRequest
    {
        public string AccountID { get; set; }
        public string ProductID { get; set; }
        public string ProductDetailID { get; set; }
        public int Quantity { get; set; }
    }
}