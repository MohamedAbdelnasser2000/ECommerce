using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerceWebsite.Models;
using ECommerceWebsite.Repository;

namespace ECommerceWebsite.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ReviewsController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public ReviewsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index(string? status, string? search, int page = 1, int pageSize = 20)
    {
        var reviews = await _unitOfWork.Review.GetAllAsync(
            includeProperties: "Product,User"
        );

        // Apply filters
        if (!string.IsNullOrEmpty(status))
        {
            switch (status.ToLower())
            {
                case "approved":
                    reviews = reviews.Where(r => r.IsApproved);
                    break;
                case "pending":
                    reviews = reviews.Where(r => !r.IsApproved);
                    break;
                case "high":
                    reviews = reviews.Where(r => r.Rating >= 4);
                    break;
                case "low":
                    reviews = reviews.Where(r => r.Rating <= 2);
                    break;
            }
        }

        if (!string.IsNullOrEmpty(search))
        {
            reviews = reviews.Where(r =>
                r.Product.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                r.User.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                r.User.LastName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (r.Comment != null && r.Comment.Contains(search, StringComparison.OrdinalIgnoreCase))
            );
        }

        var orderedReviews = reviews.OrderByDescending(r => r.CreatedAt);
        var pagedReviews = orderedReviews.Skip((page - 1) * pageSize).Take(pageSize);

        ViewBag.CurrentStatus = status;
        ViewBag.CurrentSearch = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)reviews.Count() / pageSize);
        ViewBag.TotalReviews = reviews.Count();

        // Statistics
        ViewBag.TotalApproved = reviews.Count(r => r.IsApproved);
        ViewBag.TotalPending = reviews.Count(r => !r.IsApproved);
        ViewBag.AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

        return View(pagedReviews);
    }

    public async Task<IActionResult> Details(int id)
    {
        var review = await _unitOfWork.Review.GetFirstOrDefaultAsync(
            filter: r => r.Id == id,
            includeProperties: "Product,User,Product.Category"
        );

        if (review == null)
        {
            return NotFound();
        }

        return View(review);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var review = await _unitOfWork.Review.GetByIdAsync(id);
        if (review == null)
        {
            return NotFound();
        }

        review.IsApproved = true;
        review.ApprovedAt = DateTime.Now;

        _unitOfWork.Review.Update(review);
        await _unitOfWork.SaveAsync();

        TempData["Success"] = "Review approved successfully!";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var review = await _unitOfWork.Review.GetByIdAsync(id);
        if (review == null)
        {
            return NotFound();
        }

        review.IsApproved = false;
        review.ApprovedAt = null;

        _unitOfWork.Review.Update(review);
        await _unitOfWork.SaveAsync();

        TempData["Success"] = "Review rejected successfully!";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> QuickApprove(int id)
    {
        var review = await _unitOfWork.Review.GetByIdAsync(id);
        if (review == null)
        {
            return Json(new { success = false, message = "Review not found" });
        }

        review.IsApproved = true;
        review.ApprovedAt = DateTime.Now;

        _unitOfWork.Review.Update(review);
        await _unitOfWork.SaveAsync();

        return Json(new { success = true, message = "Review approved successfully!" });
    }

    [HttpPost]
    public async Task<IActionResult> QuickReject(int id)
    {
        var review = await _unitOfWork.Review.GetByIdAsync(id);
        if (review == null)
        {
            return Json(new { success = false, message = "Review not found" });
        }

        review.IsApproved = false;
        review.ApprovedAt = null;

        _unitOfWork.Review.Update(review);
        await _unitOfWork.SaveAsync();

        return Json(new { success = true, message = "Review rejected successfully!" });
    }

    public async Task<IActionResult> Delete(int id)
    {
        var review = await _unitOfWork.Review.GetFirstOrDefaultAsync(
            filter: r => r.Id == id,
            includeProperties: "Product,User"
        );

        if (review == null)
        {
            return NotFound();
        }

        return View(review);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var review = await _unitOfWork.Review.GetByIdAsync(id);
        if (review == null)
        {
            return NotFound();
        }

        _unitOfWork.Review.Remove(review);
        await _unitOfWork.SaveAsync();

        TempData["Success"] = "Review deleted successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> BulkApprove(int[] reviewIds)
    {
        if (reviewIds == null || reviewIds.Length == 0)
        {
            return Json(new { success = false, message = "No reviews selected" });
        }

        var reviews = await _unitOfWork.Review.GetAllAsync(filter: r => reviewIds.Contains(r.Id));

        foreach (var review in reviews)
        {
            review.IsApproved = true;
            review.ApprovedAt = DateTime.Now;
            _unitOfWork.Review.Update(review);
        }

        await _unitOfWork.SaveAsync();

        return Json(new { success = true, message = $"{reviews.Count()} reviews approved successfully!" });
    }

    [HttpPost]
    public async Task<IActionResult> BulkReject(int[] reviewIds)
    {
        if (reviewIds == null || reviewIds.Length == 0)
        {
            return Json(new { success = false, message = "No reviews selected" });
        }

        var reviews = await _unitOfWork.Review.GetAllAsync(filter: r => reviewIds.Contains(r.Id));

        foreach (var review in reviews)
        {
            review.IsApproved = false;
            review.ApprovedAt = null;
            _unitOfWork.Review.Update(review);
        }

        await _unitOfWork.SaveAsync();

        return Json(new { success = true, message = $"{reviews.Count()} reviews rejected successfully!" });
    }

    [HttpPost]
    public async Task<IActionResult> BulkDelete(int[] reviewIds)
    {
        if (reviewIds == null || reviewIds.Length == 0)
        {
            return Json(new { success = false, message = "No reviews selected" });
        }

        var reviews = await _unitOfWork.Review.GetAllAsync(filter: r => reviewIds.Contains(r.Id));
        _unitOfWork.Review.RemoveRange(reviews);
        await _unitOfWork.SaveAsync();

        return Json(new { success = true, message = $"{reviews.Count()} reviews deleted successfully!" });
    }

    [HttpGet]
    public async Task<IActionResult> GetReviewStats()
    {
        var reviews = await _unitOfWork.Review.GetAllAsync();

        var stats = new
        {
            total = reviews.Count(),
            approved = reviews.Count(r => r.IsApproved),
            pending = reviews.Count(r => !r.IsApproved),
            averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0,
            fiveStars = reviews.Count(r => r.Rating == 5),
            fourStars = reviews.Count(r => r.Rating == 4),
            threeStars = reviews.Count(r => r.Rating == 3),
            twoStars = reviews.Count(r => r.Rating == 2),
            oneStar = reviews.Count(r => r.Rating == 1)
        };

        return Json(stats);
    }

    public async Task<IActionResult> ProductReviews(int productId)
    {
        var product = await _unitOfWork.Product.GetByIdAsync(productId);
        if (product == null)
        {
            return NotFound();
        }

        var reviews = await _unitOfWork.Review.GetAllAsync(
            filter: r => r.ProductId == productId,
            includeProperties: "User"
        );

        ViewBag.Product = product;
        ViewBag.AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
        ViewBag.TotalReviews = reviews.Count();

        return View(reviews.OrderByDescending(r => r.CreatedAt));
    }
}
