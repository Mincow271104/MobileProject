namespace be.Models
{
    public class CartItem
    {
        public string CartItemID { get; set; }
        public string AccountID { get; set; }
        public string ProductID { get; set; }
        public string ProductDetailID { get; set; }
        public decimal ItemPrice { get; set; }
        public int ItemQuantity { get; set; }

        // Quan hệ
        public Account Account { get; set; }
        public Product Product { get; set; }
        public ProductDetail ProductDetail { get; set; }
    }
}
