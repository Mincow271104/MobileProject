namespace be.Models
{
    public class Product
    {
        public string ProductID { get; set; }
        public string ProductName { get; set; }
        public string ProductType { get; set; }
        public decimal ProductPrice { get; set; }
        public string ProductDescription { get; set; }
        public string ProductImage { get; set; }

        // Quan hệ
        public ICollection<ProductDetail> ProductDetails { get; set; } = new List<ProductDetail>();
        public ICollection<ProductPetType> ProductPetTypes { get; set; } = new List<ProductPetType>();
        public ICollection<Banner> Banners { get; set; } = new List<Banner>();
        public ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<Image> Images { get; set; } = new List<Image>();
    }
}
