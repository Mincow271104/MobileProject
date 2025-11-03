namespace be.Models.DTOs
{
    public class ChangeInvoiceStatusRequest
    {
        public string InvoiceID { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string CancelReason { get; set; }
    }
}