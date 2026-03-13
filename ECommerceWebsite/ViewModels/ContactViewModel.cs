using System.ComponentModel.DataAnnotations;

namespace ECommerceWebsite.ViewModels
{
    public class ContactViewModel
    {
        [Required]
        [Display(Name = "Name")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Subject")]
        [StringLength(120)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Message")]
        [StringLength(4000, MinimumLength = 10)]
        public string Message { get; set; } = string.Empty;

        // Optional: for phone and consent
        [Phone]
        [Display(Name = "Phone")]
        public string? Phone { get; set; }

        [Display(Name = "I agree to be contacted back")]
        public bool Consent { get; set; } = true;
    }
}