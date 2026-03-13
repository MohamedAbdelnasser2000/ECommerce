using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ECommerceWebsite.Repository;
using Microsoft.Extensions.Configuration;

namespace ECommerceWebsite.Controllers;

public class BaseController : Controller
{
    protected readonly IUnitOfWork _unitOfWork;
    private IConfiguration? _configuration;

    public BaseController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        _configuration ??= HttpContext?.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
        // Get site settings and make them available to all views
        var siteName = await _unitOfWork.Setting.GetValueAsync("SiteName") ?? "E-Commerce Store";
        var siteDescription = await _unitOfWork.Setting.GetValueAsync("SiteDescription") ?? "Your online shopping destination";
        var contactEmail = await _unitOfWork.Setting.GetValueAsync("ContactEmail") ?? "admin@ecommerce.com";
        var contactPhone = await _unitOfWork.Setting.GetValueAsync("ContactPhone") ?? "+1-234-567-8900";
        var address = await _unitOfWork.Setting.GetValueAsync("Address") ?? "123 Main St, City, State 12345";
        var facebookUrl = await _unitOfWork.Setting.GetValueAsync("FacebookUrl") ?? "";
        var twitterUrl = await _unitOfWork.Setting.GetValueAsync("TwitterUrl") ?? "";
        var instagramUrl = await _unitOfWork.Setting.GetValueAsync("InstagramUrl") ?? "";
        var linkedInUrl = await _unitOfWork.Setting.GetValueAsync("LinkedInUrl") ?? "";
        var youTubeUrl = await _unitOfWork.Setting.GetValueAsync("YouTubeUrl") ?? "";

        // SEO
        var metaTitle = await _unitOfWork.Setting.GetValueAsync("MetaTitle") ?? "E-Commerce Store";
        var metaDescription = await _unitOfWork.Setting.GetValueAsync("MetaDescription") ?? "Best online shopping experience";
        var metaKeywords = await _unitOfWork.Setting.GetValueAsync("MetaKeywords") ?? "ecommerce, shopping, online store";
        var googleAnalyticsId = await _unitOfWork.Setting.GetValueAsync("GoogleAnalyticsId") ?? string.Empty;
        var facebookPixelId = await _unitOfWork.Setting.GetValueAsync("FacebookPixelId") ?? string.Empty;

        // Helper to fetch localized setting value (tries <Key>Ar when UI culture is Arabic)
        static bool IsArabic() => System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ar", StringComparison.OrdinalIgnoreCase);
        async Task<string?> GetLocalizedSettingAsync(string key)
        {
            if (IsArabic())
            {
                var arVal = await _unitOfWork.Setting.GetValueAsync(key + "Ar");
                if (!string.IsNullOrWhiteSpace(arVal)) return arVal;
            }
            return await _unitOfWork.Setting.GetValueAsync(key);
        }

        // Homepage Hero (localized via DB where available)
        var heroTitle = await GetLocalizedSettingAsync("HeroTitle") ?? "Discover Amazing Products";
        var heroSubtitle = await GetLocalizedSettingAsync("HeroSubtitle") ?? "Shop the latest trends and exclusive items at unbeatable prices.";
        var heroBadge = await GetLocalizedSettingAsync("HeroBadge") ?? "New Collection";
        var heroButtonText = await GetLocalizedSettingAsync("HeroButtonText") ?? "Shop Now";
        var heroButtonLink = await _unitOfWork.Setting.GetValueAsync("HeroButtonLink") ?? "/Product"; // link not localized
        var heroImageUrl = await _unitOfWork.Setting.GetValueAsync("HeroImageUrl") ?? "/images/hero-image.png"; // image not localized

        // Homepage CTA (localized via DB where available)
        var ctaBadge = await GetLocalizedSettingAsync("CtaBadgeText") ?? "-60%";
        var ctaTitle = await GetLocalizedSettingAsync("CtaTitle") ?? "Global Sale";
        var ctaDesc = await GetLocalizedSettingAsync("CtaDescription");
        ctaDesc = string.IsNullOrWhiteSpace(ctaDesc) ? "Don't miss our seasonal offers." : ctaDesc;
        var ctaBtnText = await GetLocalizedSettingAsync("CtaButtonText") ?? "Buy Now";
        var ctaBtnLink = await _unitOfWork.Setting.GetValueAsync("CtaButtonLink") ?? "/Product"; // link not localized
        var ctaBg = await _unitOfWork.Setting.GetValueAsync("CtaBackgroundUrl") ?? "/img/bg-img/bg-5.jpg"; // background not localized

        ViewBag.SiteName = siteName;
        ViewBag.SiteDescription = siteDescription;
        ViewBag.ContactEmail = contactEmail;
        ViewBag.ContactPhone = contactPhone;
        ViewBag.Address = address;
        ViewBag.FacebookUrl = facebookUrl;
        ViewBag.TwitterUrl = twitterUrl;
        ViewBag.InstagramUrl = instagramUrl;
        ViewBag.LinkedInUrl = linkedInUrl;
        ViewBag.YouTubeUrl = youTubeUrl;

        ViewBag.MetaTitle = metaTitle;
        ViewBag.MetaDescription = metaDescription;
        ViewBag.MetaKeywords = metaKeywords;
        ViewBag.GoogleAnalyticsId = googleAnalyticsId;
        ViewBag.FacebookPixelId = facebookPixelId;

        // Pass infra configs from appsettings
        try
        {
            var cdn = _configuration?["Cdn:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(cdn))
            {
                ViewBag.CdnBaseUrl = cdn.TrimEnd('/');
            }
            var ga = _configuration?["Analytics:GoogleAnalyticsId"];
            if (!string.IsNullOrWhiteSpace(ga))
            {
                ViewBag.GoogleAnalyticsId = ga;
            }
        }
        catch { }

        ViewBag.HeroTitle = heroTitle;
        ViewBag.HeroSubtitle = heroSubtitle;
        ViewBag.HeroBadge = heroBadge;
        ViewBag.HeroButtonText = heroButtonText;
        ViewBag.HeroButtonLink = heroButtonLink;
        ViewBag.HeroImageUrl = heroImageUrl;

        ViewBag.CtaBadgeText = ctaBadge;
        ViewBag.CtaTitle = ctaTitle;
        ViewBag.CtaDescription = ctaDesc;
        ViewBag.CtaButtonText = ctaBtnText;
        ViewBag.CtaButtonLink = ctaBtnLink;
        ViewBag.CtaBackgroundUrl = ctaBg;

        // Load categories to show in the header (active + ShowInHeader)
        try
        {
            // Query only columns that exist for sure (avoid ShowInHeader before migration applies)
            var headerCategories = await _unitOfWork.Category.GetAllAsync(
                filter: c => c.IsActive
            );
            ViewBag.HeaderCategories = headerCategories
                .Where(c => c.ShowInHeader)
                .OrderBy(c => c.Name)
                .ToList();
        }
        catch
        {
            ViewBag.HeaderCategories = Enumerable.Empty<object>();
        }

        await next();
    }
}
