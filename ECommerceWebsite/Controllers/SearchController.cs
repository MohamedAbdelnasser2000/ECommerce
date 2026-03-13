using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ECommerceWebsite.Repository;
using ECommerceWebsite.Models;
using ECommerceWebsite.Extensions;
using System.Text.Json;

namespace ECommerceWebsite.Controllers;

public class SearchController : BaseController
{
    private readonly UserManager<ApplicationUser> _userManager;

    public SearchController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager) : base(unitOfWork)
    {
        _userManager = userManager;
    }

    // Main search page with advanced filters
    public async Task<IActionResult> Index(
        string? query = null,
        int? categoryId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        int? minRating = null,
        bool? inStock = null,
        bool? onSale = null,
        string? sort = "relevance",
        string? view = "grid",
        int page = 1,
        int pageSize = 12)
    {
        var products = await GetFilteredProducts(query, categoryId, minPrice, maxPrice, minRating, inStock, onSale);

        // Apply sorting
        products = ApplySorting(products, sort, query);

        var total = products.Count();
        var pagedProducts = products.Skip((page - 1) * pageSize).Take(pageSize);

        // Get available categories for filter dropdown
        var categories = await _unitOfWork.Category.GetAllAsync(filter: c => c.IsActive);

        // Calculate price range for filters
        var allProducts = await _unitOfWork.Product.GetAllAsync(filter: p => p.IsActive && p.StockQuantity > 0);
        var minProductPrice = allProducts.Min(p => p.FinalPrice);
        var maxProductPrice = allProducts.Max(p => p.FinalPrice);

        // ViewBag data for filters
        ViewBag.Query = query;
        ViewBag.Categories = categories;
        ViewBag.MinPriceRange = minProductPrice;
        ViewBag.MaxPriceRange = maxProductPrice;
        ViewBag.CurrentCategory = categoryId;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.MinRating = minRating;
        ViewBag.InStock = inStock;
        ViewBag.OnSale = onSale;
        ViewBag.SortBy = sort;
        ViewBag.ViewMode = string.IsNullOrWhiteSpace(view) ? "grid" : view;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
        ViewBag.TotalResults = total;

        return View(pagedProducts);
    }

    // AJAX search suggestions
    [HttpGet]
    public async Task<IActionResult> Suggestions(string query)
    {
        if (string.IsNullOrEmpty(query) || query.Length < 2)
        {
            return Json(new List<string>());
        }

        var products = await _unitOfWork.Product.GetAllAsync(
            filter: p => p.IsActive && p.StockQuantity > 0 && (
                p.Name.Contains(query) ||
                p.Description.Contains(query) ||
                (p.NameAr != null && p.NameAr.Contains(query)) ||
                (p.DescriptionAr != null && p.DescriptionAr.Contains(query))
            ),
            includeProperties: "Category"
        );

        var suggestions = new List<string>();

        // Add product names
        suggestions.AddRange(products.Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                                   .Select(p => p.Name)
                                   .Distinct()
                                   .Take(8));

        // Add Arabic names if they match
        suggestions.AddRange(products.Where(p => p.NameAr != null && p.NameAr.Contains(query))
                                   .Select(p => p.NameAr!)
                                   .Distinct()
                                   .Take(8));

        return Json(suggestions.Distinct().Take(8));
    }

    // Quick search results (for search dropdown)
    [HttpGet]
    public async Task<IActionResult> QuickSearch(string query, int limit = 5)
    {
        if (string.IsNullOrEmpty(query) || query.Length < 2)
        {
            return Json(new List<object>());
        }

        var products = await _unitOfWork.Product.GetAllAsync(
            filter: p => p.IsActive && p.StockQuantity > 0 && (
                p.Name.Contains(query) ||
                p.Description.Contains(query) ||
                (p.NameAr != null && p.NameAr.Contains(query)) ||
                (p.DescriptionAr != null && p.DescriptionAr.Contains(query))
            ),
            includeProperties: "Category"
        );

        var results = products.Take(limit).Select(p => new
        {
            id = p.Id,
            name = p.GetLocalizedName(),
            price = p.FinalPrice,
            imageUrl = p.ImageUrl ?? "/images/no-image.png",
            categoryName = p.Category.GetLocalizedName(),
            rating = p.AverageRating
        });

        return Json(results);
    }

    // Advanced search with detailed filters
    [HttpPost]
    public async Task<IActionResult> AdvancedSearch(SearchFilterModel filters)
    {
        var products = await GetFilteredProducts(
            filters.Query,
            filters.CategoryId,
            filters.MinPrice,
            filters.MaxPrice,
            filters.MinRating,
            filters.InStock,
            filters.OnSale
        );

        // Apply sorting
        products = ApplySorting(products, filters.SortBy ?? "relevance", filters.Query);

        var total = products.Count();
        var pagedProducts = products.Skip((filters.Page - 1) * filters.PageSize).Take(filters.PageSize);

        return Json(new
        {
            products = pagedProducts.Select(p => new
            {
                id = p.Id,
                name = p.GetLocalizedName(),
                description = p.GetLocalizedDescription(),
                price = p.FinalPrice,
                originalPrice = p.Price,
                discountPrice = p.DiscountPrice,
                imageUrl = p.ImageUrl ?? "/images/no-image.png",
                categoryName = p.Category.GetLocalizedName(),
                rating = p.AverageRating,
                reviewCount = p.Reviews.Count,
                isInStock = p.StockQuantity > 0,
                isOnSale = p.DiscountPrice.HasValue
            }),
            totalCount = total,
            currentPage = filters.Page,
            totalPages = (int)Math.Ceiling((double)total / filters.PageSize)
        });
    }

    // Helper method to get filtered products
    private async Task<IQueryable<Product>> GetFilteredProducts(
        string? query,
        int? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        int? minRating,
        bool? inStock,
        bool? onSale)
    {
        var products = await _unitOfWork.Product.GetAllAsync(
            filter: p => p.IsActive,
            includeProperties: "Category,Reviews"
        );

        // Apply search query filter
        if (!string.IsNullOrEmpty(query))
        {
            products = products.Where(p =>
                p.Name.Contains(query) ||
                p.Description.Contains(query) ||
                (p.NameAr != null && p.NameAr.Contains(query)) ||
                (p.DescriptionAr != null && p.DescriptionAr.Contains(query))
            );
        }

        // Apply category filter
        if (categoryId.HasValue)
        {
            products = products.Where(p => p.CategoryId == categoryId.Value);
        }

        // Apply price range filter
        if (minPrice.HasValue)
        {
            products = products.Where(p => p.FinalPrice >= minPrice.Value);
        }
        if (maxPrice.HasValue)
        {
            products = products.Where(p => p.FinalPrice <= maxPrice.Value);
        }

        // Apply rating filter
        if (minRating.HasValue && minRating.Value > 0)
        {
            products = products.Where(p => p.AverageRating >= minRating.Value);
        }

        // Apply stock filter
        if (inStock.HasValue && inStock.Value)
        {
            products = products.Where(p => p.StockQuantity > 0);
        }

        // Apply sale filter
        if (onSale.HasValue && onSale.Value)
        {
            products = products.Where(p => p.DiscountPrice.HasValue);
        }

        return products.AsQueryable();
    }

    // Helper method to apply sorting
    private IQueryable<Product> ApplySorting(IQueryable<Product> products, string sort, string? query)
    {
        return sort switch
        {
            "price-low" => products.OrderBy(p => p.FinalPrice),
            "price-high" => products.OrderByDescending(p => p.FinalPrice),
            "popular" => products.OrderByDescending(p => p.OrderItems.Count),
            "top-rated" => products.OrderByDescending(p => p.AverageRating),
            "newest" => products.OrderByDescending(p => p.CreatedAt),
            "oldest" => products.OrderBy(p => p.CreatedAt),
            "name-asc" => products.OrderBy(p => p.Name),
            "name-desc" => products.OrderByDescending(p => p.Name),
            "relevance" when !string.IsNullOrEmpty(query) =>
                // For relevance, prioritize exact name matches, then partial matches
                products.OrderByDescending(p =>
                    (p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ? 3 :
                     p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ? 2 :
                     p.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                ).ThenByDescending(p => p.CreatedAt),
            _ => products.OrderByDescending(p => p.CreatedAt) // Default: newest first
        };
    }
}

// Search Filter Model for advanced search
public class SearchFilterModel
{
    public string? Query { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MinRating { get; set; }
    public bool? InStock { get; set; }
    public bool? OnSale { get; set; }
    public string? SortBy { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}
