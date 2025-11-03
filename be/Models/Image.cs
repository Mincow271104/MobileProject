namespace be.Models
{
    public class Image
    {
        public int ImageID { get; set; } // Primary key, INTEGER, auto-increment
        public string ImageUrl { get; set; } // TEXT, not null
        public string ReferenceType { get; set; } // STRING(20), not null
        public string ReferenceID { get; set; } // STRING(10), not null

        // Quan hệ
        public Product? Product { get; set; }
    }
}
