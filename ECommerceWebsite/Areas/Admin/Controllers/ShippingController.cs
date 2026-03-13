using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerceWebsite.Models;
using ECommerceWebsite.Repository;
using ECommerceWebsite.ViewModels;

namespace ECommerceWebsite.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ShippingController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public ShippingController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        var shippingMethods = await _unitOfWork.ShippingMethod.GetAllAsync();
        return View(shippingMethods);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ShippingMethodViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var shippingMethod = new ShippingMethod
                {
                    Name = model.Name,
                    Description = model.Description,
                    Cost = model.Cost,
                    EstimatedDays = model.EstimatedDays,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await _unitOfWork.ShippingMethod.AddAsync(shippingMethod);
                await _unitOfWork.SaveAsync();

                TempData["Success"] = "تم إضافة طريقة الشحن بنجاح";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"حدث خطأ أثناء إضافة طريقة الشحن: {ex.Message}";
            }
        }

        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var shippingMethod = await _unitOfWork.ShippingMethod.GetByIdAsync(id);
        if (shippingMethod == null)
        {
            TempData["Error"] = "لم يتم العثور على طريقة الشحن";
            return RedirectToAction(nameof(Index));
        }

        var model = new ShippingMethodViewModel
        {
            Id = shippingMethod.Id,
            Name = shippingMethod.Name,
            Description = shippingMethod.Description,
            Cost = shippingMethod.Cost,
            EstimatedDays = shippingMethod.EstimatedDays,
            IsActive = shippingMethod.IsActive
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ShippingMethodViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var shippingMethod = await _unitOfWork.ShippingMethod.GetByIdAsync(model.Id);
                if (shippingMethod == null)
                {
                    TempData["Error"] = "لم يتم العثور على طريقة الشحن";
                    return RedirectToAction(nameof(Index));
                }

                shippingMethod.Name = model.Name;
                shippingMethod.Description = model.Description;
                shippingMethod.Cost = model.Cost;
                shippingMethod.EstimatedDays = model.EstimatedDays;
                shippingMethod.IsActive = model.IsActive;
                shippingMethod.UpdatedAt = DateTime.Now;

                _unitOfWork.ShippingMethod.Update(shippingMethod);
                await _unitOfWork.SaveAsync();

                TempData["Success"] = "تم تحديث طريقة الشحن بنجاح";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"حدث خطأ أثناء تحديث طريقة الشحن: {ex.Message}";
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var shippingMethod = await _unitOfWork.ShippingMethod.GetByIdAsync(id);
            if (shippingMethod == null)
            {
                TempData["Error"] = "لم يتم العثور على طريقة الشحن";
                return RedirectToAction(nameof(Index));
            }

            _unitOfWork.ShippingMethod.Remove(shippingMethod);
            await _unitOfWork.SaveAsync();

            TempData["Success"] = "تم حذف طريقة الشحن بنجاح";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"حدث خطأ أثناء حذف طريقة الشحن: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        try
        {
            var shippingMethod = await _unitOfWork.ShippingMethod.GetByIdAsync(id);
            if (shippingMethod == null)
            {
                return Json(new { success = false, message = "لم يتم العثور على طريقة الشحن" });
            }

            shippingMethod.IsActive = !shippingMethod.IsActive;
            shippingMethod.UpdatedAt = DateTime.Now;

            _unitOfWork.ShippingMethod.Update(shippingMethod);
            await _unitOfWork.SaveAsync();

            return Json(new
            {
                success = true,
                message = $"تم {(shippingMethod.IsActive ? "تفعيل" : "إلغاء تفعيل")} طريقة الشحن بنجاح",
                isActive = shippingMethod.IsActive
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"حدث خطأ: {ex.Message}" });
        }
    }
}
