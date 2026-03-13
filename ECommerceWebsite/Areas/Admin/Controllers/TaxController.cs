using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerceWebsite.Models;
using ECommerceWebsite.Repository;
using ECommerceWebsite.ViewModels;

namespace ECommerceWebsite.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class TaxController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public TaxController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        var taxSettings = await _unitOfWork.TaxSetting.GetAllAsync();
        return View(taxSettings);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaxSettingViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var taxSetting = new TaxSetting
                {
                    Country = model.Country,
                    Region = model.Region,
                    City = model.City,
                    TaxRate = model.TaxRate,
                    IsActive = model.IsActive,
                    Notes = model.Notes,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await _unitOfWork.TaxSetting.AddAsync(taxSetting);
                await _unitOfWork.SaveAsync();

                TempData["Success"] = "تم إضافة إعدادات الضريبة بنجاح";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"حدث خطأ أثناء إضافة إعدادات الضريبة: {ex.Message}";
            }
        }

        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var taxSetting = await _unitOfWork.TaxSetting.GetByIdAsync(id);
        if (taxSetting == null)
        {
            TempData["Error"] = "لم يتم العثور على إعدادات الضريبة";
            return RedirectToAction(nameof(Index));
        }

        var model = new TaxSettingViewModel
        {
            Id = taxSetting.Id,
            Country = taxSetting.Country,
            Region = taxSetting.Region,
            City = taxSetting.City,
            TaxRate = taxSetting.TaxRate,
            IsActive = taxSetting.IsActive,
            Notes = taxSetting.Notes
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TaxSettingViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var taxSetting = await _unitOfWork.TaxSetting.GetByIdAsync(model.Id);
                if (taxSetting == null)
                {
                    TempData["Error"] = "لم يتم العثور على إعدادات الضريبة";
                    return RedirectToAction(nameof(Index));
                }

                taxSetting.Country = model.Country;
                taxSetting.Region = model.Region;
                taxSetting.City = model.City;
                taxSetting.TaxRate = model.TaxRate;
                taxSetting.IsActive = model.IsActive;
                taxSetting.Notes = model.Notes;
                taxSetting.UpdatedAt = DateTime.Now;

                _unitOfWork.TaxSetting.Update(taxSetting);
                await _unitOfWork.SaveAsync();

                TempData["Success"] = "تم تحديث إعدادات الضريبة بنجاح";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"حدث خطأ أثناء تحديث إعدادات الضريبة: {ex.Message}";
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
            var taxSetting = await _unitOfWork.TaxSetting.GetByIdAsync(id);
            if (taxSetting == null)
            {
                TempData["Error"] = "لم يتم العثور على إعدادات الضريبة";
                return RedirectToAction(nameof(Index));
            }

            _unitOfWork.TaxSetting.Remove(taxSetting);
            await _unitOfWork.SaveAsync();

            TempData["Success"] = "تم حذف إعدادات الضريبة بنجاح";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"حدث خطأ أثناء حذف إعدادات الضريبة: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        try
        {
            var taxSetting = await _unitOfWork.TaxSetting.GetByIdAsync(id);
            if (taxSetting == null)
            {
                return Json(new { success = false, message = "لم يتم العثور على إعدادات الضريبة" });
            }

            taxSetting.IsActive = !taxSetting.IsActive;
            taxSetting.UpdatedAt = DateTime.Now;

            _unitOfWork.TaxSetting.Update(taxSetting);
            await _unitOfWork.SaveAsync();

            return Json(new
            {
                success = true,
                message = $"تم {(taxSetting.IsActive ? "تفعيل" : "إلغاء تفعيل")} إعدادات الضريبة بنجاح",
                isActive = taxSetting.IsActive
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"حدث خطأ: {ex.Message}" });
        }
    }
}
