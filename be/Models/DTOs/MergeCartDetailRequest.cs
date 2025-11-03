namespace be.Models.DTOs
{
    public class MergeCartDetailRequest
    {
        public string AccountID { get; set; }
        public string ProductID { get; set; }
        public string ProductDetailID1 { get; set; }
        public string ProductDetailID2 { get; set; }
        public int Quantity { get; set; }
    }
}