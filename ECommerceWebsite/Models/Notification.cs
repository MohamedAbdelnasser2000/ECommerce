using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace ECommerceWebsite.Models
{
    public class Notification
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; }
        
        [Required]
        public string Message { get; set; }
        
        public NotificationType Type { get; set; }
        
        public bool IsRead { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [MaxLength(500)]
        public string ActionUrl { get; set; }
        
        [MaxLength(50)]
        public string Icon { get; set; }
        
        // Navigation property
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
        
        // Helper method to get the appropriate badge class based on notification type
        public string GetNotificationBadgeClass()
        {
            return Type switch
            {
                NotificationType.Success => "success",
                NotificationType.Warning => "warning",
                NotificationType.Error => "danger",
                NotificationType.OrderUpdate => "info",
                NotificationType.LowStock => "warning",
                NotificationType.NewReview => "primary",
                _ => "info"
            };
        }
        
        // Helper method to get the appropriate icon if not specified
        public string GetNotificationIcon()
        {
            return Type switch
            {
                NotificationType.Success => "fas fa-check-circle",
                NotificationType.Warning => "fas fa-exclamation-triangle",
                NotificationType.Error => "fas fa-exclamation-circle",
                NotificationType.OrderUpdate => "fas fa-shopping-cart",
                NotificationType.LowStock => "fas fa-boxes",
                NotificationType.NewReview => "fas fa-star",
                _ => "fas fa-info-circle"
            };
        }
    }
}
