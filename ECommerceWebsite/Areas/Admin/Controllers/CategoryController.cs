using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerceWebsite.Models;
using ECommerceWebsite.Repository;

namespace ECommerceWebsite.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoryController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public CategoryController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
    {
        _unitOfWork = unitOfWork;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _unitOfWork.Category.GetAllAsync(includeProperties: "Products");
        return View(categories);
    }

    public async Task<IActionResult> Details(int id)
    {
        var category = await _unitOfWork.Category.GetFirstOrDefaultAsync(
            filter: c => c.Id == id,
            includeProperties: "Products"
        );

        if (category == null)
        {
            return NotFound();
        }

        return View(category);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category category, IFormFile? imageFile)
    {
        if (ModelState.IsValid)
        {
            // Handle image upload
            if (imageFile != null && imageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "categories");
                Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                category.ImageUrl = "/images/categories/" + uniqueFileName;
            }

            await _unitOfWork.Category.AddAsync(category);
            await _unitOfWork.SaveAsync();

            TempData["Success"] = "Category created successfully!";
            return RedirectToAction(nameof(Index));
        }

        return View(category);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var category = await _unitOfWork.Category.GetByIdAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Category category, IFormFile? imageFile)
    {
        if (id != category.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existingCategory = await _unitOfWork.Category.GetByIdAsync(id);
                if (existingCategory == null)
                {
                    return NotFound();
                }

                // Handle image upload
                string newImageUrl = existingCategory.ImageUrl;
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(existingCategory.ImageUrl))
                    {
                        string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, existingCategory.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "categories");
                    Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    newImageUrl = "/images/categories/" + uniqueFileName;
                }

                // Update the existing tracked entity instead of creating a new one
                existingCategory.Name = category.Name;
                existingCategory.Description = category.Description;
                existingCategory.NameAr = category.NameAr;
                existingCategory.DescriptionAr = category.DescriptionAr;
                existingCategory.IsActive = category.IsActive;
                existingCategory.ImageUrl = newImageUrl;
                existingCategory.ShowInHeader = category.ShowInHeader;

                await _unitOfWork.SaveAsync();

                TempData["Success"] = "Category updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating category: {ex.Message}";
            }
        }

        return View(category);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var category = await _unitOfWork.Category.GetFirstOrDefaultAsync(
            filter: c => c.Id == id,
            includeProperties: "Products"
        );

        if (category == null)
        {
            return NotFound();
        }

        return View(category);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var category = await _unitOfWork.Category.GetByIdAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        // Check if category has products
        var hasProducts = await _unitOfWork.Product.AnyAsync(p => p.CategoryId == id);
        if (hasProducts)
        {
            TempData["Error"] = "Cannot delete category that contains products!";
            return RedirectToAction(nameof(Index));
        }

        // Delete image file if exists
        if (!string.IsNullOrEmpty(category.ImageUrl))
        {
            string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, category.ImageUrl.TrimStart('/'));
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
        }

        _unitOfWork.Category.Remove(category);
        await _unitOfWork.SaveAsync();

        TempData["Success"] = "Category deleted successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var category = await _unitOfWork.Category.GetByIdAsync(id);
        if (category == null)
        {
            return Json(new { success = false, message = "Category not found" });
        }

        category.IsActive = !category.IsActive;

        _unitOfWork.Category.Update(category);
        await _unitOfWork.SaveAsync();

        return Json(new { success = true, isActive = category.IsActive });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleShowOnHome(int id)
    {
        var category = await _unitOfWork.Category.GetByIdAsync(id);
        if (category == null)
        {
            return Json(new { success = false, message = "Category not found" });
        }

        category.ShowOnHome = !category.ShowOnHome;
        _unitOfWork.Category.Update(category);
        await _unitOfWork.SaveAsync();

        return Json(new { success = true, showOnHome = category.ShowOnHome });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleShowInHeader(int id)
    {
        var category = await _unitOfWork.Category.GetByIdAsync(id);
        if (category == null)
        {
            return Json(new { success = false, message = "Category not found" });
        }

        category.ShowInHeader = !category.ShowInHeader;
        _unitOfWork.Category.Update(category);
        await _unitOfWork.SaveAsync();

        return Json(new { success = true, showInHeader = category.ShowInHeader });
    }
}
