using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerceWebsite.Repository;
using ECommerceWebsite.Services;
using ECommerceWebsite.Models;
using System.Text;

namespace ECommerceWebsite.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class EmailController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IEmailBackgroundQueue _emailQueue;
    private readonly INewsletterService _newsletterService;

    public EmailController(IUnitOfWork unitOfWork, IEmailService emailService, INewsletterService newsletterService, IEmailBackgroundQueue emailQueue)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _newsletterService = newsletterService;
        _emailQueue = emailQueue;
    }

    public async Task<IActionResult> Index()
    {
        var emailLogs = await _unitOfWork.EmailLog.GetRecentLogsAsync(100);
        return View(emailLogs);
    }

    public async Task<IActionResult> Templates()
    {
        var templates = await _unitOfWork.EmailTemplate.GetActiveTemplatesAsync();
        return View(templates);
    }

    public async Task<IActionResult> CreateTemplate()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateTemplate(EmailTemplate template)
    {
        if (ModelState.IsValid)
        {
            var isUnique = await _unitOfWork.EmailTemplate.IsTemplateNameUniqueAsync(template.Name);
            if (!isUnique)
            {
                ModelState.AddModelError("Name", "Template name already exists");
                return View(template);
            }

            template.CreatedAt = DateTime.Now;
            await _unitOfWork.EmailTemplate.AddAsync(template);
            await _unitOfWork.SaveAsync();

            TempData["Success"] = "Email template created successfully";
            return RedirectToAction(nameof(Templates));
        }

        return View(template);
    }

    public async Task<IActionResult> EditTemplate(int id)
    {
        var template = await _unitOfWork.EmailTemplate.GetByIdAsync(id);
        if (template == null)
            return NotFound();

        return View(template);
    }

    [HttpPost]
    public async Task<IActionResult> EditTemplate(EmailTemplate template)
    {
        if (ModelState.IsValid)
        {
            var isUnique = await _unitOfWork.EmailTemplate.IsTemplateNameUniqueAsync(template.Name, template.Id);
            if (!isUnique)
            {
                ModelState.AddModelError("Name", "Template name already exists");
                return View(template);
            }

            template.UpdatedAt = DateTime.Now;
            _unitOfWork.EmailTemplate.Update(template);
            await _unitOfWork.SaveAsync();

            TempData["Success"] = "Email template updated successfully";
            return RedirectToAction(nameof(Templates));
        }

        return View(template);
    }

    [HttpGet]
    public async Task<IActionResult> PreviewTemplate(int id)
    {
        try
        {
            var template = await _unitOfWork.EmailTemplate.GetByIdAsync(id);
            if (template == null)
            {
                return Json(new { success = false, message = "Template not found" });
            }

            return Json(new {
                success = true,
                template = new {
                    name = template.Name,
                    subject = template.Subject,
                    htmlContent = template.HtmlContent,
                    textContent = template.TextContent
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Failed to load template" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        try
        {
            var template = await _unitOfWork.EmailTemplate.GetByIdAsync(id);
            if (template == null)
            {
                return Json(new { success = false, message = "Template not found" });
            }

            // Soft delete - mark as inactive instead of removing
            template.IsActive = false;
            template.UpdatedAt = DateTime.Now;
            _unitOfWork.EmailTemplate.Update(template);
            await _unitOfWork.SaveAsync();

            return Json(new { success = true, message = "Template deleted successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Failed to delete template" });
        }
    }

    public async Task<IActionResult> Newsletter()
    {
        var subscriptions = await _newsletterService.GetActiveSubscriptionsAsync();
        ViewBag.SubscriberCount = await _newsletterService.GetSubscriberCountAsync();
        return View(subscriptions);
    }

    [HttpPost]
    public async Task<IActionResult> SendNewsletter(string subject, string content)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Subject and content are required";
                return RedirectToAction(nameof(Newsletter));
            }

            // Get all active subscribers
            var subscribers = await _newsletterService.GetActiveSubscriptionsAsync();

            if (!subscribers.Any())
            {
                TempData["Warning"] = "No active subscribers found";
                return RedirectToAction(nameof(Newsletter));
            }

            // Enqueue newsletter emails for background processing
            var enqueued = 0;
            foreach (var subscriber in subscribers)
            {
                var personalizedContent = content.Replace("{{CustomerName}}",
                    !string.IsNullOrEmpty(subscriber.FirstName) ? subscriber.FirstName : "Valued Customer");
                await _emailQueue.QueueEmailAsync(new EmailQueueItem(subscriber.Email, subject, personalizedContent));
                enqueued++;
            }

            TempData["Success"] = $"Newsletter queued for {enqueued} subscribers. Delivery will process in background.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "An error occurred while sending newsletter: " + ex.Message;
        }

        return RedirectToAction(nameof(Newsletter));
    }

    [HttpPost]
    public async Task<IActionResult> UnsubscribeUser(string email)
    {
        try
        {
            var subscription = await _newsletterService.GetSubscriptionByEmailAsync(email);
            if (subscription != null)
            {
                await _newsletterService.UnsubscribeAsync(email);
                return Json(new { success = true, message = "User unsubscribed successfully" });
            }
            return Json(new { success = false, message = "Subscription not found" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Failed to unsubscribe user" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ResubscribeUser(string email)
    {
        try
        {
            var subscription = await _newsletterService.GetSubscriptionByEmailAsync(email);
            if (subscription != null)
            {
                await _newsletterService.ResubscribeAsync(email);
                return Json(new { success = true, message = "User resubscribed successfully" });
            }
            return Json(new { success = false, message = "Subscription not found" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Failed to resubscribe user" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportSubscribers()
    {
        try
        {
            var subscribers = await _newsletterService.GetAllSubscriptionsAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Email,First Name,Last Name,Subscribed Date,Status");

            foreach (var subscriber in subscribers)
            {
                csv.AppendLine($"{subscriber.Email},{subscriber.FirstName},{subscriber.LastName},{subscriber.SubscribedAt:yyyy-MM-dd},{(subscriber.IsActive ? "Active" : "Unsubscribed")}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"newsletter_subscribers_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            return File(bytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Failed to export subscribers";
            return RedirectToAction(nameof(Newsletter));
        }
    }

    public async Task<IActionResult> EmailStats()
    {
        var today = DateTime.Today;
        var thisWeek = today.AddDays(-7);
        var thisMonth = today.AddDays(-30);

        ViewBag.TodaySent = await _unitOfWork.EmailLog.GetSentCountAsync(today);
        ViewBag.WeekSent = await _unitOfWork.EmailLog.GetSentCountAsync(thisWeek);
        ViewBag.MonthSent = await _unitOfWork.EmailLog.GetSentCountAsync(thisMonth);

        ViewBag.TodayFailed = await _unitOfWork.EmailLog.GetFailedCountAsync(today);
        ViewBag.WeekFailed = await _unitOfWork.EmailLog.GetFailedCountAsync(thisWeek);
        ViewBag.MonthFailed = await _unitOfWork.EmailLog.GetFailedCountAsync(thisMonth);

        ViewBag.TotalSubscribers = await _newsletterService.GetSubscriberCountAsync();

        return View();
    }
}
