using System.ComponentModel.DataAnnotations;

namespace ECommerceWebsite.Models;

public class ProductImage
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsMain { get; set; } = false;
    public int ProductId { get; set; }

    // Navigation properties
    public virtual Product Product { get; set; } = null!;
}
