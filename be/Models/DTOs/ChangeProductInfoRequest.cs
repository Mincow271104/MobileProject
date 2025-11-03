namespace be.Models.DTOs
{
    public class ChangeProductInfoRequest
    {
        public string ProductID { get; set; }
        public string ProductName { get; set; }
        public string ProductType { get; set; }
        public decimal? ProductPrice { get; set; }
        public string ProductDescription { get; set; }
        public string[] PetType { get; set; }
        public ProductDetailRequest[] ProductDetail { get; set; }
        public ImageRequest[] Image { get; set; }
        public string ProductImage { get; set; }
    }
}