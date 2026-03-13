using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using ECommerceWebsite.Repository;
using ECommerceWebsite.Models;
using ECommerceWebsite.Extensions;
using ECommerceWebsite.Services;

namespace ECommerceWebsite.Controllers;

public class ProductController : BaseController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notificationService;
    private readonly ICacheService _cache;

    public ProductController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, INotificationService notificationService, ICacheService cache) : base(unitOfWork)
    {
        _userManager = userManager;
        _notificationService = notificationService;
        _cache = cache;
    }

    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<IActionResult> Index(
        int? categoryId,
        string? search,
        string? sort = "newest",
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? view = "grid",
        int page = 1,
        int pageSize = 12)
    {
        var products = await _unitOfWork.Product.GetAllAsync(
            filter: p => p.IsActive && p.StockQuantity > 0 &&
                        (categoryId == null || p.CategoryId == categoryId) &&
                        (
                            string.IsNullOrEmpty(search)
                            || p.Name.Contains(search)
                            || p.Description.Contains(search)
                            || (p.NameAr != null && p.NameAr.Contains(search))
                            || (p.DescriptionAr != null && p.DescriptionAr.Contains(search))
                        ),
            includeProperties: "Category"
        );


        // Apply price range on FinalPrice (DiscountPrice ?? Price)
        if (minPrice.HasValue)
        {
            products = products.Where(p => (p.DiscountPrice ?? p.Price) >= minPrice.Value);
        }
        if (maxPrice.HasValue)
        {
            products = products.Where(p => (p.DiscountPrice ?? p.Price) <= maxPrice.Value);
        }

        // Sorting
        products = sort switch
        {
            "price-low" => products.OrderBy(p => (p.DiscountPrice ?? p.Price)),
            "price-high" => products.OrderByDescending(p => (p.DiscountPrice ?? p.Price)),
            "popular" => products.OrderByDescending(p => p.OrderItems.Count), // fallback: may be zero if not loaded
            "top-rated" => products.OrderByDescending(p => p.AverageRating),
            _ => products.OrderByDescending(p => p.CreatedAt) // newest default
        };

        var total = products.Count();
        var pagedProducts = products.Skip((page - 1) * pageSize).Take(pageSize);
        
        // Cache active categories for 10 minutes to speed up page rendering
        ViewBag.Categories = await _cache.GetOrCreateAsync(
            key: "categories:active",
            factory: () => _unitOfWork.Category.GetAllAsync(filter: c => c.IsActive),
            absoluteExpiration: TimeSpan.FromMinutes(10)
        );
        ViewBag.CurrentCategory = categoryId;
        ViewBag.CurrentSearch = search;
        ViewBag.SortBy = sort;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.ViewMode = string.IsNullOrWhiteSpace(view) ? "grid" : view;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
        
        return View(pagedProducts);
    }

    // New action for modern product page
    public async Task<IActionResult> ModernIndex(
        int? categoryId,
        string? search,
        string? sort = "newest",
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? view = "grid",
        int page = 1,
        int pageSize = 12)
    {
        var products = await _unitOfWork.Product.GetAllAsync(
            filter: p => p.IsActive &&
                        (categoryId == null || p.CategoryId == categoryId) &&
                        (
                            string.IsNullOrEmpty(search)
                            || p.Name.Contains(search)
                            || p.Description.Contains(search)
                            || (p.NameAr != null && p.NameAr.Contains(search))
                            || (p.DescriptionAr != null && p.DescriptionAr.Contains(search))
                        ),
            includeProperties: "Category"
        );


        // Apply price range on FinalPrice (DiscountPrice ?? Price)
        if (minPrice.HasValue)
        {
            products = products.Where(p => (p.DiscountPrice ?? p.Price) >= minPrice.Value);
        }
        if (maxPrice.HasValue)
        {
            products = products.Where(p => (p.DiscountPrice ?? p.Price) <= maxPrice.Value);
        }

        // Sorting
        products = sort switch
        {
            "price-low" => products.OrderBy(p => (p.DiscountPrice ?? p.Price)),
            "price-high" => products.OrderByDescending(p => (p.DiscountPrice ?? p.Price)),
            "popular" => products.OrderByDescending(p => p.OrderItems.Count), // fallback: may be zero if not loaded
            "top-rated" => products.OrderByDescending(p => p.AverageRating),
            _ => products.OrderByDescending(p => p.CreatedAt) // newest default
        };

        var total = products.Count();
        var pagedProducts = products.Skip((page - 1) * pageSize).Take(pageSize);
        
        ViewBag.Categories = await _unitOfWork.Category.GetAllAsync(filter: c => c.IsActive);
        ViewBag.CurrentCategory = categoryId;
        ViewBag.CurrentSearch = search;
        ViewBag.SortBy = sort;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.ViewMode = string.IsNullOrWhiteSpace(view) ? "grid" : view;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
        
        return View("ModernIndex", pagedProducts);
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _unitOfWork.Product.GetFirstOrDefaultAsync(
            filter: p => p.Id == id && p.IsActive,
            includeProperties: "Category,ProductImages,Reviews,Reviews.User"
        );

        if (product == null)
        {
            return NotFound();
        }

        // Related products: same category, active, in stock, exclude current; order by popularity then newest
        var related = (await _unitOfWork.Product.GetAllAsync(
            filter: p => p.IsActive && p.StockQuantity > 0 && p.CategoryId == product.CategoryId && p.Id != product.Id,
            includeProperties: "Category"
        ))
        .OrderByDescending(p => p.OrderItems.Count)
        .ThenByDescending(p => p.CreatedAt)
        .Take(8)
        .ToList();

        ViewBag.RelatedProducts = related;
        return View(product);
    }

    public async Task<IActionResult> Search(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return RedirectToAction("Index");
        }

        var products = await _unitOfWork.Product.GetAllAsync(
            filter: p => p.IsActive && p.StockQuantity > 0 && (
                p.Name.Contains(query) || p.Description.Contains(query)
                || (p.NameAr != null && p.NameAr.Contains(query))
                || (p.DescriptionAr != null && p.DescriptionAr.Contains(query))
            ),
            includeProperties: "Category"
        );


        ViewBag.SearchQuery = query;
        return View("Index", products);
    }

    public async Task<IActionResult> Category(int id)
    {
        var category = await _unitOfWork.Category.GetByIdAsync(id);
        if (category == null || !category.IsActive)
        {
            return NotFound();
        }

        var products = await _unitOfWork.Product.GetAllAsync(
            filter: p => p.CategoryId == id && p.IsActive && p.StockQuantity > 0,
            includeProperties: "Category"
        );

        ViewBag.CategoryName = category.GetLocalizedName();
        return View("Index", products);
    }

    [HttpGet]
    public async Task<IActionResult> GetProductsByCategory(int categoryId)
    {
        var products = await _unitOfWork.Product.GetAllAsync(
            filter: p => p.CategoryId == categoryId && p.IsActive && p.StockQuantity > 0,
            includeProperties: "Category"
        );

        return Json(products.Select(p => new
        {
            id = p.Id,
            name = p.GetLocalizedName(),
            price = p.FinalPrice,
            imageUrl = p.ImageUrl,
            categoryName = p.Category.GetLocalizedName()
        }));
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddReview(int productId, int rating, string comment)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            // Check if product exists
            var product = await _unitOfWork.Product.GetByIdAsync(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            // Check if user already reviewed this product
            var existingReview = await _unitOfWork.Review.GetFirstOrDefaultAsync(
                filter: r => r.ProductId == productId && r.UserId == user.Id
            );

            if (existingReview != null)
            {
                return Json(new { success = false, message = "You have already reviewed this product" });
            }

            // Create new review
            var review = new Review
            {
                ProductId = productId,
                UserId = user.Id,
                Rating = rating,
                Comment = comment.Trim(),
                CreatedAt = DateTime.Now,
                IsApproved = false // Reviews need admin approval
            };

            await _unitOfWork.Review.AddAsync(review);
            await _unitOfWork.SaveAsync();

            // Notify admins for new review (service already fans out to admins)
            await _notificationService.CreateNewReviewNotificationAsync(review);

            return Json(new { success = true, message = "Review submitted successfully! It will be visible after approval." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error submitting review. Please try again." });
        }
    }
}
