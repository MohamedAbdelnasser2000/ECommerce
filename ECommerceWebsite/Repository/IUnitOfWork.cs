using ECommerceWebsite.Models;

namespace ECommerceWebsite.Repository;

public interface IUnitOfWork
{
    IRepository<Category> Category { get; }
    IRepository<Product> Product { get; }
    IRepository<ProductImage> ProductImage { get; }
    IRepository<Order> Order { get; }
    IRepository<OrderItem> OrderItem { get; }
    IRepository<Review> Review { get; }
    IRepository<CartItem> CartItem { get; }
    IRepository<WishlistItem> WishlistItem { get; }
    IRepository<ApplicationUser> ApplicationUser { get; }
    ISettingRepository Setting { get; }
    ICouponRepository Coupon { get; }
    ICouponUsageRepository CouponUsage { get; }
    INotificationRepository Notification { get; }
    IEmailTemplateRepository EmailTemplate { get; }
    INewsletterRepository Newsletter { get; }
    IEmailLogRepository EmailLog { get; }
    IRepository<ContactMessage> ContactMessage { get; }
    IShippingMethodRepository ShippingMethod { get; }
    ITaxSettingRepository TaxSetting { get; }

    Task SaveAsync();
}
