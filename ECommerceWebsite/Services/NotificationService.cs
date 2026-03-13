using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerceWebsite.Data;
using ECommerceWebsite.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using ECommerceWebsite.Hubs;
using ECommerceWebsite.Services;

namespace ECommerceWebsite.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationService> _logger;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IEmailService _emailService;
    private readonly IEmailBackgroundQueue _emailQueue;

    public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger, IHubContext<NotificationHub> hubContext, IEmailService emailService, IEmailBackgroundQueue emailQueue)
    {
        _context = context;
        _logger = logger;
        _hubContext = hubContext;
        _emailService = emailService;
        _emailQueue = emailQueue;
    }

    public async Task NotifyAdminsNewOrderAsync(Order order)
    {
        try
        {
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            if (adminRole == null)
            {
                _logger.LogWarning("Admin role not found when trying to notify about new order.");
                return;
            }

            var adminUserIds = await _context.UserRoles
                .Where(ur => ur.RoleId == adminRole.Id)
                .Select(ur => ur.UserId)
                .ToListAsync();

            var customerEmail = order.UserId != null
                ? (await _context.Users.Where(u => u.Id == order.UserId).Select(u => u.Email).FirstOrDefaultAsync())
                : order.GuestEmail;

            foreach (var adminUserId in adminUserIds)
            {
                await CreateNotificationAsync(
                    adminUserId,
                    "New Order Placed",
                    $"Order #{order.OrderNumber} placed for {order.TotalAmount:C} by {(string.IsNullOrWhiteSpace(customerEmail) ? "guest" : customerEmail)}",
                    NotificationType.OrderUpdate,
                    $"/Admin/Order/Details/{order.Id}",
                    "fas fa-shopping-cart"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify admins about new order {OrderId}", order?.Id);
        }
    }

    public async Task CreateNotificationAsync(string userId, string title, string message, NotificationType type, string? actionUrl = null, string? icon = null)
    {
        try
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                ActionUrl = actionUrl,
                Icon = icon ?? GetDefaultIcon(type),
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Broadcast to the specific user via SignalR
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", new
            {
                id = notification.Id,
                title = notification.Title,
                message = notification.Message,
                type = notification.Type.ToString(),
                isRead = notification.IsRead,
                createdAt = notification.CreatedAt,
                actionUrl = notification.ActionUrl,
                icon = notification.Icon
            });

            _logger.LogInformation("Notification created for user {UserId}: {Title}", userId, title);

            // Send email alongside in-site notification (enqueue for background processing)
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                {
                    var subject = title;
                    var html = $"<h3>{title}</h3><p>{message}</p>" + (string.IsNullOrWhiteSpace(actionUrl) ? string.Empty : $"<p><a href=\"{actionUrl}\">View</a></p>");
                    await _emailQueue.QueueEmailAsync(new EmailQueueItem(user.Email, subject, html, message));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue email for notification to user {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification for user {UserId}", userId);
        }
    }

    public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false)
    {
        var query = _context.Notifications.Where(n => n.UserId == userId);
        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Notification {NotificationId} marked as read", notificationId);
        }
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        try
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark all notifications as read for user {UserId}", userId);
        }
    }

    public async Task DeleteNotificationAsync(int notificationId)
    {
        try
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete notification {NotificationId}", notificationId);
        }
    }

    public async Task CreateOrderNotificationAsync(string userId, Order order, string message)
    {
        await CreateNotificationAsync(
            userId,
            $"Order Update - #{order.OrderNumber}",
            message,
            NotificationType.OrderUpdate,
            $"/Customer/Orders/Details/{order.Id}",
            "fas fa-shopping-cart"
        );
    }

    public async Task CreateLowStockNotificationAsync(Product product)
    {
        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole != null)
        {
            var adminUsers = await _context.UserRoles
                .Where(ur => ur.RoleId == adminRole.Id)
                .Select(ur => ur.UserId)
                .ToListAsync();

            foreach (var adminUserId in adminUsers)
            {
                await CreateNotificationAsync(
                    adminUserId,
                    "Low Stock Alert",
                    $"Product '{product.Name}' is running low on stock ({product.StockQuantity} units remaining)",
                    NotificationType.LowStock,
                    $"/Admin/Product/Edit/{product.Id}",
                    "fas fa-exclamation-triangle"
                );
            }
        }
    }

    public async Task CreateNewReviewNotificationAsync(Review review)
    {
        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole != null)
        {
            var adminUsers = await _context.UserRoles
                .Where(ur => ur.RoleId == adminRole.Id)
                .Select(ur => ur.UserId)
                .ToListAsync();

            foreach (var adminUserId in adminUsers)
            {
                await CreateNotificationAsync(
                    adminUserId,
                    "New Review Received",
                    $"A new {review.Rating}-star review has been submitted for '{review.Product?.Name}'",
                    NotificationType.NewReview,
                    $"/Admin/Reviews/Details/{review.Id}",
                    "fas fa-star"
                );
            }
        }
    }

    public async Task CreateWelcomeNotificationAsync(string userId)
    {
        await CreateNotificationAsync(
            userId,
            "Welcome to our store!",
            "Thank you for joining our community. Start exploring our amazing products!",
            NotificationType.Welcome,
            "/Product",
            "fas fa-heart"
        );
    }

    public async Task<Notification> GetNotificationByIdAsync(int id)
    {
        return await _context.Notifications.FindAsync(id);
    }

    public async Task<IEnumerable<Notification>> GetPendingNotificationsForUser(string userId, int maxCount = 50)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(maxCount)
            .ToListAsync();
    }

    public async Task MarkNotificationAsRead(int notificationId, string userId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification != null && notification.UserId == userId && !notification.IsRead)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task NotifyAdminsNewContactMessageAsync(ContactMessage message)
    {
        try
        {
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            if (adminRole == null)
            {
                _logger.LogWarning("Admin role not found when trying to notify about new contact message.");
                return;
            }

            var adminUserIds = await _context.UserRoles
                .Where(ur => ur.RoleId == adminRole.Id)
                .Select(ur => ur.UserId)
                .ToListAsync();

            foreach (var adminUserId in adminUserIds)
            {
                await CreateNotificationAsync(
                    adminUserId,
                    "New Contact Message",
                    $"From: {message.Name} ({message.Email}) - Subject: {message.Subject}",
                    NotificationType.Info,
                    $"/Admin/Contact/Details/{message.Id}",
                    "fas fa-envelope"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify admins about new contact message");
        }
    }

    private string GetDefaultIcon(NotificationType type)
    {
        return type switch
        {
            NotificationType.Info => "fas fa-info-circle",
            NotificationType.Success => "fas fa-check-circle",
            NotificationType.Warning => "fas fa-exclamation-triangle",
            NotificationType.Error => "fas fa-times-circle",
            NotificationType.OrderUpdate => "fas fa-shopping-cart",
            NotificationType.LowStock => "fas fa-exclamation-triangle",
            NotificationType.NewReview => "fas fa-star",
            NotificationType.Welcome => "fas fa-heart",
            NotificationType.Newsletter => "fas fa-envelope",
            _ => "fas fa-bell"
        };
    }

    public async Task CreateSampleAdminNotificationsAsync(string adminUserId)
    {
        try
        {
            var notifications = new List<Notification>
            {
                new Notification
                {
                    UserId = adminUserId,
                    Type = NotificationType.LowStock,
                    Title = "Low Stock Alert",
                    Message = "Product 'Wireless Headphones' is running low on stock (5 items remaining)",
                    Icon = "fas fa-exclamation-triangle",
                    ActionUrl = "/Admin/Product",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                    IsRead = false
                },
                new Notification
                {
                    UserId = adminUserId,
                    Type = NotificationType.OrderUpdate,
                    Title = "New Order Received",
                    Message = "Order #ORD-2025-001 has been placed by customer@example.com",
                    Icon = "fas fa-shopping-cart",
                    ActionUrl = "/Admin/Order",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-25),
                    IsRead = false
                },
                new Notification
                {
                    UserId = adminUserId,
                    Type = NotificationType.NewReview,
                    Title = "New Product Review",
                    Message = "John Doe left a 5-star review for 'Gaming Laptop'",
                    Icon = "fas fa-star",
                    ActionUrl = "/Admin/Reviews",
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    IsRead = false
                },
                new Notification
                {
                    UserId = adminUserId,
                    Type = NotificationType.Success,
                    Title = "Newsletter Sent",
                    Message = "Monthly newsletter has been successfully sent to 150 subscribers",
                    Icon = "fas fa-envelope",
                    ActionUrl = "/Admin/Email/Newsletter",
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                    IsRead = true
                }
            };

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create sample admin notifications");
        }
    }
}
