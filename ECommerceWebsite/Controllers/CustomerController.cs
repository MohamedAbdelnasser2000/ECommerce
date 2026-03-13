using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ECommerceWebsite.Models;
using ECommerceWebsite.Repository;

namespace ECommerceWebsite.Controllers;

[Authorize(Roles = "Customer")]
public class CustomerController : BaseController
{
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomerController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager) : base(unitOfWork)
    {
        _userManager = userManager;
    }

    // Customer Dashboard
    public async Task<IActionResult> Dashboard()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Get customer statistics
        var orders = await _unitOfWork.Order.GetAllAsync(
            filter: o => o.UserId == user.Id,
            includeProperties: "OrderItems,OrderItems.Product"
        );

        var reviews = await _unitOfWork.Review.GetAllAsync(
            filter: r => r.UserId == user.Id,
            includeProperties: "Product"
        );

        var cartItems = await _unitOfWork.CartItem.GetAllAsync(
            filter: c => c.UserId == user.Id,
            includeProperties: "Product"
        );

        var wishlistItems = await _unitOfWork.WishlistItem.GetAllAsync(
            filter: w => w.UserId == user.Id,
            includeProperties: "Product"
        );

        ViewBag.TotalOrders = orders.Count();
        ViewBag.PendingOrders = orders.Count(o => o.Status == OrderStatus.Pending);
        ViewBag.CompletedOrders = orders.Count(o => o.Status == OrderStatus.Delivered);
        ViewBag.TotalSpent = orders.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount);
        ViewBag.TotalReviews = reviews.Count();
        ViewBag.CartItemsCount = cartItems.Count();
        ViewBag.WishlistItemsCount = wishlistItems.Count();

        return View(user);
    }

    // Customer Profile
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ApplicationUser model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (ModelState.IsValid)
        {
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;
            user.City = model.City;
            user.PostalCode = model.PostalCode;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "Profile updated successfully!";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }

        return View(model);
    }

    // Customer Orders
    public async Task<IActionResult> Orders(int page = 1, int pageSize = 10)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var orders = await _unitOfWork.Order.GetAllAsync(
            filter: o => o.UserId == user.Id,
            includeProperties: "OrderItems,OrderItems.Product"
        );

        var orderedOrders = orders.OrderByDescending(o => o.OrderDate);
        var pagedOrders = orderedOrders.Skip((page - 1) * pageSize).Take(pageSize);

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)orders.Count() / pageSize);

        return View(pagedOrders);
    }

    // Order Details
    public async Task<IActionResult> OrderDetails(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var order = await _unitOfWork.Order.GetFirstOrDefaultAsync(
            filter: o => o.Id == id && o.UserId == user.Id,
            includeProperties: "OrderItems,OrderItems.Product,OrderItems.Product.Category"
        );

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    // Customer Reviews
    public async Task<IActionResult> Reviews(int page = 1, int pageSize = 10)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var reviews = await _unitOfWork.Review.GetAllAsync(
            filter: r => r.UserId == user.Id,
            includeProperties: "Product"
        );

        var orderedReviews = reviews.OrderByDescending(r => r.CreatedAt);
        var pagedReviews = orderedReviews.Skip((page - 1) * pageSize).Take(pageSize);

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)reviews.Count() / pageSize);

        return View(pagedReviews);
    }

    // Wishlist
    public async Task<IActionResult> Wishlist()
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

    // Change Password
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (result.Succeeded)
        {
            TempData["Success"] = "Password changed successfully!";
            return RedirectToAction(nameof(Dashboard));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        return View(model);
    }

    // Remove from Wishlist
    [HttpPost]
    public async Task<IActionResult> RemoveFromWishlist([FromBody] RemoveFromWishlistRequest request)
    {
        try
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

            return Json(new { success = true, message = "Item removed from wishlist" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error removing item from wishlist" });
        }
    }

    // Clear Wishlist
    [HttpPost]
    public async Task<IActionResult> ClearWishlist()
    {
        try
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

            return Json(new { success = true, message = "Wishlist cleared successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error clearing wishlist" });
        }
    }

    // Delete Review
    [HttpPost]
    public async Task<IActionResult> DeleteReview([FromBody] DeleteReviewRequest request)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            var review = await _unitOfWork.Review.GetFirstOrDefaultAsync(
                filter: r => r.Id == request.ReviewId && r.UserId == user.Id
            );

            if (review == null)
            {
                return Json(new { success = false, message = "Review not found" });
            }

            _unitOfWork.Review.Remove(review);
            await _unitOfWork.SaveAsync();

            return Json(new { success = true, message = "Review deleted successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error deleting review" });
        }
    }
    // Request models for AJAX calls (shared with WishlistController)
    public class DeleteReviewRequest
    {
        public int ReviewId { get; set; }
    }
}
