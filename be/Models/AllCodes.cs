namespace be.Models
{
    public class AllCodes
    {
        public int CodeID { get; set; } // Primary key, INTEGER, auto-increment
        public string Type { get; set; } // STRING(30), not null
        public string Code { get; set; } // STRING(20), not null
        public string CodeValueVI { get; set; } // STRING(50), not null
        public decimal? ExtraValue { get; set; } // DECIMAL(10,2), nullable
    }
}
