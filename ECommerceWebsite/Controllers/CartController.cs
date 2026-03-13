using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ECommerceWebsite.Models;
using ECommerceWebsite.Repository;
using Newtonsoft.Json;
using ECommerceWebsite.Services;

namespace ECommerceWebsite.Controllers;

public class CartController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notificationService;

    public CartController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _notificationService = notificationService;
    }

    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = _userManager.GetUserId(User);
            var cartItems = await _unitOfWork.CartItem.GetAllAsync(
                filter: c => c.UserId == userId,
                includeProperties: "Product,Product.Category"
            );
            return View(cartItems);
        }
        else
        {
            // Handle guest cart from session
            var sessionCart = GetSessionCart();
            var cartItems = new List<CartItem>();

            foreach (var item in sessionCart)
            {
                var product = await _unitOfWork.Product.GetFirstOrDefaultAsync(
                    filter: p => p.Id == item.ProductId,
                    includeProperties: "Category"
                );
                if (product != null)
                {
                    cartItems.Add(new CartItem
                    {
                        ProductId = product.Id,
                        Product = product,
                        Quantity = item.Quantity
                    });
                }
            }

            return View(cartItems);
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
    {
        var product = await _unitOfWork.Product.GetByIdAsync(productId);
        if (product == null || !product.IsActive)
        {
            return Json(new { success = false, message = "Product not found" });
        }

        if (product.StockQuantity < quantity)
        {
            return Json(new { success = false, message = "Insufficient stock" });
        }

        if (User.Identity?.IsAuthenticated == true)
        {
            // Handle authenticated user cart
            var userId = _userManager.GetUserId(User);

            var existingCartItem = await _unitOfWork.CartItem.GetFirstOrDefaultAsync(
                filter: c => c.UserId == userId && c.ProductId == productId
            );

            if (existingCartItem != null)
            {
                existingCartItem.Quantity += quantity;
                _unitOfWork.CartItem.Update(existingCartItem);
            }
            else
            {
                var cartItem = new CartItem
                {
                    UserId = userId!,
                    ProductId = productId,
                    Quantity = quantity
                };
                await _unitOfWork.CartItem.AddAsync(cartItem);
            }

            await _unitOfWork.SaveAsync();

            // Notify user
            await _notificationService.CreateNotificationAsync(
                userId!,
                "Added to Cart",
                $"{product.Name} x{quantity} has been added to your cart.",
                NotificationType.Success,
                "/Cart"
            );

            var cartCount = await GetCartItemCount(userId!);
            return Json(new { success = true, message = "Product added to cart", cartCount });
        }
        else
        {
            // Guest cart in session
            var sessionCart = GetSessionCart();
            var item = sessionCart.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                var newQty = item.Quantity + quantity;
                if (product.StockQuantity < newQty)
                {
                    return Json(new { success = false, message = "Insufficient stock" });
                }
                item.Quantity = newQty;
            }
            else
            {
                sessionCart.Add(new SessionCartItem { ProductId = productId, Quantity = quantity });
            }

            SaveSessionCart(sessionCart);

            var cartCount = sessionCart.Sum(c => c.Quantity);
            return Json(new { success = true, message = "Product added to cart", cartCount });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity, int? productId = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = _userManager.GetUserId(User);
            var cartItem = await _unitOfWork.CartItem.GetFirstOrDefaultAsync(
                filter: c => c.Id == cartItemId && c.UserId == userId,
                includeProperties: "Product"
            );

            if (cartItem == null)
            {
                return Json(new { success = false, message = "Cart item not found" });
            }

            if (quantity <= 0)
            {
                _unitOfWork.CartItem.Remove(cartItem);
                // Notify user about removal
                await _notificationService.CreateNotificationAsync(
                    userId!,
                    "Removed from Cart",
                    $"{cartItem.Product.Name} has been removed from your cart.",
                    NotificationType.Info,
                    "/Cart"
                );
            }
            else if (cartItem.Product.StockQuantity >= quantity)
            {
                cartItem.Quantity = quantity;
                _unitOfWork.CartItem.Update(cartItem);
                // Notify user about update
                await _notificationService.CreateNotificationAsync(
                    userId!,
                    "Cart Updated",
                    $"{cartItem.Product.Name} quantity updated to {quantity}.",
                    NotificationType.Info,
                    "/Cart"
                );
            }
            else
            {
                return Json(new { success = false, message = "Insufficient stock" });
            }

            await _unitOfWork.SaveAsync();

            var cartCount = await GetCartItemCount(userId!);
            var totalPrice = cartItem.Quantity > 0 ? cartItem.TotalPrice : 0;
        
            return Json(new { success = true, cartCount, totalPrice });
        }
        else
        {
            // For guests, update session cart
            var sessionCart = GetSessionCart();
            var item = sessionCart.FirstOrDefault(c => c.ProductId == productId);
            if (item == null)
            {
                return Json(new { success = false, message = "Cart item not found" });
            }

            var product = await _unitOfWork.Product.GetByIdAsync(item.ProductId);
            if (product == null || !product.IsActive)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            if (quantity <= 0)
            {
                sessionCart.Remove(item);
            }
            else if (product.StockQuantity >= quantity)
            {
                item.Quantity = quantity;
            }
            else
            {
                return Json(new { success = false, message = "Insufficient stock" });
            }

            SaveSessionCart(sessionCart);

            var cartCount = sessionCart.Sum(c => c.Quantity);
            var totalPrice = item.Quantity > 0 ? item.Quantity * product.FinalPrice : 0;

            return Json(new { success = true, cartCount, totalPrice });
        }
    }

    [HttpPost]
    public async Task<IActionResult> RemoveFromCart(int cartItemId)
    {
        var userId = _userManager.GetUserId(User);
        var cartItem = await _unitOfWork.CartItem.GetFirstOrDefaultAsync(
            filter: c => c.Id == cartItemId && c.UserId == userId
        );

        if (cartItem == null)
        {
            return Json(new { success = false, message = "Cart item not found" });
        }

        _unitOfWork.CartItem.Remove(cartItem);
        await _unitOfWork.SaveAsync();

        // Notify user
        await _notificationService.CreateNotificationAsync(
            userId!,
            "Removed from Cart",
            "An item was removed from your cart.",
            NotificationType.Info,
            "/Cart"
        );

        var cartCount = await GetCartItemCount(userId!);
        return Json(new { success = true, message = "Item removed from cart", cartCount });
    }

    [HttpPost]
    public async Task<IActionResult> ClearCart()
    {
        var userId = _userManager.GetUserId(User);
        var items = await _unitOfWork.CartItem.GetAllAsync(
            filter: c => c.UserId == userId
        );

        _unitOfWork.CartItem.RemoveRange(items);
        await _unitOfWork.SaveAsync();

        return Json(new { success = true, cartCount = 0, message = "Cart cleared" });
    }

    [HttpGet]
    public async Task<IActionResult> GetCartCount()
    {
        var count = await GetCartItemCount();
        return Json(new { count });
    }
    
    [HttpGet]
    [Route("api/cart/count")]
    public async Task<IActionResult> GetCartCountApi()
    {
        var count = await GetCartItemCount();
        return Json(new { count });
    }

    private async Task<int> GetCartItemCount()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = _userManager.GetUserId(User);
            var cartItems = await _unitOfWork.CartItem.GetAllAsync(
                filter: c => c.UserId == userId
            );
            return cartItems.Sum(c => c.Quantity);
        }
        else
        {
            var sessionCart = GetSessionCart();
            return sessionCart.Sum(c => c.Quantity);
        }
    }

    private async Task<int> GetCartItemCount(string userId)
    {
        var cartItems = await _unitOfWork.CartItem.GetAllAsync(
            filter: c => c.UserId == userId
        );
        return cartItems.Sum(c => c.Quantity);
    }

    private List<SessionCartItem> GetSessionCart()
    {
        var sessionCartJson = HttpContext.Session.GetString("GuestCart");
        if (string.IsNullOrEmpty(sessionCartJson))
        {
            return new List<SessionCartItem>();
        }
        return JsonConvert.DeserializeObject<List<SessionCartItem>>(sessionCartJson) ?? new List<SessionCartItem>();
    }

    private void SaveSessionCart(List<SessionCartItem> cart)
    {
        var cartJson = JsonConvert.SerializeObject(cart);
        HttpContext.Session.SetString("GuestCart", cartJson);
    }
}
