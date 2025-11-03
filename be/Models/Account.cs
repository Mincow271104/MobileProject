namespace be.Models
{
    public class Account
    {
        public string AccountID { get; set; } // Primary key, STRING(10)
        public string AccountName { get; set; } // STRING(50), not null
        public string Email { get; set; } // STRING(100), not null
        public string Password { get; set; } // TEXT, not null
        public string UserName { get; set; } // STRING(50), not null
        public string? UserImage { get; set; } // TEXT, nullable
        public string Phone { get; set; } // STRING(11), not null
        public string Address { get; set; } // STRING(100), not null
        public string Gender { get; set; } // STRING(20), not null, liên kết với AllCodes (Type = 'Gender')
        public int? LoginAttempt { get; set; } // INTEGER, nullable
        public DateTime? LockUntil { get; set; } // DATE, nullable
        public DateTime CreatedAt { get; set; } // DATE, not null
        public string AccountStatus { get; set; } // STRING(20), not null, liên kết với AllCodes (Type = 'AccountStatus')
        public string AccountType { get; set; } // STRING(20), not null, liên kết với AllCodes (Type = 'AccountType')

        // Quan hệ
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
