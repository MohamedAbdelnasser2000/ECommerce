using Microsoft.AspNetCore.Mvc;
using ECommerceWebsite.Services;
using ECommerceWebsite.Models;

namespace ECommerceWebsite.Controllers;

public class NewsletterController : Controller
{
    private readonly INewsletterService _newsletterService;
    private readonly IEmailService _emailService;
    private readonly IEmailBackgroundQueue _emailQueue;
    private readonly ILogger<NewsletterController> _logger;

    public NewsletterController(
        INewsletterService newsletterService,
        IEmailService emailService,
        IEmailBackgroundQueue emailQueue,
        ILogger<NewsletterController> logger)
    {
        _newsletterService = newsletterService;
        _emailService = emailService;
        _emailQueue = emailQueue;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Subscribe(string email, string? firstName = null, string? lastName = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { success = false, message = "Email is required" });
            }

            var result = await _newsletterService.SubscribeAsync(email, firstName, lastName);

            if (result)
            {
                // Enqueue welcome email to send in background
                var subject = "Welcome to our Newsletter";
                var html = $"<h3>Welcome {(firstName ?? "Subscriber")}</h3><p>Thank you for subscribing!</p>";
                await _emailQueue.QueueEmailAsync(new EmailQueueItem(email, subject, html, $"Welcome {(firstName ?? "Subscriber")}\nThank you for subscribing!"));

                return Json(new { success = true, message = "Successfully subscribed to newsletter!" });
            }
            else
            {
                return Json(new { success = false, message = "Email is already subscribed" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to newsletter: {Email}", email);
            return Json(new { success = false, message = "An error occurred while subscribing" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Unsubscribe(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { success = false, message = "Email is required" });
            }

            var result = await _newsletterService.UnsubscribeAsync(email);

            if (result)
            {
                return Json(new { success = true, message = "Successfully unsubscribed from newsletter" });
            }
            else
            {
                return Json(new { success = false, message = "Email not found in subscription list" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from newsletter: {Email}", email);
            return Json(new { success = false, message = "An error occurred while unsubscribing" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> UnsubscribeByToken(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                ViewBag.Message = "Invalid unsubscribe link";
                ViewBag.Success = false;
                return View();
            }

            var result = await _newsletterService.UnsubscribeByTokenAsync(token);

            if (result)
            {
                ViewBag.Message = "You have been successfully unsubscribed from our newsletter";
                ViewBag.Success = true;
            }
            else
            {
                ViewBag.Message = "Invalid or expired unsubscribe link";
                ViewBag.Success = false;
            }

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing with token: {Token}", token);
            ViewBag.Message = "An error occurred while processing your request";
            ViewBag.Success = false;
            return View();
        }
    }

    [HttpGet]
    public async Task<IActionResult> IsSubscribed(string email)
    {
        try
        {
            var isSubscribed = await _newsletterService.IsSubscribedAsync(email);
            return Json(new { isSubscribed });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking subscription status: {Email}", email);
            return Json(new { isSubscribed = false });
        }
    }

    [HttpGet]
    public IActionResult Subscribe()
    {
        return View();
    }
}
