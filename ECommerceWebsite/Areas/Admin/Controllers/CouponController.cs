using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using ECommerceWebsite.Repository;
using ECommerceWebsite.Models;

namespace ECommerceWebsite.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CouponController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public CouponController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        var coupons = await _unitOfWork.Coupon.GetAllAsync(
            includeProperties: "Category,Product"
        );

        var couponViewModels = coupons.Select(c => new CouponViewModel
        {
            Id = c.Id,
            Code = c.Code,
            Name = c.Name,
            Description = c.Description,
            Type = c.Type,
            Value = c.Value,
            MinimumOrderAmount = c.MinimumOrderAmount,
            MaximumDiscountAmount = c.MaximumDiscountAmount,
            UsageLimit = c.UsageLimit,
            UsedCount = c.UsedCount,
            IsActive = c.IsActive,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            CategoryId = c.CategoryId,
            ProductId = c.ProductId,
            CategoryName = c.Category?.Name,
            ProductName = c.Product?.Name
        }).ToList();

        return View(couponViewModels);
    }

    public async Task<IActionResult> Details(int id)
    {
        var coupon = await _unitOfWork.Coupon.GetFirstOrDefaultAsync(
            filter: c => c.Id == id,
            includeProperties: "Category,Product,CouponUsages.User,CouponUsages.Order"
        );

        if (coupon == null)
        {
            return NotFound();
        }

        return View(coupon);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateDropdowns();
        return View(new CouponViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CouponViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Check if code is unique
            var isUnique = await _unitOfWork.Coupon.IsCodeUniqueAsync(model.Code);
            if (!isUnique)
            {
                ModelState.AddModelError("Code", "This coupon code already exists");
                await PopulateDropdowns();
                return View(model);
            }

            var coupon = new Coupon
            {
                Code = model.Code.ToUpper(),
                Name = model.Name,
                Description = model.Description,
                Type = model.Type,
                Value = model.Value,
                MinimumOrderAmount = model.MinimumOrderAmount,
                MaximumDiscountAmount = model.MaximumDiscountAmount,
                UsageLimit = model.UsageLimit,
                IsActive = model.IsActive,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                CategoryId = model.CategoryId,
                ProductId = model.ProductId,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.Coupon.AddAsync(coupon);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Coupon created successfully";
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropdowns();
        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var coupon = await _unitOfWork.Coupon.GetByIdAsync(id);
        if (coupon == null)
        {
            return NotFound();
        }

        var model = new CouponViewModel
        {
            Id = coupon.Id,
            Code = coupon.Code,
            Name = coupon.Name,
            Description = coupon.Description,
            Type = coupon.Type,
            Value = coupon.Value,
            MinimumOrderAmount = coupon.MinimumOrderAmount,
            MaximumDiscountAmount = coupon.MaximumDiscountAmount,
            UsageLimit = coupon.UsageLimit,
            IsActive = coupon.IsActive,
            StartDate = coupon.StartDate,
            EndDate = coupon.EndDate,
            CategoryId = coupon.CategoryId,
            ProductId = coupon.ProductId
        };

        await PopulateDropdowns();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CouponViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Check if code is unique (excluding current coupon)
            var isUnique = await _unitOfWork.Coupon.IsCodeUniqueAsync(model.Code, model.Id);
            if (!isUnique)
            {
                ModelState.AddModelError("Code", "This coupon code already exists");
                await PopulateDropdowns();
                return View(model);
            }

            var coupon = await _unitOfWork.Coupon.GetByIdAsync(model.Id);
            if (coupon == null)
            {
                return NotFound();
            }

            coupon.Code = model.Code.ToUpper();
            coupon.Name = model.Name;
            coupon.Description = model.Description;
            coupon.Type = model.Type;
            coupon.Value = model.Value;
            coupon.MinimumOrderAmount = model.MinimumOrderAmount;
            coupon.MaximumDiscountAmount = model.MaximumDiscountAmount;
            coupon.UsageLimit = model.UsageLimit;
            coupon.IsActive = model.IsActive;
            coupon.StartDate = model.StartDate;
            coupon.EndDate = model.EndDate;
            coupon.CategoryId = model.CategoryId;
            coupon.ProductId = model.ProductId;
            coupon.UpdatedAt = DateTime.Now;

            _unitOfWork.Coupon.Update(coupon);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Coupon updated successfully";
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropdowns();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var coupon = await _unitOfWork.Coupon.GetByIdAsync(id);
        if (coupon == null)
        {
            return Json(new { success = false, message = "Coupon not found" });
        }

        // Check if coupon has been used
        var hasUsages = await _unitOfWork.CouponUsage.GetFirstOrDefaultAsync(
            filter: cu => cu.CouponId == id
        );

        if (hasUsages != null)
        {
            return Json(new { success = false, message = "Cannot delete coupon that has been used" });
        }

        _unitOfWork.Coupon.Remove(coupon);
        await _unitOfWork.SaveAsync();

        return Json(new { success = true, message = "Coupon deleted successfully" });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var coupon = await _unitOfWork.Coupon.GetByIdAsync(id);
        if (coupon == null)
        {
            return Json(new { success = false, message = "Coupon not found" });
        }

        coupon.IsActive = !coupon.IsActive;
        coupon.UpdatedAt = DateTime.Now;

        _unitOfWork.Coupon.Update(coupon);
        await _unitOfWork.SaveAsync();

        return Json(new { 
            success = true, 
            message = $"Coupon {(coupon.IsActive ? "activated" : "deactivated")} successfully",
            isActive = coupon.IsActive
        });
    }

    private async Task PopulateDropdowns()
    {
        var categories = await _unitOfWork.Category.GetAllAsync(filter: c => c.IsActive);
        var products = await _unitOfWork.Product.GetAllAsync(filter: p => p.IsActive);

        ViewBag.Categories = new SelectList(categories, "Id", "Name");
        ViewBag.Products = new SelectList(products, "Id", "Name");
        ViewBag.CouponTypes = new SelectList(Enum.GetValues(typeof(CouponType)).Cast<CouponType>()
            .Select(e => new { Value = (int)e, Text = e.ToString() }), "Value", "Text");
    }
}
