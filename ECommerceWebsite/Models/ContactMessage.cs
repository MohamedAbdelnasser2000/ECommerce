using System;
using System.ComponentModel.DataAnnotations;

namespace ECommerceWebsite.Models
{
    public class ContactMessage
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(120)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [MaxLength(4000)]
        public string Message { get; set; } = string.Empty;

        [Phone]
        [MaxLength(30)]
        public string? Phone { get; set; }

        public bool Consent { get; set; } = true;

        public bool IsRead { get; set; } = false;

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(512)]
        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
