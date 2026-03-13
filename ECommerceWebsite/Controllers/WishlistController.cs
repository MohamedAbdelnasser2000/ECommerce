using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ECommerceWebsite.Models;
using ECommerceWebsite.Repository;
using Newtonsoft.Json;
using ECommerceWebsite.Services;

namespace ECommerceWebsite.Controllers;

public class WishlistController : BaseController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notificationService;

    public WishlistController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, INotificationService notificationService) : base(unitOfWork)
    {
        _userManager = userManager;
        _notificationService = notificationService;
    }

    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var wishlistItems = await _unitOfWork.WishlistItem.GetAllAsync(
                filter: w => w.UserId == user.Id,
                includeProperties: "Product,Product.Category"
            );

            return View(wishlistItems);
        }
        else
        {
            // Guest wishlist from session
            var sessionWishlist = GetSessionWishlist();
            var wishlistItems = new List<WishlistItem>();

            foreach (var productId in sessionWishlist)
            {
                var product = await _unitOfWork.Product.GetFirstOrDefaultAsync(
                    filter: p => p.Id == productId,
                    includeProperties: "Category"
                );
                if (product != null)
                {
                    wishlistItems.Add(new WishlistItem
                    {
                        ProductId = product.Id,
                        Product = product,
                        // UserId default is empty string for guests
                        CreatedAt = DateTime.Now
                    });
                }
            }

            return View(wishlistItems);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToWishlist([FromBody] AddToWishlistRequest request)
    {
        try
        {
            var product = await _unitOfWork.Product.GetByIdAsync(request.ProductId);
            if (product == null || !product.IsActive)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Check if already in wishlist
                var existingItem = await _unitOfWork.WishlistItem.GetFirstOrDefaultAsync(
                    filter: w => w.UserId == user.Id && w.ProductId == request.ProductId
                );

                if (existingItem != null)
                {
                    return Json(new { success = false, message = "Product already in wishlist" });
                }

                var wishlistItem = new WishlistItem
                {
                    UserId = user.Id,
                    ProductId = request.ProductId,
                    CreatedAt = DateTime.Now
                };

                await _unitOfWork.WishlistItem.AddAsync(wishlistItem);
                await _unitOfWork.SaveAsync();

                // Notify user
                await _notificationService.CreateNotificationAsync(
                    user.Id,
                    "Added to Wishlist",
                    $"{product.Name} has been added to your wishlist.",
                    NotificationType.Success,
                    "/Wishlist"
                );
            }
            else
            {
                var sessionWishlist = GetSessionWishlist();
                if (sessionWishlist.Contains(request.ProductId))
                {
                    return Json(new { success = false, message = "Product already in wishlist" });
                }
                sessionWishlist.Add(request.ProductId);
                SaveSessionWishlist(sessionWishlist);
            }

            return Json(new { success = true, message = "Product added to wishlist" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error adding to wishlist" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFromWishlist([FromBody] RemoveFromWishlistRequest request)
    {
        try
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var wishlistItem = await _unitOfWork.WishlistItem.GetFirstOrDefaultAsync(
                    filter: w => w.Id == request.WishlistItemId && w.UserId == user.Id
                );

                if (wishlistItem == null)
                {
                    return Json(new { success = false, message = "Item not found in wishlist" });
                }

                _unitOfWork.WishlistItem.Remove(wishlistItem);
                await _unitOfWork.SaveAsync();

                await _notificationService.CreateNotificationAsync(
                    user.Id,
                    "Removed from Wishlist",
                    $"{wishlistItem.ProductId} has been removed from your wishlist.",
                    NotificationType.Info,
                    "/Wishlist"
                );
            }
            else
            {
                // For guests, this method might not be used since no Ids, but if needed
                return Json(new { success = false, message = "Not supported for guests" });
            }

            return Json(new { success = true, message = "Item removed from wishlist" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error removing from wishlist" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveByProductId([FromBody] RemoveByProductIdRequest request)
    {
        try
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var wishlistItem = await _unitOfWork.WishlistItem.GetFirstOrDefaultAsync(
                    filter: w => w.ProductId == request.ProductId && w.UserId == user.Id
                );

                if (wishlistItem == null)
                {
                    return Json(new { success = false, message = "Item not found in wishlist" });
                }

                _unitOfWork.WishlistItem.Remove(wishlistItem);
                await _unitOfWork.SaveAsync();

                await _notificationService.CreateNotificationAsync(
                    user.Id,
                    "Removed from Wishlist",
                    $"Product has been removed from your wishlist.",
                    NotificationType.Info,
                    "/Wishlist"
                );
            }
            else
            {
                var sessionWishlist = GetSessionWishlist();
                if (!sessionWishlist.Contains(request.ProductId))
                {
                    return Json(new { success = false, message = "Item not found in wishlist" });
                }
                sessionWishlist.Remove(request.ProductId);
                SaveSessionWishlist(sessionWishlist);
            }

            return Json(new { success = true, message = "Item removed from wishlist" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error removing from wishlist" });
        }
    }

    // Clear Wishlist
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearWishlist()
    {
        try
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var wishlistItems = await _unitOfWork.WishlistItem.GetAllAsync(
                    filter: w => w.UserId == user.Id
                );

                if (!wishlistItems.Any())
                {
                    return Json(new { success = false, message = "Wishlist is already empty" });
                }

                foreach (var item in wishlistItems)
                {
                    _unitOfWork.WishlistItem.Remove(item);
                }

                await _unitOfWork.SaveAsync();

                await _notificationService.CreateNotificationAsync(
                    user.Id,
                    "Wishlist Cleared",
                    "Your wishlist has been cleared.",
                    NotificationType.Info,
                    "/Wishlist"
                );
            }
            else
            {
                var sessionWishlist = GetSessionWishlist();
                if (!sessionWishlist.Any())
                {
                    return Json(new { success = false, message = "Wishlist is already empty" });
                }
                sessionWishlist.Clear();
                SaveSessionWishlist(sessionWishlist);
            }

            return Json(new { success = true, message = "Wishlist cleared successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error clearing wishlist" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetWishlistCount()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { count = 0 });
            }

            var count = await _unitOfWork.WishlistItem.CountAsync(
                filter: w => w.UserId == user.Id
            );

            return Json(new { count });
        }
        else
        {
            var sessionWishlist = GetSessionWishlist();
            return Json(new { count = sessionWishlist.Count });
        }
    }

    [HttpGet]
    public async Task<IActionResult> IsInWishlist(int productId)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { inWishlist = false });
            }

            var exists = await _unitOfWork.WishlistItem.GetFirstOrDefaultAsync(
                filter: w => w.UserId == user.Id && w.ProductId == productId
            );

            return Json(new { inWishlist = exists != null });
        }
        else
        {
            var sessionWishlist = GetSessionWishlist();
            return Json(new { inWishlist = sessionWishlist.Contains(productId) });
        }
    }

    private List<int> GetSessionWishlist()
    {
        var sessionWishlistJson = HttpContext.Session.GetString("SessionWishlist");
        if (string.IsNullOrEmpty(sessionWishlistJson))
        {
            return new List<int>();
        }
        return JsonConvert.DeserializeObject<List<int>>(sessionWishlistJson) ?? new List<int>();
    }

    private void SaveSessionWishlist(List<int> wishlist)
    {
        var wishlistJson = JsonConvert.SerializeObject(wishlist);
        HttpContext.Session.SetString("SessionWishlist", wishlistJson);
    }
}

// Request models for Wishlist
public class AddToWishlistRequest
{
    public int ProductId { get; set; }
}

// Remove the duplicate property definition in RemoveFromWishlistRequest
public class RemoveFromWishlistRequest
{
    public int WishlistItemId { get; set; }
}

public class RemoveByProductIdRequest
{
    public int ProductId { get; set; }
}
