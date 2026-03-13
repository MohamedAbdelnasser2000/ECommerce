using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerceWebsite.Data;
using ECommerceWebsite.Models;

namespace ECommerceWebsite.Repository
{
    public class NotificationRepository : Repository<Notification>, INotificationRepository
    {
        private readonly ApplicationDbContext _context;

        public NotificationRepository(ApplicationDbContext context) : base(context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Notification> GetByIdAsync(int id)
        {
            return await _context.Notifications.FindAsync(id);
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false, bool includeSent = true)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            // includeSent parameter is ignored in simplified model (no IsSent field)

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task CreateAsync(Notification notification)
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));
            
            notification.CreatedAt = DateTime.UtcNow;
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
        }

        public async Task CreateBatchAsync(IEnumerable<Notification> notifications)
        {
            if (notifications == null) throw new ArgumentNullException(nameof(notifications));
            
            var notificationList = notifications.ToList();
            var now = DateTime.UtcNow;
            
            foreach (var notification in notificationList)
            {
                notification.CreatedAt = now;
                await _context.Notifications.AddAsync(notification);
            }
            
            await _context.SaveChangesAsync();
        }

        public async Task MarkAsReadAsync(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                _context.Notifications.Update(notification);
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAsReadAsync(IEnumerable<int> ids)
        {
            var idList = ids?.ToList();
            if (idList == null || !idList.Any()) return;
            
            var notifications = await _context.Notifications
                .Where(n => idList.Contains(n.Id) && !n.IsRead)
                .ToListAsync();
                
            var now = DateTime.UtcNow;
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }
            
            if (notifications.Any())
            {
                _context.Notifications.UpdateRange(notifications);
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();
                
            var now = DateTime.UtcNow;
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }
            
            if (unreadNotifications.Any())
            {
                _context.Notifications.UpdateRange(unreadNotifications);
                await _context.SaveChangesAsync();
            }
        }


        public async Task DeleteAsync(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

    }
}
