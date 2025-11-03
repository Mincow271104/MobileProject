namespace be.Models.DTOs
{
    public class CouponRequest
    {
        public string CouponID { get; set; }
        public string CouponCode { get; set; }
        public string CouponDescription { get; set; }
        public decimal? MinOrderValue { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscount { get; set; }
        public string DiscountType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string CouponStatus { get; set; }
    }
}