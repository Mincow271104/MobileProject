namespace be.Models
{
    public class ProductDetail
    {
        public string ProductDetailID { get; set; }
        public string ProductID { get; set; }
        public string DetailName { get; set; }
        public int Stock { get; set; }
        public int SoldCount { get; set; }
        public decimal ExtraPrice { get; set; }
        public decimal Promotion { get; set; }
        public DateTime CreatedAt { get; set; }
        public string DetailStatus { get; set; }

        // Quan hệ
        public Product Product { get; set; }
        public ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
