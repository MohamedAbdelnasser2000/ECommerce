using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerceWebsite.Models;
using ECommerceWebsite.Repository;
using ECommerceWebsite.Services;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerceWebsite.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;

    public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, INotificationService notificationService, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _webHostEnvironment = webHostEnvironment;
        _notificationService = notificationService;
        _emailService = emailService;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _unitOfWork.Product.GetAllAsync(includeProperties: "Category");
        return View(products);
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _unitOfWork.Product.GetFirstOrDefaultAsync(
            filter: p => p.Id == id,
            includeProperties: "Category,ProductImages,Reviews,Reviews.User"
        );

        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    public async Task<IActionResult> Create()
    {
        try
        {
            var categories = await _unitOfWork.Category.GetAllAsync(filter: c => c.IsActive);

            // Ensure categories is a proper list
            var categoriesList = categories?.ToList() ?? new List<Category>();

            ViewBag.Categories = categoriesList;
            ViewBag.DebugCategoriesCount = categoriesList.Count;
            ViewBag.DebugCategoriesList = categoriesList.Select(c => $"{c.Id}:{c.Name}").ToList();

            // Debug: Log to console
            Console.WriteLine($"Categories loaded: {categoriesList.Count}");
            foreach (var cat in categoriesList)
            {
                Console.WriteLine($"Category: {cat.Id} - {cat.Name} - Active: {cat.IsActive}");
            }

            return View();
        }
        catch (Exception ex)
        {
            // Log error and provide fallback
            Console.WriteLine($"Error loading categories: {ex.Message}");
            ViewBag.Categories = new List<Category>();
            ViewBag.DebugCategoriesCount = 0;
            ViewBag.DebugError = $"Error loading categories: {ex.Message}";
            return View();
        }
    }

[HttpPost]
[RequestSizeLimit(15_000_000)] // ~15 MB
[EnableRateLimiting("upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product, IFormFile? imageFile, List<IFormFile>? additionalImages)
    {
        // Remove validation for fields that will be set automatically
        ModelState.Remove("CreatedAt");
        ModelState.Remove("UpdatedAt");
        ModelState.Remove("Category");

        // Debug: Log the CategoryId value
        var categoryId = product?.CategoryId ?? 0;

        if (ModelState.IsValid)
        {
            // Set timestamps
            product.CreatedAt = DateTime.Now;
            product.UpdatedAt = DateTime.Now;

            // Handle main image upload with validation
            var allowedExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
            if (imageFile != null && imageFile.Length > 0)
            {
                if (imageFile.Length > 10_000_000) { ModelState.AddModelError("imageFile", "File too large (max 10MB)"); return View(product); }
                var ext = Path.GetExtension(imageFile.FileName);
                if (!allowedExt.Contains(ext)) { ModelState.AddModelError("imageFile", "Only JPG, PNG, WEBP allowed"); return View(product); }

                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.WriteThrough))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                product.ImageUrl = "/images/products/" + uniqueFileName;
            }

            try
            {
                // Create product first to get Id
                await _unitOfWork.Product.AddAsync(product);
                await _unitOfWork.SaveAsync();

                // Handle up to 6 additional images
                if (additionalImages != null && additionalImages.Any())
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                    Directory.CreateDirectory(uploadsFolder);

                    int maxAdditional = 6;
                    foreach (var img in additionalImages.Where(f => f != null && f.Length > 0).Take(maxAdditional))
                    {
                        if (img.Length > 10_000_000) { continue; }
                        var e = Path.GetExtension(img.FileName);
                        if (!allowedExt.Contains(e)) { continue; }
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + img.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.WriteThrough))
                        {
                            await img.CopyToAsync(stream);
                        }

                        var productImage = new ProductImage
                        {
                            ProductId = product.Id,
                            ImageUrl = "/images/products/" + uniqueFileName,
                            IsMain = false
                        };
                        await _unitOfWork.ProductImage.AddAsync(productImage);
                    }
                    await _unitOfWork.SaveAsync();
                }

                TempData["Success"] = "Product created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating product: {ex.Message}";
            }
        }
        else
        {
            // Log validation errors for debugging
            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .Select(x => new { Field = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) });

            TempData["Error"] = "Please fix the validation errors and try again.";

            // Debug: Add detailed error information
            var errorDetails = string.Join(", ", errors.Select(e => $"{e.Field}: {string.Join(", ", e.Errors)}"));
            TempData["DebugErrors"] = $"Validation errors: {errorDetails}";
        }

        var categoriesForView = await _unitOfWork.Category.GetAllAsync(filter: c => c.IsActive);
        ViewBag.Categories = categoriesForView;
        ViewBag.DebugCategoriesCount = categoriesForView?.Count() ?? 0;
        ViewBag.DebugCategoryId = categoryId;
        return View(product);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var product = await _unitOfWork.Product.GetFirstOrDefaultAsync(
            filter: p => p.Id == id,
            includeProperties: "Category,ProductImages"
        );
        if (product == null)
        {
            return NotFound();
        }

        ViewBag.Categories = await _unitOfWork.Category.GetAllAsync(filter: c => c.IsActive);
        return View(product);
    }

[HttpPost]
[RequestSizeLimit(20_000_000)]
[EnableRateLimiting("upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Product product, IFormFile? imageFile, List<IFormFile>? additionalImages, List<int>? deleteImageIds)
    {
        if (id != product.Id)
        {
            return NotFound();
        }

        // Remove validation for fields that will be set automatically
        ModelState.Remove("CreatedAt");
        ModelState.Remove("UpdatedAt");
        ModelState.Remove("Category");

        if (ModelState.IsValid)
        {
            try
            {
                var existingProduct = await _unitOfWork.Product.GetFirstOrDefaultAsync(
                    filter: p => p.Id == id,
                    includeProperties: "ProductImages"
                );
                if (existingProduct == null)
                {
                    return NotFound();
                }

                // Handle main image upload
                string newImageUrl = existingProduct.ImageUrl;
                var allowedExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
                if (imageFile != null && imageFile.Length > 0)
                {
                    if (imageFile.Length > 10_000_000) { ModelState.AddModelError("imageFile", "File too large (max 10MB)"); return View(product); }
                    var ext = Path.GetExtension(imageFile.FileName);
                    if (!allowedExt.Contains(ext)) { ModelState.AddModelError("imageFile", "Only JPG, PNG, WEBP allowed"); return View(product); }
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(existingProduct.ImageUrl))
                    {
                        string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, existingProduct.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                    Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.WriteThrough))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    newImageUrl = "/images/products/" + uniqueFileName;
                }

                // Delete selected additional images
                if (deleteImageIds != null && deleteImageIds.Any())
                {
                    foreach (var imgId in deleteImageIds)
                    {
                        var img = existingProduct.ProductImages.FirstOrDefault(pi => pi.Id == imgId);
                        if (img != null)
                        {
                            string imgPath = Path.Combine(_webHostEnvironment.WebRootPath, img.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(imgPath))
                            {
                                System.IO.File.Delete(imgPath);
                            }
                            _unitOfWork.ProductImage.Remove(img);
                        }
                    }
                }

                // Add up to 6 additional images (respect current count)
                if (additionalImages != null && additionalImages.Any())
                {
                    var currentCount = existingProduct.ProductImages?.Count ?? 0;
                    int allowedToAdd = Math.Max(0, 6 - currentCount);
                    if (allowedToAdd > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                        Directory.CreateDirectory(uploadsFolder);

                        var allowedExt2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
                        foreach (var img in additionalImages.Where(f => f != null && f.Length > 0).Take(allowedToAdd))
                        {
                            if (img.Length > 10_000_000) { continue; }
                            var e = Path.GetExtension(img.FileName);
                            if (!allowedExt2.Contains(e)) { continue; }
                            string uniqueFileName = Guid.NewGuid().ToString() + "_" + img.FileName;
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.WriteThrough))
                            {
                                await img.CopyToAsync(stream);
                            }

                            var productImage = new ProductImage
                            {
                                ProductId = existingProduct.Id,
                                ImageUrl = "/images/products/" + uniqueFileName,
                                IsMain = false
                            };
                            await _unitOfWork.ProductImage.AddAsync(productImage);
                        }
                    }
                }

                // Update the existing tracked entity instead of creating a new one
                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.DiscountPrice = product.DiscountPrice;
                existingProduct.StockQuantity = product.StockQuantity;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.IsActive = product.IsActive;
                existingProduct.IsFeatured = product.IsFeatured;
                existingProduct.IsFeaturedInNewCarousel = product.IsFeaturedInNewCarousel;
                existingProduct.ImageUrl = newImageUrl;
                existingProduct.UpdatedAt = DateTime.Now;

                await _unitOfWork.SaveAsync();

                // Check for low stock and send notifications
                await CheckLowStockAndNotify(existingProduct);

                TempData["Success"] = "Product updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating product: {ex.Message}";
            }
        }
        else
        {
            // Log validation errors for debugging
            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .Select(x => new { Field = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) });

            TempData["Error"] = "Please fix the validation errors and try again.";
        }

        ViewBag.Categories = await _unitOfWork.Category.GetAllAsync(filter: c => c.IsActive);
        return View(product);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var product = await _unitOfWork.Product.GetFirstOrDefaultAsync(
            filter: p => p.Id == id,
            includeProperties: "Category"
        );

        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await _unitOfWork.Product.GetByIdAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        // Prevent deleting products that are referenced in orders
        if (await _unitOfWork.OrderItem.AnyAsync(oi => oi.ProductId == id))
        {
            TempData["Error"] = "Cannot delete this product because it has order history. Consider deactivating it instead.";
            return RedirectToAction(nameof(Index));
        }

        // Delete image file if exists
        if (!string.IsNullOrEmpty(product.ImageUrl))
        {
            string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, product.ImageUrl.TrimStart('/'));
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
        }

        _unitOfWork.Product.Remove(product);
        await _unitOfWork.SaveAsync();

        TempData["Success"] = "Product deleted successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var product = await _unitOfWork.Product.GetByIdAsync(id);
        if (product == null)
        {
            return Json(new { success = false, message = "Product not found" });
        }

        product.IsActive = !product.IsActive;
        product.UpdatedAt = DateTime.Now;

        _unitOfWork.Product.Update(product);
        await _unitOfWork.SaveAsync();

        return Json(new { success = true, isActive = product.IsActive });
    }

    private async Task CheckLowStockAndNotify(Product product)
    {
        const int lowStockThreshold = 10; // You can make this configurable

        if (product.StockQuantity <= lowStockThreshold && product.IsActive)
        {
            try
            {
                // Send notification to admins
                await _notificationService.CreateLowStockNotificationAsync(product);

                // Send email to admin (you can get admin emails from configuration or database)
                var adminEmails = new[] { "admin@example.com" }; // Make this configurable
                foreach (var adminEmail in adminEmails)
                {
                    await _emailService.SendLowStockAlertAsync(adminEmail, product);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the product update
                // _logger.LogError(ex, "Failed to send low stock notification for product {ProductId}", product.Id);
            }
        }
    }
}
