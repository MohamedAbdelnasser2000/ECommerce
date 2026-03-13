using ECommerceWebsite.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerceWebsite.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(string userId, string title, string message, NotificationType type, string? actionUrl = null, string? icon = null);
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false);
        Task<int> GetUnreadCountAsync(string userId);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(string userId);
        Task DeleteNotificationAsync(int notificationId);
        Task CreateOrderNotificationAsync(string userId, Order order, string message);
        Task CreateLowStockNotificationAsync(Product product);
        Task CreateNewReviewNotificationAsync(Review review);
        Task CreateWelcomeNotificationAsync(string userId);
        Task CreateSampleAdminNotificationsAsync(string adminUserId);
        Task<Notification> GetNotificationByIdAsync(int id);
        Task NotifyAdminsNewOrderAsync(Order order);
        Task NotifyAdminsNewContactMessageAsync(ContactMessage message);
        Task MarkNotificationAsRead(int notificationId, string userId);
    }
}