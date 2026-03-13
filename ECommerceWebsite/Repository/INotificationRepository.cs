using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerceWebsite.Models;

namespace ECommerceWebsite.Repository
{
    public interface INotificationRepository
    {
        Task<Notification> GetByIdAsync(int id);
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false, bool includeSent = true);
        Task<int> GetUnreadCountAsync(string userId);
        Task CreateAsync(Notification notification);
        Task CreateBatchAsync(IEnumerable<Notification> notifications);
        Task MarkAsReadAsync(int id);
        Task MarkAsReadAsync(IEnumerable<int> ids);
        Task MarkAllAsReadAsync(string userId);
        Task DeleteAsync(int id);
        Task<bool> SaveChangesAsync();
        // Note: Real-time/SignalR-related repository methods removed in simplified implementation
    }
}
