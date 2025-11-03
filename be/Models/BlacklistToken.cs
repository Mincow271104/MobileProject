namespace be.Models
{
    public class BlacklistToken
    {
        public int TokenID { get; set; } // Primary key, INTEGER, auto-increment
        public string Token { get; set; } // TEXT, not null
        public string? ExtraValue { get; set; } // STRING(10), nullable
        public DateTime? CreatedAt { get; set; } // DATE, nullable
        public DateTime? ExpiredAt { get; set; } // DATE, nullable
    }
}
