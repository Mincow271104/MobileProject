namespace be.Models
{
    public class ProductPetType
    {
        public int ProductPetTypeID { get; set; } // Primary key, INTEGER, auto-increment
        public string ProductID { get; set; }
        public string PetType { get; set; }

        // Quan hệ
        public Product Product { get; set; }
    }
}
