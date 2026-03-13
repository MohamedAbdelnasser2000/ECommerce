using ECommerceWebsite.Data;
using ECommerceWebsite.Models;

namespace ECommerceWebsite.Repository;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _db;

    public UnitOfWork(ApplicationDbContext db)
    {
        _db = db;
        Category = new Repository<Category>(_db);
        Product = new Repository<Product>(_db);
        ProductImage = new Repository<ProductImage>(_db);
        Order = new Repository<Order>(_db);
        OrderItem = new Repository<OrderItem>(_db);
        Review = new Repository<Review>(_db);
        CartItem = new Repository<CartItem>(_db);
        WishlistItem = new Repository<WishlistItem>(_db);
        ApplicationUser = new Repository<ApplicationUser>(_db);
        Setting = new SettingRepository(_db);
        Coupon = new CouponRepository(_db);
        CouponUsage = new CouponUsageRepository(_db);
        Notification = new NotificationRepository(_db);
        EmailTemplate = new EmailTemplateRepository(_db);
        Newsletter = new NewsletterRepository(_db);
        EmailLog = new EmailLogRepository(_db);
        ContactMessage = new Repository<ContactMessage>(_db);
        ShippingMethod = new ShippingMethodRepository(_db);
        TaxSetting = new TaxSettingRepository(_db);
    }

    public IRepository<Category> Category { get; private set; }
    public IRepository<Product> Product { get; private set; }
    public IRepository<ProductImage> ProductImage { get; private set; }
    public IRepository<Order> Order { get; private set; }
    public IRepository<OrderItem> OrderItem { get; private set; }
    public IRepository<Review> Review { get; private set; }
    public IRepository<CartItem> CartItem { get; private set; }
    public IRepository<WishlistItem> WishlistItem { get; private set; }
    public IRepository<ApplicationUser> ApplicationUser { get; private set; }
    public ISettingRepository Setting { get; private set; }
    public ICouponRepository Coupon { get; private set; }
    public ICouponUsageRepository CouponUsage { get; private set; }
    public INotificationRepository Notification { get; private set; }
    public IEmailTemplateRepository EmailTemplate { get; private set; }
    public INewsletterRepository Newsletter { get; private set; }
    public IEmailLogRepository EmailLog { get; private set; }
    public IRepository<ContactMessage> ContactMessage { get; private set; }
    public IShippingMethodRepository ShippingMethod { get; private set; }
    public ITaxSettingRepository TaxSetting { get; private set; }

    public async Task SaveAsync()
    {
        await _db.SaveChangesAsync();
    }
}
