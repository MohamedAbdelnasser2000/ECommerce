using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceWebsite.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Existing properties...
        
        // Navigation property for notifications
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        
        // Notification preferences
        public bool ReceiveEmailNotifications { get; set; } = true;
        public bool ReceiveInAppNotifications { get; set; } = true;
        public bool ReceiveOrderNotifications { get; set; } = true;
        public bool ReceiveStockNotifications { get; set; } = true;
        public bool ReceivePromotionalNotifications { get; set; } = true;
        
        // Additional user properties
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;
        
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();
        
        // Address information
        [StringLength(500)]
        public string Address { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string City { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string PostalCode { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string Country { get; set; } = string.Empty;
        
        // Account settings
        public string ProfileImageUrl { get; set; } = string.Empty;
        
        public DateTime? DateOfBirth { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastLoginDate { get; set; }
        
        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
        public virtual ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();
        
        // Helper method to check if user should receive a specific type of notification
        public bool ShouldReceiveNotification(NotificationType type)
        {
            if (!ReceiveInAppNotifications) return false;
            
            return type switch
            {
                NotificationType.OrderUpdate => ReceiveOrderNotifications,
                NotificationType.LowStock => ReceiveStockNotifications,
                _ => true
            };
        }
    }
}
