using System.ComponentModel.DataAnnotations;

namespace ECommerceWebsite.Models;

public class TaxSetting
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Country { get; set; } = string.Empty;

    [StringLength(100)]
    public string Region { get; set; } = string.Empty; // State/Province

    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [Range(0, 100)]
    public decimal TaxRate { get; set; } // Percentage (e.g., 15.5 for 15.5%)

    [Required]
    public bool IsActive { get; set; } = true;

    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
