using be.Models;
using Microsoft.EntityFrameworkCore;

// Chuyển từ Sequelize sang EF Core DbContext
// Ghi chú: DbContext quản lý các bảng liên quan đến chức năng mua hàng
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // DbSet cho các model giữ lại
    public DbSet<Account> Accounts { get; set; }
    public DbSet<AllCodes> AllCodes { get; set; }
    public DbSet<Banner> Banners { get; set; }
    public DbSet<BlacklistToken> BlacklistTokens { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<Image> Images { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceDetail> InvoiceDetails { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductDetail> ProductDetails { get; set; }
    public DbSet<ProductPetType> ProductPetTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Cấu hình Account
        modelBuilder.Entity<Account>()
            .HasKey(a => a.AccountID);
        modelBuilder.Entity<Account>()
            .HasIndex(a => a.Email)
            .IsUnique()
            .HasDatabaseName("unique_email");
        modelBuilder.Entity<Account>()
            .HasIndex(a => a.Phone)
            .HasDatabaseName("index_phone");
        modelBuilder.Entity<Account>()
            .HasIndex(a => a.UserName)
            .HasDatabaseName("index_username");

        // Cấu hình AllCodes
        modelBuilder.Entity<AllCodes>()
            .HasKey(a => a.CodeID);
        modelBuilder.Entity<AllCodes>()
            .HasIndex(a => new { a.Type, a.Code })
            .IsUnique()
            .HasDatabaseName("unique_type_code");
        modelBuilder.Entity<AllCodes>()
            .HasIndex(a => a.Type)
            .HasDatabaseName("index_type");
        modelBuilder.Entity<AllCodes>()
            .Property(a => a.ExtraValue)
            .HasColumnType("decimal(18,2)");

        // Cấu hình Banner
        modelBuilder.Entity<Banner>()
            .HasKey(b => b.BannerID);
        modelBuilder.Entity<Banner>()
            .HasIndex(b => b.ProductID)
            .HasDatabaseName("index_product_id");

        // Cấu hình BlacklistToken
        modelBuilder.Entity<BlacklistToken>()
            .HasKey(b => b.TokenID);
        modelBuilder.Entity<BlacklistToken>()
            .HasIndex(b => b.Token)
            .HasDatabaseName("index_token");

        // Cấu hình CartItem
        modelBuilder.Entity<CartItem>()
            .HasKey(c => c.CartItemID);
        modelBuilder.Entity<CartItem>()
            .HasIndex(c => c.AccountID)
            .HasDatabaseName("index_account_id");
        modelBuilder.Entity<CartItem>()
            .HasIndex(c => c.ProductID)
            .HasDatabaseName("index_product_id");
        modelBuilder.Entity<CartItem>()
            .HasIndex(c => c.ProductDetailID)
            .HasDatabaseName("index_product_detail_id");
        modelBuilder.Entity<CartItem>()
            .Property(c => c.ItemPrice)
            .HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CartItem>()
            .HasOne(c => c.Product)
            .WithMany()
            .HasForeignKey(c => c.ProductID)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_CartItems_Products_ProductID");
        modelBuilder.Entity<CartItem>()
            .HasOne(c => c.ProductDetail)
            .WithMany()
            .HasForeignKey(c => c.ProductDetailID)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_CartItems_ProductDetails_ProductDetailID");
        modelBuilder.Entity<CartItem>()
            .HasOne(c => c.Account)
            .WithMany()
            .HasForeignKey(c => c.AccountID)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_CartItems_Accounts_AccountID");

        // Cấu hình Coupon
        modelBuilder.Entity<Coupon>()
            .HasKey(c => c.CouponID);
        modelBuilder.Entity<Coupon>()
            .HasIndex(c => c.CouponCode)
            .IsUnique()
            .HasDatabaseName("unique_coupon_code");
        modelBuilder.Entity<Coupon>()
            .HasIndex(c => new { c.StartDate, c.EndDate })
            .HasDatabaseName("index_date_range");
        modelBuilder.Entity<Coupon>()
            .Property(c => c.DiscountValue)
            .HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Coupon>()
            .Property(c => c.MaxDiscount)
            .HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Coupon>()
            .Property(c => c.MinOrderValue)
            .HasColumnType("decimal(18,2)");

        // Cấu hình Image
        modelBuilder.Entity<Image>()
            .HasKey(i => i.ImageID);
        modelBuilder.Entity<Image>()
            .HasIndex(i => new { i.ReferenceType, i.ReferenceID })
            .HasDatabaseName("index_reference");
        modelBuilder.Entity<Image>()
            .HasOne(i => i.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.ReferenceID)
            .HasConstraintName("FK_Image_Product")
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        // Cấu hình Invoice
        modelBuilder.Entity<Invoice>()
            .HasKey(i => i.InvoiceID);
        modelBuilder.Entity<Invoice>()
            .HasIndex(i => i.CouponID)
            .HasDatabaseName("index_coupon_id");
        modelBuilder.Entity<Invoice>()
            .HasIndex(i => i.AccountID)
            .HasDatabaseName("index_account_id");
        modelBuilder.Entity<Invoice>()
            .Property(i => i.DiscountAmount)
            .HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Invoice>()
            .Property(i => i.TotalPayment)
            .HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Invoice>()
            .Property(i => i.TotalPrice)
            .HasColumnType("decimal(18,2)");

        // Cấu hình InvoiceDetail
        modelBuilder.Entity<InvoiceDetail>()
            .HasKey(i => i.InvoiceDetailID);
        modelBuilder.Entity<InvoiceDetail>()
            .HasIndex(i => i.InvoiceID)
            .HasDatabaseName("index_invoice_id");
        modelBuilder.Entity<InvoiceDetail>()
            .HasIndex(i => i.ProductID)
            .HasDatabaseName("index_product_id");
        modelBuilder.Entity<InvoiceDetail>()
            .HasIndex(i => i.ProductDetailID)
            .HasDatabaseName("index_product_detail_id");
        modelBuilder.Entity<InvoiceDetail>()
            .Property(i => i.ItemPrice)
            .HasColumnType("decimal(18,2)");
        modelBuilder.Entity<InvoiceDetail>()
            .HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductID)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_InvoiceDetails_Products_ProductID");
        modelBuilder.Entity<InvoiceDetail>()
            .HasOne(i => i.ProductDetail)
            .WithMany()
            .HasForeignKey(i => i.ProductDetailID)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_InvoiceDetails_ProductDetails_ProductDetailID");
        modelBuilder.Entity<InvoiceDetail>()
            .HasOne(i => i.Invoice)
            .WithMany()
            .HasForeignKey(i => i.InvoiceID)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_InvoiceDetails_Invoices_InvoiceID");

        // Cấu hình Product
        modelBuilder.Entity<Product>()
            .HasKey(p => p.ProductID);
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.ProductName)
            .HasDatabaseName("index_product_name");
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.ProductType)
            .HasDatabaseName("index_product_type");
        modelBuilder.Entity<Product>()
            .Property(p => p.ProductPrice)
            .HasColumnType("decimal(18,2)");

        // Cấu hình ProductDetail
        modelBuilder.Entity<ProductDetail>()
            .HasKey(p => p.ProductDetailID);
        modelBuilder.Entity<ProductDetail>()
            .HasIndex(p => p.ProductID)
            .HasDatabaseName("index_product_id");
        modelBuilder.Entity<ProductDetail>()
            .Property(p => p.ExtraPrice)
            .HasColumnType("decimal(18,2)");
        modelBuilder.Entity<ProductDetail>()
            .Property(p => p.Promotion)
            .HasColumnType("decimal(18,2)");

        // Cấu hình ProductPetType
        modelBuilder.Entity<ProductPetType>()
            .HasKey(p => p.ProductPetTypeID);
        modelBuilder.Entity<ProductPetType>()
            .HasIndex(p => p.ProductID)
            .HasDatabaseName("index_product_id");
    }
}