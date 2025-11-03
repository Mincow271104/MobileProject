namespace be.Models.DTOs
{
    public class InvoiceRequest
    {
        public string AccountID { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverPhone { get; set; }
        public string ReceiverAddress { get; set; }
        public CartItemRequest[] CartItems { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPayment { get; set; }
        public string PaymentStatus { get; set; }
        public string ShippingStatus { get; set; }
        public string PaymentType { get; set; }
        public string ShippingMethod { get; set; }
        public string CouponID { get; set; }
        public string Email { get; set; }
        public bool IsBuyNow { get; set; }
    }

    public class CartItemRequest
    {
        public string ProductID { get; set; }
        public string ProductDetailID { get; set; }
        public int ItemQuantity { get; set; }
        public decimal ItemPrice { get; set; }
    }
}
