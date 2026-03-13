namespace ECommerceWebsite.Models;

// Represents a single cart item stored in session for guest users
public class SessionCartItem
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}