using Microsoft.AspNetCore.Mvc;
using ECommerceWebsite.Repository;
using System.Text;

namespace ECommerceWebsite.Controllers;

public class SitemapController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public SitemapController(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    [HttpGet("sitemap.xml")]
    public async Task<IActionResult> Sitemap()
    {
        var baseUrl = _configuration["BaseUrl"]?.TrimEnd('/') ?? ($"{Request.Scheme}://{Request.Host}");

        var urls = new List<(string loc, string? lastmod, string changefreq, string priority)>();

        // Static pages
        urls.Add(($"{baseUrl}/", DateTime.UtcNow.ToString("yyyy-MM-dd"), "daily", "1.0"));
        urls.Add(($"{baseUrl}/Product", DateTime.UtcNow.ToString("yyyy-MM-dd"), "daily", "0.8"));
        urls.Add(($"{baseUrl}/Home/Contact", null, "yearly", "0.3"));
        urls.Add(($"{baseUrl}/Home/Privacy", null, "yearly", "0.2"));
        urls.Add(($"{baseUrl}/Home/Terms", null, "yearly", "0.2"));
        urls.Add(($"{baseUrl}/Home/Cookies", null, "yearly", "0.2"));

        // Categories
        var categories = await _unitOfWork.Category.GetAllAsync(filter: c => c.IsActive);
        foreach (var c in categories)
        {
            var last = (c.GetType().GetProperty("UpdatedAt")?.GetValue(c) as DateTime?)
                       ?? (c.GetType().GetProperty("CreatedAt")?.GetValue(c) as DateTime?)
                       ?? DateTime.UtcNow;
            urls.Add(($"{baseUrl}/Product?categoryId={c.Id}", last.ToString("yyyy-MM-dd"), "weekly", "0.6"));
        }

        // Products
        var products = await _unitOfWork.Product.GetAllAsync(filter: p => p.IsActive);
        foreach (var p in products)
        {
            var last = (p.GetType().GetProperty("UpdatedAt")?.GetValue(p) as DateTime?)
                       ?? (p.GetType().GetProperty("CreatedAt")?.GetValue(p) as DateTime?)
                       ?? DateTime.UtcNow;
            urls.Add(($"{baseUrl}/Product/Details/{p.Id}", last.ToString("yyyy-MM-dd"), "weekly", "0.7"));
        }

        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.Append("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
        foreach (var u in urls)
        {
            sb.Append("<url>");
            sb.Append($"<loc>{System.Security.SecurityElement.Escape(u.loc)}</loc>");
            if (!string.IsNullOrEmpty(u.lastmod)) sb.Append($"<lastmod>{u.lastmod}</lastmod>");
            sb.Append($"<changefreq>{u.changefreq}</changefreq>");
            sb.Append($"<priority>{u.priority}</priority>");
            sb.Append("</url>");
        }
        sb.Append("</urlset>");

        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }

    [HttpGet("robots.txt")]
    public IActionResult Robots()
    {
        var baseUrl = _configuration["BaseUrl"]?.TrimEnd('/') ?? ($"{Request.Scheme}://{Request.Host}");
        var content = new StringBuilder()
            .AppendLine("User-agent: *")
            .AppendLine("Allow: /")
            .AppendLine("Disallow: /admin/")
            .AppendLine("Disallow: /Areas/Admin/")
            .AppendLine($"Sitemap: {baseUrl}/sitemap.xml")
            .ToString();
        return Content(content, "text/plain", Encoding.UTF8);
    }
}


