namespace be.Models
{
    public class InvoiceDetail
    {
        public string InvoiceDetailID { get; set; }
            public string InvoiceID { get; set; }
            public string ProductID { get; set; }
            public string ProductDetailID { get; set; }
            public int ItemQuantity { get; set; }
            public decimal ItemPrice { get; set; }
        // Quan hệ
        public Invoice Invoice { get; set; }
        public Product Product { get; set; }
        public ProductDetail ProductDetail { get; set; }
    }
}
