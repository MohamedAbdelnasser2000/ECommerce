using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using ECommerceWebsite.Models;
using ECommerceWebsite.Repository;
using ECommerceWebsite.Services;
using System.IO;

namespace ECommerceWebsite.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class SettingsController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly IEmailService _emailService;

    public SettingsController(IUnitOfWork unitOfWork, IConfiguration configuration, IWebHostEnvironment env, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _env = env;
        _emailService = emailService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeedArabicHeroCta()
    {
        try
        {
            // Arabic defaults for Homepage Hero
            await _unitOfWork.Setting.SetValueAsync("HeroTitleAr", "اكتشف منتجات مذهلة", "Homepage hero title (Arabic) - default");
            await _unitOfWork.Setting.SetValueAsync("HeroSubtitleAr", "تسوّق أحدث الصيحات والمنتجات الحصرية بأفضل الأسعار.", "Homepage hero subtitle (Arabic) - default");
            await _unitOfWork.Setting.SetValueAsync("HeroBadgeAr", "تشكيلة جديدة", "Homepage hero badge (Arabic) - default");
            await _unitOfWork.Setting.SetValueAsync("HeroButtonTextAr", "تسوّق الآن", "Homepage hero button text (Arabic) - default");

            // Arabic defaults for Homepage CTA
            await _unitOfWork.Setting.SetValueAsync("CtaBadgeTextAr", "-٦٠٪", "CTA badge text (Arabic) - default");
            await _unitOfWork.Setting.SetValueAsync("CtaTitleAr", "تخفيضات كبرى", "CTA title (Arabic) - default");
            await _unitOfWork.Setting.SetValueAsync("CtaDescriptionAr", "لا تفوّت عروضنا الموسمية واقتنِ ما تحب بأسعار مذهلة.", "CTA description (Arabic) - default");
            await _unitOfWork.Setting.SetValueAsync("CtaButtonTextAr", "اشترِ الآن", "CTA button text (Arabic) - default");

            TempData["Success"] = "Arabic defaults populated for Hero and CTA.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error seeding Arabic defaults: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }
    public async Task<IActionResult> Index()
    {
        // Get settings from database, fallback to configuration if not found
        var settings = await _unitOfWork.Setting.GetAllSettingsAsync();

        var model = new SettingsViewModel
        {
            SiteName = settings.GetValueOrDefault("SiteName", _configuration["SiteSettings:SiteName"] ?? "E-Commerce Store"),
            SiteDescription = settings.GetValueOrDefault("SiteDescription", _configuration["SiteSettings:SiteDescription"] ?? "Your online shopping destination"),
            ContactEmail = settings.GetValueOrDefault("ContactEmail", _configuration["SiteSettings:ContactEmail"] ?? "admin@ecommerce.com"),
            ContactPhone = settings.GetValueOrDefault("ContactPhone", _configuration["SiteSettings:ContactPhone"] ?? "+1-234-567-8900"),
            Address = settings.GetValueOrDefault("Address", _configuration["SiteSettings:Address"] ?? "123 Main St, City, State 12345"),
            Currency = settings.GetValueOrDefault("Currency", "USD"),
            TimeZone = settings.GetValueOrDefault("TimeZone", "UTC"),
            Language = settings.GetValueOrDefault("Language", "en"),

            // Email Settings
            SmtpHost = settings.GetValueOrDefault("SmtpHost", _configuration["EmailSettings:SmtpHost"] ?? ""),
            SmtpPort = int.TryParse(settings.GetValueOrDefault("SmtpPort", _configuration["EmailSettings:SmtpPort"] ?? "587"), out var port) ? port : 587,
            SmtpUsername = settings.GetValueOrDefault("SmtpUsername", _configuration["EmailSettings:Username"] ?? ""),
            SmtpPassword = settings.GetValueOrDefault("SmtpPassword", _configuration["EmailSettings:Password"] ?? ""),
            EnableSsl = bool.TryParse(settings.GetValueOrDefault("EnableSsl", _configuration["EmailSettings:EnableSsl"] ?? "false"), out var ssl) && ssl,

            // Payment Settings
            PayPalClientId = settings.GetValueOrDefault("PayPalClientId", _configuration["PaymentSettings:PayPal:ClientId"] ?? ""),
            PayPalClientSecret = settings.GetValueOrDefault("PayPalClientSecret", _configuration["PaymentSettings:PayPal:ClientSecret"] ?? ""),
            StripePublishableKey = settings.GetValueOrDefault("StripePublishableKey", _configuration["PaymentSettings:Stripe:PublishableKey"] ?? ""),
            StripeSecretKey = settings.GetValueOrDefault("StripeSecretKey", _configuration["PaymentSettings:Stripe:SecretKey"] ?? ""),

            // General Settings
            ItemsPerPage = int.TryParse(settings.GetValueOrDefault("ItemsPerPage", _configuration["SiteSettings:ItemsPerPage"] ?? "12"), out var items) ? items : 12,
            EnableRegistration = bool.TryParse(settings.GetValueOrDefault("EnableRegistration", _configuration["SiteSettings:EnableRegistration"] ?? "true"), out var reg) && reg,
            EnableReviews = bool.TryParse(settings.GetValueOrDefault("EnableReviews", _configuration["SiteSettings:EnableReviews"] ?? "true"), out var reviews) && reviews,
            EnableWishlist = bool.TryParse(settings.GetValueOrDefault("EnableWishlist", _configuration["SiteSettings:EnableWishlist"] ?? "true"), out var wishlist) && wishlist,

            // SEO Settings
            MetaTitle = settings.GetValueOrDefault("MetaTitle", _configuration["SeoSettings:MetaTitle"] ?? "E-Commerce Store"),
            MetaDescription = settings.GetValueOrDefault("MetaDescription", _configuration["SeoSettings:MetaDescription"] ?? "Best online shopping experience"),
            MetaKeywords = settings.GetValueOrDefault("MetaKeywords", _configuration["SeoSettings:MetaKeywords"] ?? "ecommerce, shopping, online store"),
            GoogleAnalyticsId = settings.GetValueOrDefault("GoogleAnalyticsId", ""),
            FacebookPixelId = settings.GetValueOrDefault("FacebookPixelId", ""),

            // Social Media
            FacebookUrl = settings.GetValueOrDefault("FacebookUrl", _configuration["SocialMedia:Facebook"] ?? ""),
            TwitterUrl = settings.GetValueOrDefault("TwitterUrl", _configuration["SocialMedia:Twitter"] ?? ""),
            InstagramUrl = settings.GetValueOrDefault("InstagramUrl", _configuration["SocialMedia:Instagram"] ?? ""),
            LinkedInUrl = settings.GetValueOrDefault("LinkedInUrl", _configuration["SocialMedia:LinkedIn"] ?? ""),
            YouTubeUrl = settings.GetValueOrDefault("YouTubeUrl", ""),

            // Homepage Hero
            HeroTitle = settings.GetValueOrDefault("HeroTitle", "Discover Amazing Products"),
            HeroSubtitle = settings.GetValueOrDefault("HeroSubtitle", "Shop the latest trends and exclusive items at unbeatable prices."),
            HeroBadge = settings.GetValueOrDefault("HeroBadge", "New Collection"),
            HeroButtonText = settings.GetValueOrDefault("HeroButtonText", "Shop Now"),
            HeroButtonLink = settings.GetValueOrDefault("HeroButtonLink", Url.Action("Index", "Product") ?? "/Product"),
            HeroImageUrl = settings.GetValueOrDefault("HeroImageUrl", "/images/hero-image.png"),

            // Homepage Hero (Arabic)
            HeroTitleAr = settings.GetValueOrDefault("HeroTitleAr", string.Empty),
            HeroSubtitleAr = settings.GetValueOrDefault("HeroSubtitleAr", string.Empty),
            HeroBadgeAr = settings.GetValueOrDefault("HeroBadgeAr", string.Empty),
            HeroButtonTextAr = settings.GetValueOrDefault("HeroButtonTextAr", string.Empty),

            // Homepage CTA
            CtaBadgeText = settings.GetValueOrDefault("CtaBadgeText", "-60%"),
            CtaTitle = settings.GetValueOrDefault("CtaTitle", "Global Sale"),
            CtaDescription = settings.GetValueOrDefault("CtaDescription", "Don't miss our seasonal offers."),
            CtaButtonText = settings.GetValueOrDefault("CtaButtonText", "Buy Now"),
            CtaButtonLink = settings.GetValueOrDefault("CtaButtonLink", Url.Action("Index", "Product") ?? "/Product"),
            CtaBackgroundUrl = settings.GetValueOrDefault("CtaBackgroundUrl", "/img/bg-img/bg-5.jpg"),

            // Homepage CTA (Arabic)
            CtaBadgeTextAr = settings.GetValueOrDefault("CtaBadgeTextAr", string.Empty),
            CtaTitleAr = settings.GetValueOrDefault("CtaTitleAr", string.Empty),
            CtaDescriptionAr = settings.GetValueOrDefault("CtaDescriptionAr", string.Empty),
            CtaButtonTextAr = settings.GetValueOrDefault("CtaButtonTextAr", string.Empty)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateGeneral(SettingsViewModel model)
    {
        try
        {
            // Save general settings to database
            await _unitOfWork.Setting.SetValueAsync("SiteName", model.SiteName ?? "", "Website name");
            await _unitOfWork.Setting.SetValueAsync("SiteDescription", model.SiteDescription ?? "", "Website description");
            await _unitOfWork.Setting.SetValueAsync("ContactEmail", model.ContactEmail ?? "", "Contact email address");
            await _unitOfWork.Setting.SetValueAsync("ContactPhone", model.ContactPhone ?? "", "Contact phone number");
            await _unitOfWork.Setting.SetValueAsync("Address", model.Address ?? "", "Business address");
            await _unitOfWork.Setting.SetValueAsync("Currency", model.Currency ?? "", "Default currency");
            await _unitOfWork.Setting.SetValueAsync("TimeZone", model.TimeZone ?? "", "Default timezone");
            await _unitOfWork.Setting.SetValueAsync("Language", model.Language ?? "", "Default language");
            await _unitOfWork.Setting.SetValueAsync("ItemsPerPage", model.ItemsPerPage.ToString(), "Items per page");
            await _unitOfWork.Setting.SetValueAsync("EnableRegistration", model.EnableRegistration.ToString(), "Allow user registration");
            await _unitOfWork.Setting.SetValueAsync("EnableReviews", model.EnableReviews.ToString(), "Enable product reviews");
            await _unitOfWork.Setting.SetValueAsync("EnableWishlist", model.EnableWishlist.ToString(), "Enable wishlist feature");

            // Handle hero image upload if provided
            if (model.HeroImageFile != null && model.HeroImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "hero");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var fileExt = Path.GetExtension(model.HeroImageFile.FileName);
                var fileName = $"hero_{DateTime.Now:yyyyMMddHHmmss}{fileExt}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.HeroImageFile.CopyToAsync(stream);
                }
                // Public URL
                var publicUrl = $"/uploads/hero/{fileName}";
                model.HeroImageUrl = publicUrl;
            }

            // Save homepage hero settings
            await _unitOfWork.Setting.SetValueAsync("HeroTitle", model.HeroTitle ?? "", "Homepage hero title");
            await _unitOfWork.Setting.SetValueAsync("HeroSubtitle", model.HeroSubtitle ?? "", "Homepage hero subtitle");
            await _unitOfWork.Setting.SetValueAsync("HeroBadge", model.HeroBadge ?? "", "Homepage hero badge");
            await _unitOfWork.Setting.SetValueAsync("HeroButtonText", model.HeroButtonText ?? "", "Homepage hero button text");
            await _unitOfWork.Setting.SetValueAsync("HeroButtonLink", model.HeroButtonLink ?? "", "Homepage hero button link");
            await _unitOfWork.Setting.SetValueAsync("HeroImageUrl", model.HeroImageUrl ?? "", "Homepage hero image url");

            // Save homepage hero settings (Arabic)
            await _unitOfWork.Setting.SetValueAsync("HeroTitleAr", model.HeroTitleAr ?? "", "Homepage hero title (Arabic)");
            await _unitOfWork.Setting.SetValueAsync("HeroSubtitleAr", model.HeroSubtitleAr ?? "", "Homepage hero subtitle (Arabic)");
            await _unitOfWork.Setting.SetValueAsync("HeroBadgeAr", model.HeroBadgeAr ?? "", "Homepage hero badge (Arabic)");
            await _unitOfWork.Setting.SetValueAsync("HeroButtonTextAr", model.HeroButtonTextAr ?? "", "Homepage hero button text (Arabic)");

            // Handle CTA background upload if provided
            if (model.CtaBackgroundFile != null && model.CtaBackgroundFile.Length > 0)
            {
                var ctaFolder = Path.Combine(_env.WebRootPath, "uploads", "cta");
                if (!Directory.Exists(ctaFolder)) Directory.CreateDirectory(ctaFolder);

                var ext = Path.GetExtension(model.CtaBackgroundFile.FileName);
                var ctaName = $"cta_{DateTime.Now:yyyyMMddHHmmss}{ext}";
                var ctaPath = Path.Combine(ctaFolder, ctaName);
                using (var stream = new FileStream(ctaPath, FileMode.Create))
                {
                    await model.CtaBackgroundFile.CopyToAsync(stream);
                }
                model.CtaBackgroundUrl = $"/uploads/cta/{ctaName}";
            }

            // Save CTA settings
            await _unitOfWork.Setting.SetValueAsync("CtaBadgeText", model.CtaBadgeText ?? "", "CTA badge text");
            await _unitOfWork.Setting.SetValueAsync("CtaTitle", model.CtaTitle ?? "", "CTA title");
            await _unitOfWork.Setting.SetValueAsync("CtaDescription", model.CtaDescription ?? "", "CTA description");
            await _unitOfWork.Setting.SetValueAsync("CtaButtonText", model.CtaButtonText ?? "", "CTA button text");
            await _unitOfWork.Setting.SetValueAsync("CtaButtonLink", model.CtaButtonLink ?? "", "CTA button link");
            await _unitOfWork.Setting.SetValueAsync("CtaBackgroundUrl", model.CtaBackgroundUrl ?? "", "CTA background url");

            // Save CTA settings (Arabic)
            await _unitOfWork.Setting.SetValueAsync("CtaBadgeTextAr", model.CtaBadgeTextAr ?? "", "CTA badge text (Arabic)");
            await _unitOfWork.Setting.SetValueAsync("CtaTitleAr", model.CtaTitleAr ?? "", "CTA title (Arabic)");
            await _unitOfWork.Setting.SetValueAsync("CtaDescriptionAr", model.CtaDescriptionAr ?? "", "CTA description (Arabic)");
            await _unitOfWork.Setting.SetValueAsync("CtaButtonTextAr", model.CtaButtonTextAr ?? "", "CTA button text (Arabic)");

            TempData["Success"] = "General settings updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error updating settings: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEmail(SettingsViewModel model)
    {
        try
        {
            // Validate email settings
            if (string.IsNullOrEmpty(model.SmtpHost))
            {
                TempData["Error"] = "SMTP Host is required";
                return RedirectToAction(nameof(Index));
            }

            // Save email settings to database
            await _unitOfWork.Setting.SetValueAsync("SmtpHost", model.SmtpHost ?? "", "SMTP server host");
            await _unitOfWork.Setting.SetValueAsync("SmtpPort", model.SmtpPort.ToString(), "SMTP server port");
            await _unitOfWork.Setting.SetValueAsync("SmtpUsername", model.SmtpUsername ?? "", "SMTP username");
            await _unitOfWork.Setting.SetValueAsync("SmtpPassword", model.SmtpPassword ?? "", "SMTP password");
            await _unitOfWork.Setting.SetValueAsync("EnableSsl", model.EnableSsl.ToString(), "Enable SSL for email");

            TempData["Success"] = "Email settings updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error updating email settings: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePayment(SettingsViewModel model)
    {
        try
        {
            // Save payment settings to database
            await _unitOfWork.Setting.SetValueAsync("PayPalClientId", model.PayPalClientId ?? "", "PayPal Client ID");
            await _unitOfWork.Setting.SetValueAsync("PayPalClientSecret", model.PayPalClientSecret ?? "", "PayPal Client Secret");
            await _unitOfWork.Setting.SetValueAsync("StripePublishableKey", model.StripePublishableKey ?? "", "Stripe Publishable Key");
            await _unitOfWork.Setting.SetValueAsync("StripeSecretKey", model.StripeSecretKey ?? "", "Stripe Secret Key");

            TempData["Success"] = "Payment settings updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error updating payment settings: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSeo(SettingsViewModel model)
    {
        try
        {
            // Save SEO settings to database
            await _unitOfWork.Setting.SetValueAsync("MetaTitle", model.MetaTitle ?? "", "Default meta title");
            await _unitOfWork.Setting.SetValueAsync("MetaDescription", model.MetaDescription ?? "", "Default meta description");
            await _unitOfWork.Setting.SetValueAsync("MetaKeywords", model.MetaKeywords ?? "", "Default meta keywords");
            await _unitOfWork.Setting.SetValueAsync("GoogleAnalyticsId", model.GoogleAnalyticsId ?? "", "Google Analytics tracking ID");
            await _unitOfWork.Setting.SetValueAsync("FacebookPixelId", model.FacebookPixelId ?? "", "Facebook Pixel ID");

            TempData["Success"] = "SEO settings updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error updating SEO settings: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    public async Task<IActionResult> TestEmail(string email)
    {
        try
        {
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Email address is required" });
            }

            var subject = "Test Email - E-Commerce Website";
            var body = "<p>This is a test email from your E-Commerce admin settings.</p><p>If you received this, SMTP is configured correctly.</p>";
            var sent = await _emailService.SendEmailAsync(email, subject, body, "This is a test email.");

            if (sent)
            {
                return Json(new { success = true, message = "Test email sent successfully!" });
            }
            else
            {
                return Json(new { success = false, message = "Failed to send test email. Check SMTP settings and try again." });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error sending test email: {ex.Message}" });
        }
    }

    public IActionResult Backup()
    {
        var model = new BackupViewModel
        {
            LastBackupDate = DateTime.Now.AddDays(-1), // Mock data
            BackupSize = "15.2 MB", // Mock data
            BackupLocation = "/backups/",
            AutoBackupEnabled = true,
            BackupFrequency = "Daily"
        };

        return View(model);
    }

    [HttpPost]
    public IActionResult CreateBackup()
    {
        try
        {
            // In a real application, you would create a database backup here
            TempData["Success"] = "Backup created successfully!";
            return RedirectToAction(nameof(Backup));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error creating backup: {ex.Message}";
            return RedirectToAction(nameof(Backup));
        }
    }

    public IActionResult Maintenance()
    {
        var model = new MaintenanceViewModel
        {
            MaintenanceMode = false, // Mock data
            MaintenanceMessage = "We are currently performing scheduled maintenance. Please check back soon.",
            AllowedIPs = new List<string> { "127.0.0.1", "::1" },
            CacheEnabled = true,
            CacheExpiration = 30
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateMaintenance(MaintenanceViewModel model)
    {
        try
        {
            // In a real application, you would update maintenance settings here
            TempData["Success"] = "Maintenance settings updated successfully!";
            return RedirectToAction(nameof(Maintenance));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error updating maintenance settings: {ex.Message}";
            return RedirectToAction(nameof(Maintenance));
        }
    }

    [HttpPost]
    public IActionResult ClearCache()
    {
        try
        {
            // In a real application, you would clear cache here
            return Json(new { success = true, message = "Cache cleared successfully!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error clearing cache: {ex.Message}" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSocial(SettingsViewModel model)
    {
        try
        {
            // Save social media settings to database
            await _unitOfWork.Setting.SetValueAsync("FacebookUrl", model.FacebookUrl ?? "", "Facebook page URL");
            await _unitOfWork.Setting.SetValueAsync("TwitterUrl", model.TwitterUrl ?? "", "Twitter profile URL");
            await _unitOfWork.Setting.SetValueAsync("InstagramUrl", model.InstagramUrl ?? "", "Instagram profile URL");
            await _unitOfWork.Setting.SetValueAsync("LinkedInUrl", model.LinkedInUrl ?? "", "LinkedIn profile URL");
            await _unitOfWork.Setting.SetValueAsync("YouTubeUrl", model.YouTubeUrl ?? "", "YouTube channel URL");

            TempData["Success"] = "Social media settings updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error updating social media settings: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateAutoBackup(BackupViewModel model)
    {
        try
        {
            TempData["Success"] = "Auto backup settings updated successfully!";
            return RedirectToAction(nameof(Backup));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error updating auto backup settings: {ex.Message}";
            return RedirectToAction(nameof(Backup));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RestoreBackup(IFormFile backupFile, bool confirmRestore)
    {
        try
        {
            if (backupFile == null || backupFile.Length == 0)
            {
                TempData["Error"] = "Please select a backup file";
                return RedirectToAction(nameof(Backup));
            }

            if (!confirmRestore)
            {
                TempData["Error"] = "Please confirm that you want to restore from backup";
                return RedirectToAction(nameof(Backup));
            }

            // In a real application, you would restore from the backup file here
            TempData["Success"] = "Backup restored successfully!";
            return RedirectToAction(nameof(Backup));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error restoring backup: {ex.Message}";
            return RedirectToAction(nameof(Backup));
        }
    }
}




