namespace be.Models
{
    public class Invoice
    {
        public string InvoiceID { get; set; }
        public string AccountID { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverPhone { get; set; }
        public string ReceiverAddress { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPayment { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CanceledAt { get; set; }
        public string CancelReason { get; set; }
        public string PaymentStatus { get; set; }
        public string ShippingStatus { get; set; }
        public string PaymentType { get; set; }
        public string ShippingMethod { get; set; }
        public string CouponID { get; set; }

        // Quan hệ
        public Coupon? Coupon { get; set; }
        public Account? Account { get; set; }
        public ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();
    }
}
