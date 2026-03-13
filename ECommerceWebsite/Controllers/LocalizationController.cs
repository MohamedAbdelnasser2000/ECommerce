using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceWebsite.Controllers
{
    // Handles language switching by writing the culture cookie
    public class LocalizationController : Controller
    {
        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(culture)) culture = "en-US";

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true, // allow writing even if consent not given yet
                    SameSite = SameSiteMode.Lax,
                    HttpOnly = false,
                    Secure = Request.IsHttps,
                    Path = "/"
                }
            );

            if (string.IsNullOrWhiteSpace(returnUrl)) returnUrl = Url.Content("~/");
            return LocalRedirect(returnUrl);
        }
    }
}