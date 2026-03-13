using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ECommerceWebsite.Repository;
using ECommerceWebsite.Models;
using ECommerceWebsite.Services;

namespace ECommerceWebsite.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class HomeController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notificationService;

    public HomeController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _notificationService = notificationService;
    }

    public async Task<IActionResult> Index()
    {
        // Basic statistics
        var totalProducts = await _unitOfWork.Product.CountAsync();
        var totalCategories = await _unitOfWork.Category.CountAsync();
        var totalOrders = await _unitOfWork.Order.CountAsync();
        var totalUsers = await _unitOfWork.ApplicationUser.CountAsync();

        // Active products
        var activeProducts = await _unitOfWork.Product.CountAsync(filter: p => p.IsActive);
        var inactiveProducts = totalProducts - activeProducts;

        // Orders by status
        var pendingOrders = await _unitOfWork.Order.CountAsync(filter: o => o.Status == Models.OrderStatus.Pending);
        var processingOrders = await _unitOfWork.Order.CountAsync(filter: o => o.Status == Models.OrderStatus.Processing);
        var shippedOrders = await _unitOfWork.Order.CountAsync(filter: o => o.Status == Models.OrderStatus.Shipped);
        var deliveredOrders = await _unitOfWork.Order.CountAsync(filter: o => o.Status == Models.OrderStatus.Delivered);
        var cancelledOrders = await _unitOfWork.Order.CountAsync(filter: o => o.Status == Models.OrderStatus.Cancelled);

        // Recent orders
        var allOrders = await _unitOfWork.Order.GetAllAsync(
            includeProperties: "User,OrderItems,OrderItems.Product"
        );
        var recentOrders = allOrders.OrderByDescending(o => o.OrderDate).Take(5);

        // Low stock products
        var lowStockProducts = await _unitOfWork.Product.GetAllAsync(
            filter: p => p.StockQuantity <= 10 && p.IsActive,
            includeProperties: "Category"
        );

        // Revenue calculations
        var totalRevenue = allOrders.Where(o => o.Status == Models.OrderStatus.Delivered).Sum(o => o.TotalAmount);
        var thisMonthRevenue = allOrders.Where(o => o.Status == Models.OrderStatus.Delivered &&
                                                   o.OrderDate.Month == DateTime.Now.Month &&
                                                   o.OrderDate.Year == DateTime.Now.Year).Sum(o => o.TotalAmount);

        // Top selling products
        var topProducts = allOrders
            .Where(o => o.Status == Models.OrderStatus.Delivered)
            .SelectMany(o => o.OrderItems)
            .GroupBy(oi => oi.Product)
            .Select(g => new { Product = g.Key, TotalSold = g.Sum(oi => oi.Quantity) })
            .OrderByDescending(x => x.TotalSold)
            .Take(5);

        // Products by category
        var allProducts = await _unitOfWork.Product.GetAllAsync(includeProperties: "Category");
        var productsByCategory = allProducts
            .GroupBy(p => p.Category.Name)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count);

        // Coupon statistics
        var totalCoupons = await _unitOfWork.Coupon.CountAsync();
        var activeCoupons = await _unitOfWork.Coupon.CountAsync(filter: c => c.IsActive &&
            c.StartDate <= DateTime.Now &&
            (c.EndDate == null || c.EndDate >= DateTime.Now));
        var expiredCoupons = await _unitOfWork.Coupon.CountAsync(filter: c => c.EndDate != null && c.EndDate < DateTime.Now);
        var totalCouponUsages = await _unitOfWork.CouponUsage.CountAsync();

        // Total discount given through coupons
        var allCouponUsages = await _unitOfWork.CouponUsage.GetAllAsync();
        var totalDiscountGiven = allCouponUsages.Sum(cu => cu.DiscountAmount);

        // Monthly sales data for chart (last 6 months)
        var monthlySales = new List<decimal>();
        var monthLabels = new List<string>();

        for (int i = 5; i >= 0; i--)
        {
            var targetDate = DateTime.Now.AddMonths(-i);
            var monthRevenue = allOrders
                .Where(o => o.Status == Models.OrderStatus.Delivered &&
                           o.OrderDate.Month == targetDate.Month &&
                           o.OrderDate.Year == targetDate.Year)
                .Sum(o => o.TotalAmount);

            monthlySales.Add(monthRevenue);
            monthLabels.Add(targetDate.ToString("MMM yyyy"));
        }

        // Pass data to view
        ViewBag.TotalProducts = totalProducts;
        ViewBag.TotalCategories = totalCategories;
        ViewBag.TotalOrders = totalOrders;
        ViewBag.TotalUsers = totalUsers;
        ViewBag.ActiveProducts = activeProducts;
        ViewBag.InactiveProducts = inactiveProducts;
        ViewBag.PendingOrders = pendingOrders;
        ViewBag.ProcessingOrders = processingOrders;
        ViewBag.ShippedOrders = shippedOrders;
        ViewBag.DeliveredOrders = deliveredOrders;
        ViewBag.CancelledOrders = cancelledOrders;
        ViewBag.RecentOrders = recentOrders;
        ViewBag.LowStockProducts = lowStockProducts.Take(5);
        ViewBag.TotalRevenue = totalRevenue;
        ViewBag.ThisMonthRevenue = thisMonthRevenue;
        ViewBag.TopProducts = topProducts;
        ViewBag.ProductsByCategory = productsByCategory;
        ViewBag.MonthlySales = monthlySales;
        ViewBag.MonthLabels = monthLabels;
        ViewBag.TotalCoupons = totalCoupons;
        ViewBag.ActiveCoupons = activeCoupons;
        ViewBag.ExpiredCoupons = expiredCoupons;
        ViewBag.TotalCouponUsages = totalCouponUsages;
        ViewBag.TotalDiscountGiven = totalDiscountGiven;

        // Contact messages stats for dashboard
        var unreadContacts = await _unitOfWork.ContactMessage.CountAsync(cm => !cm.IsRead);
        var recentContacts = (await _unitOfWork.ContactMessage.GetAllAsync())
            .OrderByDescending(c => c.CreatedAt)
            .Take(5)
            .ToList();

        ViewBag.UnreadContacts = unreadContacts;
        ViewBag.RecentContacts = recentContacts;

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateSampleNotifications()
    {
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                // Create some sample notifications manually
                await _notificationService.CreateNotificationAsync(
                    currentUser.Id,
                    "Welcome to Admin Panel",
                    "You have successfully accessed the admin panel.",
                    NotificationType.Welcome
                );

                await _notificationService.CreateNotificationAsync(
                    currentUser.Id,
                    "System Update",
                    "The system has been updated with new features.",
                    NotificationType.Info
                );

                TempData["Success"] = "Sample notifications created successfully!";
            }
            else
            {
                TempData["Error"] = "Unable to create notifications. User not found.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Failed to create sample notifications.";
        }

        return RedirectToAction("Index");
    }
}
