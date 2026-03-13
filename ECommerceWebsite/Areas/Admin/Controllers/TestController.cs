using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerceWebsite.Repository;

namespace ECommerceWebsite.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class TestController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public TestController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Settings()
    {
        var settings = await _unitOfWork.Setting.GetAllSettingsAsync();
        return Json(settings);
    }
}
