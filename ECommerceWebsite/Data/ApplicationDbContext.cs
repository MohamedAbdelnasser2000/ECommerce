using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ECommerceWebsite.Models;

namespace ECommerceWebsite.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Setting> Settings { get; set; }
    public DbSet<WishlistItem> WishlistItems { get; set; }
    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<CouponUsage> CouponUsages { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NewsletterSubscription> NewsletterSubscriptions { get; set; }
    public DbSet<EmailTemplate> EmailTemplates { get; set; }
    public DbSet<EmailLog> EmailLogs { get; set; }
    public DbSet<ContactMessage> ContactMessages { get; set; }
    public DbSet<ShippingMethod> ShippingMethods { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("DefaultConnection");
        }

        // Suppress pending model changes warning for seed data
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Configure decimal precision
        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Product>()
            .Property(p => p.DiscountPrice)
            .HasPrecision(18, 2);
        // Configure decimal precision for orders
        modelBuilder.Entity<Order>()
            .Property(o => o.Subtotal)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Order>()
            .Property(o => o.ShippingCost)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Order>()
            .Property(o => o.TaxAmount)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Order>()
            .Property(o => o.DiscountAmount)
            .HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>()
            .Property(oi => oi.UnitPrice)
            .HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>()
            .Property(oi => oi.TotalPrice)
            .HasPrecision(18, 2);
        // Configure relationships
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProductImage>()
            .HasOne(pi => pi.Product)
            .WithMany(p => p.ProductImages)
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Review>()
            .HasOne(r => r.Product)
            .WithMany(p => p.Reviews)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Review>()
            .HasOne(r => r.User)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CartItem>()
            .HasOne(ci => ci.Product)
            .WithMany(p => p.CartItems)
            .HasForeignKey(ci => ci.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CartItem>()
            .HasOne(ci => ci.User)
            .WithMany(u => u.CartItems)
            .HasForeignKey(ci => ci.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<WishlistItem>()
            .HasOne(wi => wi.Product)
            .WithMany(p => p.WishlistItems)
            .HasForeignKey(wi => wi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<WishlistItem>()
            .HasOne(wi => wi.User)
            .WithMany(u => u.WishlistItems)
            .HasForeignKey(wi => wi.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        // Configure unique constraints
        modelBuilder.Entity<CartItem>()
            .HasIndex(ci => new { ci.UserId, ci.ProductId })
            .IsUnique();
        modelBuilder.Entity<WishlistItem>()
            .HasIndex(wi => new { wi.UserId, wi.ProductId })
            .IsUnique();
        // Configure Review rating constraint
        modelBuilder.Entity<Review>()
            .ToTable(t => t.HasCheckConstraint("CK_Review_Rating", "Rating >= 1 AND Rating <= 5"));
        // Configure Coupon relationships
        modelBuilder.Entity<Coupon>()
            .HasOne(c => c.Category)
            .WithMany()
            .HasForeignKey(c => c.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Coupon>()
            .HasOne(c => c.Product)
            .WithMany()
            .HasForeignKey(c => c.ProductId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<CouponUsage>()
            .HasOne(cu => cu.Coupon)
            .WithMany(c => c.CouponUsages)
            .HasForeignKey(cu => cu.CouponId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CouponUsage>()
            .HasOne(cu => cu.User)
            .WithMany(u => u.CouponUsages)
            .HasForeignKey(cu => cu.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CouponUsage>()
            .HasOne(cu => cu.Order)
            .WithMany(o => o.CouponUsages)
            .HasForeignKey(cu => cu.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        // Configure Order relationships
        modelBuilder.Entity<Order>()
            .HasOne(o => o.SelectedShippingMethod)
            .WithMany()
            .HasForeignKey(o => o.ShippingMethodId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Order>()
            .HasOne(o => o.AppliedTaxSetting)
            .WithMany()
            .HasForeignKey(o => o.TaxSettingId)
            .OnDelete(DeleteBehavior.SetNull);
        // Configure decimal precision for coupons
        modelBuilder.Entity<Coupon>()
            .Property(c => c.Value)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Coupon>()
            .Property(c => c.MinimumOrderAmount)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Coupon>()
            .Property(c => c.MaximumDiscountAmount)
            .HasPrecision(18, 2);
        modelBuilder.Entity<CouponUsage>()
            .Property(cu => cu.DiscountAmount)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Order>()
            .Property(o => o.DiscountAmount)
            .HasPrecision(18, 2);
        // Configure unique constraint for coupon codes
        modelBuilder.Entity<Coupon>()
            .HasIndex(c => c.Code)
            .IsUnique();
        // Configure Notification relationships
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        // Configure EmailLog relationships
        modelBuilder.Entity<EmailLog>()
            .HasOne(el => el.Template)
            .WithMany()
            .HasForeignKey(el => el.TemplateId)
            .OnDelete(DeleteBehavior.SetNull);
        // Configure unique constraint for newsletter emails
        modelBuilder.Entity<NewsletterSubscription>()
            .HasIndex(ns => ns.Email)
            .IsUnique();
        // Configure decimal precision for notifications
        modelBuilder.Entity<EmailTemplate>()
            .Property(et => et.HtmlContent)
            .HasColumnType("NVARCHAR(MAX)");
        modelBuilder.Entity<EmailTemplate>()
            .Property(et => et.TextContent)
            .HasColumnType("NVARCHAR(MAX)");
        modelBuilder.Entity<EmailLog>()
            .Property(el => el.Content)
            .HasColumnType("NVARCHAR(MAX)");
        // Configure ShippingMethod decimal precision
        modelBuilder.Entity<ShippingMethod>()
            .Property(sm => sm.Cost)
            .HasPrecision(18, 2);
        // Configure TaxSetting decimal precision
        modelBuilder.Entity<TaxSetting>()
            .Property(ts => ts.TaxRate)
            .HasPrecision(5, 2);
        // Seed data
        SeedData(modelBuilder);
    }
    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Categories
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Electronics", Description = "Electronic devices and gadgets", NameAr = "الإلكترونيات", DescriptionAr = "أجهزة وأدوات إلكترونية", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = 2, Name = "Clothing", Description = "Fashion and apparel", NameAr = "الملابس", DescriptionAr = "أزياء وملابس", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = 3, Name = "Books", Description = "Books and literature", NameAr = "الكتب", DescriptionAr = "كتب وأدب", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = 4, Name = "Home & Garden", Description = "Home improvement and gardening", NameAr = "المنزل والحديقة", DescriptionAr = "تحسين المنزل والبستنة", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = 5, Name = "Sports", Description = "Sports and fitness equipment", NameAr = "الرياضة", DescriptionAr = "معدات الرياضة واللياقة", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
        // Seed Products
        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                Name = "iPhone 15 Pro",
                Description = "Latest iPhone with advanced features",
                NameAr = "آيفون 15 برو",
                DescriptionAr = "أحدث آيفون بميزات متقدمة",
                Price = 999.99m,
                StockQuantity = 50,
                CategoryId = 1,
                IsActive = true,
                IsFeatured = true,
                ImageUrl = "/images/products/iphone15pro.jpg",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Product
            {
                Id = 2,
                Name = "Samsung Galaxy S24",
                Description = "Premium Android smartphone",
                NameAr = "سامسونج جالاكسي S24",
                DescriptionAr = "هاتف أندرويد فاخر",
                Price = 899.99m,
                DiscountPrice = 799.99m,
                StockQuantity = 30,
                CategoryId = 1,
                IsActive = true,
                IsFeatured = true,
                ImageUrl = "/images/products/galaxys24.jpg",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Product
            {
                Id = 3,
                Name = "Nike Air Max",
                Description = "Comfortable running shoes",
                NameAr = "نايك إير ماكس",
                DescriptionAr = "حذاء ركض مريح",
                Price = 129.99m,
                StockQuantity = 100,
                CategoryId = 5,
                IsActive = true,
                IsFeatured = false,
                ImageUrl = "/images/products/nikeairmax.jpg",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Product
            {
                Id = 4,
                Name = "Programming Book",
                Description = "Learn C# programming",
                NameAr = "كتاب البرمجة",
                DescriptionAr = "تعلم برمجة سي شارب",
                Price = 49.99m,
                StockQuantity = 200,
                CategoryId = 3,
                IsActive = true,
                IsFeatured = false,
                ImageUrl = "/images/products/csharpbook.jpg",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Product
            {
                Id = 5,
                Name = "Cotton T-Shirt",
                Description = "Comfortable cotton t-shirt",
                NameAr = "قميص قطني",
                DescriptionAr = "قميص قطني مريح",
                Price = 19.99m,
                StockQuantity = 150,
                CategoryId = 2,
                IsActive = true,
                IsFeatured = false,
                ImageUrl = "/images/products/cottontshirt.jpg",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
        // Seed Coupons
        modelBuilder.Entity<Coupon>().HasData(
            new Coupon
            {
                Id = 1,
                Code = "WELCOME10",
                Name = "Welcome Discount",
                Description = "10% off for new customers",
                Type = CouponType.Percentage,
                Value = 10,
                MinimumOrderAmount = 50,
                MaximumDiscountAmount = 20,
                UsageLimit = 100,
                IsActive = true,
                StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2024, 12, 31),
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Coupon
            {
                Id = 2,
                Code = "SAVE20",
                Name = "Save $20",
                Description = "$20 off on orders over $100",
                Type = CouponType.FixedAmount,
                Value = 20,
                MinimumOrderAmount = 100,
                UsageLimit = 50,
                IsActive = true,
                StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2024, 12, 31),
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Coupon
            {
                Id = 3,
                Code = "FREESHIP",
                Name = "Free Shipping",
                Description = "Free shipping on all orders",
                Type = CouponType.FreeShipping,
                Value = 0,
                MinimumOrderAmount = 25,
                IsActive = true,
                StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2024, 12, 31),
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Coupon
            {
                Id = 4,
                Code = "ELECTRONICS15",
                Name = "Electronics Discount",
                Description = "15% off on electronics",
                Type = CouponType.Percentage,
                Value = 15,
                CategoryId = 1, // Electronics category
                MinimumOrderAmount = 75,
                MaximumDiscountAmount = 50,
                UsageLimit = 30,
                IsActive = true,
                StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2024, 12, 31),
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
        // Seed Email Templates
        modelBuilder.Entity<EmailTemplate>().HasData(
            new EmailTemplate
            {
                Id = 1,
                Name = "Welcome Email",
                Subject = "Welcome to {StoreName}!",
                Type = EmailTemplateType.Welcome,
                HtmlContent = @"
                    <h1>Welcome to {StoreName}!</h1>
                    <p>Dear {FirstName},</p>
                    <p>Thank you for joining our community! We're excited to have you on board.</p>
                    <p>Start exploring our amazing products and enjoy exclusive offers.</p>
                    <p>Best regards,<br>The {StoreName} Team</p>
                ",
                TextContent = "Welcome to {StoreName}! Dear {FirstName}, Thank you for joining our community!",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new EmailTemplate
            {
                Id = 2,
                Name = "Order Confirmation",
                Subject = "Order Confirmation - #{OrderNumber}",
                Type = EmailTemplateType.OrderConfirmation,
                HtmlContent = @"
                    <h1>Order Confirmation</h1>
                    <p>Dear {FirstName},</p>
                    <p>Thank you for your order! Your order #{OrderNumber} has been confirmed.</p>
                    <p><strong>Order Total:</strong> ${TotalAmount}</p>
                    <p>We'll send you another email when your order ships.</p>
                    <p>Best regards,<br>The {StoreName} Team</p>
                ",
                TextContent = "Order Confirmation - #{OrderNumber}. Dear {FirstName}, Thank you for your order!",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new EmailTemplate
            {
                Id = 3,
                Name = "Order Status Update",
                Subject = "Order Update - #{OrderNumber}",
                Type = EmailTemplateType.OrderStatusUpdate,
                HtmlContent = @"
                    <h1>Order Status Update</h1>
                    <p>Dear {FirstName},</p>
                    <p>Your order #{OrderNumber} status has been updated to: <strong>{OrderStatus}</strong></p>
                    <p>You can track your order anytime by visiting your account.</p>
                    <p>Best regards,<br>The {StoreName} Team</p>
                ",
                TextContent = "Order Update - #{OrderNumber}. Your order status: {OrderStatus}",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new EmailTemplate
            {
                Id = 4,
                Name = "Newsletter",
                Subject = "Latest News from {StoreName}",
                Type = EmailTemplateType.Newsletter,
                HtmlContent = @"
                    <h1>Latest News & Offers</h1>
                    <p>Dear Subscriber,</p>
                    <p>Check out our latest products and exclusive offers!</p>
                    <p>{NewsletterContent}</p>
                    <p>Best regards,<br>The {StoreName} Team</p>
                    <p><small><a href='{UnsubscribeUrl}'>Unsubscribe</a></small></p>
                ",
                TextContent = "Latest News from {StoreName}. {NewsletterContent}",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new EmailTemplate
            {
                Id = 5,
                Name = "Low Stock Alert",
                Subject = "Low Stock Alert - {ProductName}",
                Type = EmailTemplateType.LowStock,
                HtmlContent = @"
                    <h1>Low Stock Alert</h1>
                    <p>Dear Admin,</p>
                    <p>The product <strong>{ProductName}</strong> is running low on stock.</p>
                    <p>Current stock: {StockQuantity} units</p>
                    <p>Please restock soon to avoid stockouts.</p>
                ",
                TextContent = "Low Stock Alert - {ProductName}. Current stock: {StockQuantity} units",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
        modelBuilder.Entity<ShippingMethod>().HasData(
            new ShippingMethod
            {
                Id = 1,
                Name = "Standard Shipping",
                Description = "Standard delivery (3-5 business days)",
                Cost = 5.99m,
                EstimatedDays = 5,
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new ShippingMethod
            {
                Id = 2,
                Name = "Express Shipping",
                Description = "Express delivery (1-2 business days)",
                Cost = 12.99m,
                EstimatedDays = 2,
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new ShippingMethod
            {
                Id = 3,
                Name = "Overnight Shipping",
                Description = "Next business day delivery",
                Cost = 24.99m,
                EstimatedDays = 1,
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new ShippingMethod
            {
                Id = 4,
                Name = "Free Shipping",
                Description = "Free shipping on orders over $50",
                Cost = 0.00m,
                EstimatedDays = 7,
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
        modelBuilder.Entity<TaxSetting>().HasData(
            new TaxSetting
            {
                Id = 1,
                Country = "Egypt",
                Region = "Cairo",
                City = "Cairo",
                TaxRate = 14.00m,
                IsActive = true,
                Notes = "Standard VAT rate for Cairo",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new TaxSetting
            {
                Id = 2,
                Country = "Egypt",
                Region = "Alexandria",
                City = "Alexandria",
                TaxRate = 14.00m,
                IsActive = true,
                Notes = "Standard VAT rate for Alexandria",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new TaxSetting
            {
                Id = 3,
                Country = "Saudi Arabia",
                Region = "Riyadh",
                City = "Riyadh",
                TaxRate = 15.00m,
                IsActive = true,
                Notes = "Standard VAT rate for Saudi Arabia",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new TaxSetting
            {
                Id = 4,
                Country = "UAE",
                Region = "Dubai",
                City = "Dubai",
                TaxRate = 5.00m,
                IsActive = true,
                Notes = "Standard VAT rate for UAE",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
