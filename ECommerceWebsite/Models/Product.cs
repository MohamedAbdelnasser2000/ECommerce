using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceWebsite.Models;

public class Product
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(200)]
    public string? NameAr { get; set; }

    [StringLength(1000)]
    public string? DescriptionAr { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DiscountPrice { get; set; }

    [Required]
    public int StockQuantity { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsFeatured { get; set; } = false;

    public bool IsFeaturedInNewCarousel { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    [StringLength(200)]
    public string? ShortDescription { get; set; }

    [StringLength(200)]
    public string? ShortDescriptionAr { get; set; }

    // Foreign Keys
    [Required]
    public int CategoryId { get; set; }

    // Navigation properties
    public virtual Category Category { get; set; } = null!;
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public virtual ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    // Computed properties
    public decimal FinalPrice => DiscountPrice ?? Price;
    public double AverageRating => Reviews.Any() ? Reviews.Average(r => r.Rating) : 0;
    public int ReviewCount => Reviews.Count;

    public string DisplayName
    {
        get
        {
            var culture = System.Globalization.CultureInfo.CurrentUICulture.Name;
            if (culture.StartsWith("ar", System.StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(NameAr))
            {
                return NameAr!;
            }

            return Name;
        }
    }

    public string DisplayDescription
    {
        get
        {
            var culture = System.Globalization.CultureInfo.CurrentUICulture.Name;
            if (culture.StartsWith("ar", System.StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(DescriptionAr))
            {
                return DescriptionAr!;
            }

            return Description;
        }
    }
}
