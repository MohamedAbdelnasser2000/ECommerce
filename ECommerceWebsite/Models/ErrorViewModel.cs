using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceWebsite.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}

// Category Model
public class Category
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [StringLength(100)]
    public string? NameAr { get; set; }

    [StringLength(500)]
    public string? DescriptionAr { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    // New: control showing category on homepage
    public bool ShowOnHome { get; set; } = false;

    // New: control showing category in site header navigation
    public bool ShowInHeader { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    // Localized display helpers
    public string DisplayName
    {
        get
        {
            var culture = System.Globalization.CultureInfo.CurrentUICulture.Name;
            if (culture.StartsWith("ar", System.StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(NameAr))
            {
                return NameAr!;
            }

            return Name;
        }
    }

    public string DisplayDescription
    {
        get
        {
            var culture = System.Globalization.CultureInfo.CurrentUICulture.Name;
            if (culture.StartsWith("ar", System.StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(DescriptionAr))
            {
                return DescriptionAr!;
            }

            return Description;
        }
    }
}

// Order Model
public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; } = DateTime.Now;
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    public string? CouponCode { get; set; }
    public int? CouponId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    // Shipping Information
    public string ShippingFirstName { get; set; } = string.Empty;
    public string ShippingLastName { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingPostalCode { get; set; } = string.Empty;
    public string ShippingPhone { get; set; } = string.Empty;

    // Guest information
    [EmailAddress]
    public string? GuestEmail { get; set; }

    public string? Notes { get; set; }
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }

    // Foreign Keys
    public string? UserId { get; set; }

    // Navigation properties
    public virtual ApplicationUser? User { get; set; }
    public virtual Coupon? Coupon { get; set; }
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();

    // Shipping and Tax Integration
    public int? ShippingMethodId { get; set; }
    public virtual ShippingMethod? SelectedShippingMethod { get; set; }

    public int? TaxSettingId { get; set; }
    public virtual TaxSetting? AppliedTaxSetting { get; set; }

    // Additional shipping fields for better integration
    [StringLength(100)]
    public string ShippingMethodName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal ShippingCost { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }

    // Country information for tax calculation
    [StringLength(100)]
    public string ShippingCountry { get; set; } = string.Empty;

    [StringLength(100)]
    public string ShippingRegion { get; set; } = string.Empty; // State/Province
}

// Order Item Model
public class OrderItem
{
    public int Id { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }

    // Foreign Keys
    public int OrderId { get; set; }
    public int ProductId { get; set; }

    // Navigation properties
    public virtual Order Order { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}

// Review Model
public class Review
{
    public int Id { get; set; }
    public int Rating { get; set; } // 1-5 stars
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsApproved { get; set; } = false;
    public DateTime? ApprovedAt { get; set; }

    // Foreign Keys
    public int ProductId { get; set; }
    public string UserId { get; set; } = string.Empty;

    // Navigation properties
    public virtual Product Product { get; set; } = null!;
    public virtual ApplicationUser User { get; set; } = null!;
}

// Cart Item Model
public class CartItem
{
    public int Id { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Foreign Keys
    public int ProductId { get; set; }
    public string UserId { get; set; } = string.Empty;

    // Navigation properties
    public virtual Product Product { get; set; } = null!;
    public virtual ApplicationUser User { get; set; } = null!;

    // Computed properties
    public decimal TotalPrice => Product.FinalPrice * Quantity;
}

// Wishlist Item Model
public class WishlistItem
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Foreign Keys
    public int ProductId { get; set; }
    public string UserId { get; set; } = string.Empty;

    // Navigation properties
    public virtual Product Product { get; set; } = null!;
    public virtual ApplicationUser User { get; set; } = null!;
}

// Enums
public enum OrderStatus
{
    Pending = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}

public enum PaymentMethod
{
    CreditCard = 1,
    CashOnDelivery = 2,
    PayPal = 3,
    Stripe = 4,
    BankTransfer = 5
}

public enum PaymentStatus
{
    Pending = 1,
    Paid = 2,
    Failed = 3,
    Refunded = 4
}

// ViewModels for Reports
public class ReportsViewModel
{
    public int TotalProducts { get; set; }
    public int TotalCategories { get; set; }
    public int TotalOrders { get; set; }
    public int TotalUsers { get; set; }

    public int PendingOrders { get; set; }
    public int ProcessingOrders { get; set; }
    public int ShippedOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public int CancelledOrders { get; set; }

    public int ActiveProducts { get; set; }
    public int InactiveProducts { get; set; }
    public int FeaturedProducts { get; set; }

    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }

    public decimal TotalRevenue { get; set; }

    public List<Order> RecentOrders { get; set; } = new();
}

public class SalesReportViewModel
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<Order> Orders { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
}

public class ProductReportItem
{
    public Product Product { get; set; } = null!;
    public int TotalSold { get; set; }
    public decimal Revenue { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}

public class CustomerReportItem
{
    public ApplicationUser User { get; set; } = null!;
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? LastOrderDate { get; set; }
}

// ViewModels for Settings
public class SettingsViewModel
{
    // General Settings
    public string SiteName { get; set; } = string.Empty;
    public string SiteDescription { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string TimeZone { get { return _timeZone; } set { _timeZone = value; } }
    private string _timeZone = string.Empty;
    public string Language { get; set; } = string.Empty;

    // Email Settings
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public bool EnableSsl { get; set; }

    // Payment Settings
    public string PayPalClientId { get; set; } = string.Empty;
    public string PayPalClientSecret { get; set; } = string.Empty;
    public string StripePublishableKey { get; set; } = string.Empty;
    public string StripeSecretKey { get; set; } = string.Empty;

    // General Settings
    public int ItemsPerPage { get; set; }

    // Feature Toggles
    public bool EnableRegistration { get; set; }
    public bool EnableReviews { get; set; }
    public bool EnableWishlist { get; set; }

    // SEO Settings
    public string MetaTitle { get; set; } = string.Empty;
    public string MetaDescription { get; set; } = string.Empty;
    public string MetaKeywords { get; set; } = string.Empty;
    public string GoogleAnalyticsId { get; set; } = string.Empty;
    public string FacebookPixelId { get; set; } = string.Empty;

    // Social Media
    public string FacebookUrl { get; set; } = string.Empty;
    public string TwitterUrl { get; set; } = string.Empty;
    public string InstagramUrl { get; set; } = string.Empty;
    public string LinkedInUrl { get; set; } = string.Empty;
    public string YouTubeUrl { get; set; } = string.Empty;

    // Homepage Hero Settings
    public string HeroTitle { get; set; } = string.Empty;
    public string HeroSubtitle { get; set; } = string.Empty;
    public string HeroBadge { get; set; } = string.Empty;
    public string HeroButtonText { get; set; } = string.Empty;
    public string HeroButtonLink { get; set; } = string.Empty;
    public string HeroImageUrl { get; set; } = string.Empty;

    // Homepage Hero Settings (Arabic)
    public string HeroTitleAr { get; set; } = string.Empty;
    public string HeroSubtitleAr { get; set; } = string.Empty;
    public string HeroBadgeAr { get; set; } = string.Empty;
    public string HeroButtonTextAr { get; set; } = string.Empty;

    // For file upload (not mapped to DB)
    [NotMapped]
    public IFormFile? HeroImageFile { get; set; }

    // Homepage CTA Settings
    public string CtaBadgeText { get; set; } = string.Empty;      // e.g., -60%
    public string CtaTitle { get; set; } = string.Empty;          // e.g., Global Sale
    public string CtaDescription { get; set; } = string.Empty;    // e.g., Short descriptive line under title
    public string CtaButtonText { get; set; } = string.Empty;     // e.g., Buy Now
    public string CtaButtonLink { get; set; } = string.Empty;     // e.g., /Product
    public string CtaBackgroundUrl { get; set; } = string.Empty;  // background image url

    // Homepage CTA Settings (Arabic)
    public string CtaBadgeTextAr { get; set; } = string.Empty;
    public string CtaTitleAr { get; set; } = string.Empty;
    public string CtaDescriptionAr { get; set; } = string.Empty;
    public string CtaButtonTextAr { get; set; } = string.Empty;

    [NotMapped]
    public IFormFile? CtaBackgroundFile { get; set; }             // for upload
}

// Advanced analytics view models were moved to ViewModels/AnalyticsViewModels.cs for maintainability.

// Coupon Model
public class Coupon
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public CouponType Type { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Value { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MinimumOrderAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MaximumDiscountAmount { get; set; }

    public int? UsageLimit { get; set; }

    public int UsedCount { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime StartDate { get; set; } = DateTime.Now;

    public DateTime? EndDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    // Foreign Keys
    public int? CategoryId { get; set; }
    public int? ProductId { get; set; }

    // Navigation Properties
    public virtual Category? Category { get; set; }
    public virtual Product? Product { get; set; }
    public virtual ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();
}

// Coupon Usage Model (to track who used which coupon)
public class CouponUsage
{
    public int Id { get; set; }

    [Required]
    public int CouponId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public int OrderId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; }

    public DateTime UsedAt { get; set; } = DateTime.Now;

    // Navigation Properties
    public virtual Coupon Coupon { get; set; } = null!;
    public virtual ApplicationUser User { get; set; } = null!;
    public virtual Order Order { get; set; } = null!;
}

// Coupon Types Enum
public enum CouponType
{
    [Display(Name = "Percentage")]
    Percentage = 1,

    [Display(Name = "Fixed Amount")]
    FixedAmount = 2,

    [Display(Name = "Free Shipping")]
    FreeShipping = 3,

    [Display(Name = "Buy One Get One")]
    BOGO = 4
}

public class ChangePasswordViewModel
{
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class CreateUserViewModel
{
    [Required]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Address")]
    public string? Address { get; set; }

    [Display(Name = "City")]
    public string? City { get; set; }

    [Display(Name = "Postal Code")]
    public string? PostalCode { get; set; }

    [Required]
    [Display(Name = "Role")]
    public string Role { get; set; } = string.Empty;

    [Display(Name = "Email Confirmed")]
    public bool EmailConfirmed { get; set; } = true;

    [Display(Name = "Send Welcome Email")]
    public bool SendWelcomeEmail { get; set; } = true;
}

public class CheckoutViewModel
{
    [Required]
    [Display(Name = "First Name")]
    public string ShippingFirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Last Name")]
    public string ShippingLastName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Address")]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required]
    [Display(Name = "City")]
    public string ShippingCity { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Postal Code")]
    public string ShippingPostalCode { get; set; } = string.Empty;

    [Required]
    [Phone]
    [Display(Name = "Phone Number")]
    public string ShippingPhone { get; set; } = string.Empty;

    [EmailAddress]
    [Display(Name = "Email Address")]
    public string? GuestEmail { get; set; }

    [Required]
    [Display(Name = "Payment Method")]
    public PaymentMethod PaymentMethod { get; set; }

    [Display(Name = "Order Notes")]
    public string? Notes { get; set; }

    [Display(Name = "Coupon Code")]
    public string? CouponCode { get; set; }

    // Credit Card fields (if needed)
    [Display(Name = "Card Number")]
    public string? CardNumber { get; set; }

    [Display(Name = "Expiry Month")]
    public int? ExpiryMonth { get; set; }

    [Display(Name = "Expiry Year")]
    public int? ExpiryYear { get; set; }

    [Display(Name = "CVV")]
    public string? CVV { get; set; }

    [Display(Name = "Cardholder Name")]
    public string? CardholderName { get; set; }

    // Cart items (for display)
    public List<CartItem> CartItems { get; set; } = new List<CartItem>();

    // Shipping and Tax Settings
    public int? SelectedShippingMethodId { get; set; }
    public List<ShippingMethod> AvailableShippingMethods { get; set; } = new List<ShippingMethod>();
    public List<TaxSetting> AvailableTaxSettings { get; set; } = new List<TaxSetting>();
    public int? AppliedTaxSettingId { get; set; }
    public string ShippingCountry { get; set; } = string.Empty;
    public string ShippingRegion { get; set; } = string.Empty;

    // Calculated totals with shipping and tax integration
    public decimal Subtotal => CartItems.Sum(item => item.TotalPrice);
    public decimal Shipping => SelectedShippingMethodId.HasValue ?
        AvailableShippingMethods.FirstOrDefault(sm => sm.Id == SelectedShippingMethodId)?.Cost ?? 0 :
        0;
    public decimal Tax => AppliedTaxSettingId.HasValue ?
        Subtotal * (AvailableTaxSettings.FirstOrDefault(ts => ts.Id == AppliedTaxSettingId)?.TaxRate ?? 0) / 100 :
        0;
    public decimal DiscountAmount { get; set; } = 0;
    public string? AppliedCouponCode { get; set; }
    public decimal Total => Subtotal + Shipping + Tax - DiscountAmount;

    // Settable properties for form posting
    public decimal SubtotalValue { get; set; }
    public decimal ShippingValue { get; set; }
    public decimal TaxValue { get; set; }
    public decimal TotalValue { get; set; }
}

public class BackupViewModel
{
    public DateTime? LastBackupDate { get; set; }
    public string BackupSize { get; set; } = string.Empty;
    public string BackupLocation { get; set; } = string.Empty;
    public bool AutoBackupEnabled { get; set; }
    public string BackupFrequency { get; set; } = string.Empty;
}

public class MaintenanceViewModel
{
    public bool MaintenanceMode { get; set; }
    public string MaintenanceMessage { get; set; } = string.Empty;
    public List<string> AllowedIPs { get; set; } = new();
    public bool CacheEnabled { get; set; }
    public int CacheExpiration { get; set; }
}

// Coupon ViewModels
public class CouponViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Coupon Code")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    [Display(Name = "Coupon Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Coupon Type")]
    public CouponType Type { get; set; }

    [Required]
    [Display(Name = "Value")]
    public decimal Value { get; set; }

    [Display(Name = "Minimum Order Amount")]
    public decimal? MinimumOrderAmount { get; set; }

    [Display(Name = "Maximum Discount Amount")]
    public decimal? MaximumDiscountAmount { get; set; }

    [Display(Name = "Usage Limit")]
    public int? UsageLimit { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Required]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; } = DateTime.Now;

    [Display(Name = "End Date")]
    public DateTime? EndDate { get; set; }

    [Display(Name = "Category")]
    public int? CategoryId { get; set; }

    [Display(Name = "Product")]
    public int? ProductId { get; set; }

    // For display
    public int UsedCount { get; set; }
    public string? CategoryName { get; set; }
    public string? ProductName { get; set; }
}

public class ApplyCouponRequest
{
    [Required]
    public string CouponCode { get; set; } = string.Empty;
}

public class CouponValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public Coupon? Coupon { get; set; }
}


public class EmailTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string TextContent { get; set; } = string.Empty;
    public EmailTemplateType Type { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}

public enum EmailTemplateType
{
    Welcome = 1,
    OrderConfirmation = 2,
    OrderStatusUpdate = 3,
    Newsletter = 4,
    LowStock = 5,
    NewReview = 6,
    PasswordReset = 7,
    AccountActivation = 8
}

public class NewsletterSubscription
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime SubscribedAt { get; set; } = DateTime.Now;
    public DateTime? UnsubscribedAt { get; set; }
    public string? UnsubscribeToken { get; set; }
}

public class EmailLog
{
    public int Id { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public EmailStatus Status { get; set; }
    public DateTime SentAt { get; set; } = DateTime.Now;
    public string? ErrorMessage { get; set; }
    public int? TemplateId { get; set; }
    public EmailTemplate? Template { get; set; }
}

public enum EmailStatus
{
    Pending = 1,
    Sent = 2,
    Failed = 3,
    Bounced = 4
}
