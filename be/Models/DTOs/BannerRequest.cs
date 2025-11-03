namespace be.Models.DTOs
{
    public class BannerRequest
    {
        public string BannerID { get; set; }
        public string BannerImage { get; set; }
        public DateTime? HiddenAt { get; set; }
        public string BannerStatus { get; set; }
        public string ProductID { get; set; }
    }
}
