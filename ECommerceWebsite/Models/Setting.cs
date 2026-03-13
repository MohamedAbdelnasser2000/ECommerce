using System.ComponentModel.DataAnnotations;

namespace ECommerceWebsite.Models;

public class Setting
{
    [Key]
    public string Key { get; set; } = string.Empty;
    
    public string Value { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
