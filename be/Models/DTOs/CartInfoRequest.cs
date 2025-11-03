namespace be.Models.DTOs
{
    public class CartInfoRequest
    {
        public string CartItemID { get; set; }
        public string ProductID { get; set; }
        public string ProductDetailID { get; set; }
        public decimal ItemPrice { get; set; }
        public int ItemQuantity { get; set; }
    }
}