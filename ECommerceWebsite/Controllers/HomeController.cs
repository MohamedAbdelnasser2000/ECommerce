using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ECommerceWebsite.Models;
using ECommerceWebsite.Repository;
using ECommerceWebsite.Services;
using ECommerceWebsite.ViewModels;

namespace ECommerceWebsite.Controllers;

public class HomeController : BaseController
{
    private readonly ILogger<HomeController> _logger;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;

    public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork, IEmailService emailService, INotificationService notificationService) : base(unitOfWork)
    {
        _logger = logger;
        _emailService = emailService;
        _notificationService = notificationService;
    }

    public async Task<IActionResult> Index()
    {
        var featuredProducts = await _unitOfWork.Product.GetAllAsync(
            filter: p => p.IsActive && p.IsFeatured && p.StockQuantity > 0,
            includeProperties: "Category"
        );

        var newCarouselProducts = await _unitOfWork.Product.GetAllAsync(
            filter: p => p.IsActive && p.IsFeaturedInNewCarousel && p.StockQuantity > 0,
            includeProperties: "Category"
        );
        ViewBag.NewCarouselProducts = newCarouselProducts;

        IEnumerable<Category> categories;
        try
        {
            categories = await _unitOfWork.Category.GetAllAsync(
                filter: c => c.IsActive && c.ShowOnHome
            );
        }
        catch (Exception ex)
        {
            // If database schema isn't updated (missing ShowInHeader column or similar), log and continue with empty categories
            _logger.LogWarning(ex, "Failed to load categories for homepage (possible missing migration). Falling back to empty list.");
            categories = Enumerable.Empty<Category>();
        }

        ViewBag.Categories = categories;
        return View(featuredProducts);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Terms()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Cookies()
    {
        return View();
    }

    // Contact and Blog pages to support header links
    [HttpGet]
    public async Task<IActionResult> Contact()
    {
        // Get contact information from settings
        var contactEmail = await _unitOfWork.Setting.GetValueAsync("ContactEmail") ?? "contact@ecommerce.com";
        var contactPhone = await _unitOfWork.Setting.GetValueAsync("ContactPhone") ?? "+1 (555) 123-4567";
        var address = await _unitOfWork.Setting.GetValueAsync("Address") ?? "123 Main Street, City, Country";
        var facebookUrl = await _unitOfWork.Setting.GetValueAsync("FacebookUrl");
        var twitterUrl = await _unitOfWork.Setting.GetValueAsync("TwitterUrl");
        var instagramUrl = await _unitOfWork.Setting.GetValueAsync("InstagramUrl");

        ViewBag.ContactEmail = contactEmail;
        ViewBag.ContactPhone = contactPhone;
        ViewBag.Address = address;
        ViewBag.FacebookUrl = facebookUrl;
        ViewBag.TwitterUrl = twitterUrl;
        ViewBag.InstagramUrl = instagramUrl;

        return View(new ContactViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Persist to DB
        var entity = new ContactMessage
        {
            Name = model.Name,
            Email = model.Email,
            Subject = model.Subject,
            Message = model.Message,
            Phone = model.Phone,
            Consent = model.Consent,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers["User-Agent"].ToString()
        };

        await _unitOfWork.ContactMessage.AddAsync(entity);
        await _unitOfWork.SaveAsync();

        // Notify admins in dashboard
        await _notificationService.NotifyAdminsNewContactMessageAsync(entity);

        // Optional: email forward to configured contact email
        var to = ViewBag.ContactEmail as string ?? "admin@ecommerce.com";
        var subject = $"New Contact Message from {model.Name}";
        var html = $@"<h2>New Contact Message</h2>
<p><strong>Name:</strong> {model.Name}</p>
<p><strong>Email:</strong> {model.Email}</p>
<p><strong>Subject:</strong> {model.Subject}</p>
<p><strong>Phone:</strong> {model.Phone}</p>
<p><strong>Consent:</strong> {(model.Consent ? "Yes" : "No")}</p>
<p><strong>Message:</strong><br/>{System.Net.WebUtility.HtmlEncode(model.Message).Replace("\n","<br/>")}</p>
<hr/>
<p><small>IP: {entity.IpAddress} | UA: {entity.UserAgent} | At: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</small></p>";

        var sent = await _emailService.SendEmailAsync(to, subject, html, model.Message);

        TempData["ContactStatus"] = "success"; // success regardless of email; already saved and notified
        return RedirectToAction(nameof(Contact));
    }

    [HttpGet]
    public IActionResult Blog() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
