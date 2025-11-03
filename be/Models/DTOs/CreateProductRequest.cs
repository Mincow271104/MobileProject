namespace be.Models.DTOs
{
    public class CreateProductRequest
    {
        public string ProductName { get; set; }
        public string ProductType { get; set; }
        public decimal ProductPrice { get; set; }
        public string ProductDescription { get; set; }
        public string[] PetType { get; set; }
        public ProductDetailRequest[] ProductDetail { get; set; }
        public ImageRequest[] Image { get; set; }
        public string ProductImage { get; set; }
    }

    public class ProductDetailRequest
    {
        public string DetailName { get; set; }
        public int Stock { get; set; }
        public int SoldCount { get; set; }
        public decimal ExtraPrice { get; set; }
        public decimal Promotion { get; set; }
        public string DetailStatus { get; set; }
        public string ProductDetailID { get; set; }
    }

    public class ImageRequest
    {
        public string Image { get; set; }
    }
}