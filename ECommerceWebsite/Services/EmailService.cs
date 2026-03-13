using System.Net;
using System.Net.Mail;
using System.Text;
using ECommerceWebsite.Data;
using ECommerceWebsite.Models;
using ECommerceWebsite.Repository;
using Microsoft.EntityFrameworkCore;

namespace ECommerceWebsite.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ApplicationDbContext context, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _context = context;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent, string? textContent = null)
    {
        try
        {
            // Prefer DB settings; fall back to appsettings.json
            var dbSettings = await _context.Settings.ToDictionaryAsync(s => s.Key, s => s.Value);
            string GetSetting(string key, string? fallback = null)
                => dbSettings.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v)
                    ? v
                    : (fallback ?? string.Empty);

            var smtpSection = _configuration.GetSection("EmailSettings");
            var smtpHost = GetSetting("SmtpHost", smtpSection["SmtpHost"] ?? "smtp.gmail.com");
            var smtpPortStr = GetSetting("SmtpPort", smtpSection["SmtpPort"] ?? "587");
            var smtpPort = int.TryParse(smtpPortStr, out var p) ? p : 587;
            var smtpUsername = GetSetting("SmtpUsername", smtpSection["SmtpUsername"] ?? "");
            var smtpPassword = GetSetting("SmtpPassword", smtpSection["SmtpPassword"] ?? "");
            var fromEmail = GetSetting("FromEmail", smtpSection["FromEmail"] ?? smtpUsername);
            var fromName = GetSetting("FromName", smtpSection["FromName"] ?? (_configuration["StoreName"] ?? "E-Commerce Store"));

            var enableSslStr = GetSetting("EnableSsl", smtpSection["EnableSsl"] ?? "true");
            var enableSsl = bool.TryParse(enableSslStr, out var ssl) ? ssl : true;

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = htmlContent,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            if (!string.IsNullOrEmpty(textContent))
            {
                var textView = AlternateView.CreateAlternateViewFromString(textContent, Encoding.UTF8, "text/plain");
                mailMessage.AlternateViews.Add(textView);
            }

            await client.SendMailAsync(mailMessage);

            // Log successful email
            await LogEmailAsync(toEmail, subject, htmlContent, EmailStatus.Sent);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            
            // Log failed email
            await LogEmailAsync(toEmail, subject, htmlContent, EmailStatus.Failed, ex.Message);
            
            return false;
        }
    }

    public async Task<bool> SendTemplateEmailAsync(string toEmail, EmailTemplateType templateType, Dictionary<string, string> placeholders)
    {
        try
        {
            var template = await _context.EmailTemplates
                .FirstOrDefaultAsync(t => t.Type == templateType && t.IsActive);

            if (template == null)
            {
                _logger.LogWarning("Email template not found for type: {TemplateType}", templateType);
                return false;
            }

            var subject = ReplacePlaceholders(template.Subject, placeholders);
            var htmlContent = ReplacePlaceholders(template.HtmlContent, placeholders);
            var textContent = ReplacePlaceholders(template.TextContent, placeholders);

            var result = await SendEmailAsync(toEmail, subject, htmlContent, textContent);

            if (result)
            {
                await LogEmailAsync(toEmail, subject, htmlContent, EmailStatus.Sent, null, template.Id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send template email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendWelcomeEmailAsync(string toEmail, string firstName)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "FirstName", firstName },
            { "StoreName", _configuration["StoreName"] ?? "E-Commerce Store" }
        };

        return await SendTemplateEmailAsync(toEmail, EmailTemplateType.Welcome, placeholders);
    }

    public async Task<bool> SendOrderConfirmationAsync(string toEmail, Order order)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "FirstName", order.ShippingFirstName },
            { "OrderNumber", order.OrderNumber },
            { "TotalAmount", order.TotalAmount.ToString("F2") },
            { "StoreName", _configuration["StoreName"] ?? "E-Commerce Store" }
        };

        return await SendTemplateEmailAsync(toEmail, EmailTemplateType.OrderConfirmation, placeholders);
    }

    public async Task<bool> SendOrderStatusUpdateAsync(string toEmail, Order order)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "FirstName", order.ShippingFirstName },
            { "OrderNumber", order.OrderNumber },
            { "OrderStatus", order.Status.ToString() },
            { "StoreName", _configuration["StoreName"] ?? "E-Commerce Store" }
        };

        return await SendTemplateEmailAsync(toEmail, EmailTemplateType.OrderStatusUpdate, placeholders);
    }

    public async Task<bool> SendNewsletterAsync(string toEmail, string content, string? firstName = null)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "FirstName", firstName ?? "Subscriber" },
            { "NewsletterContent", content },
            { "StoreName", _configuration["StoreName"] ?? "E-Commerce Store" },
            { "UnsubscribeUrl", $"{_configuration["BaseUrl"]}/Newsletter/Unsubscribe?email={toEmail}" }
        };

        return await SendTemplateEmailAsync(toEmail, EmailTemplateType.Newsletter, placeholders);
    }

    public async Task<bool> SendLowStockAlertAsync(string toEmail, Product product)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "ProductName", product.Name },
            { "StockQuantity", product.StockQuantity.ToString() },
            { "StoreName", _configuration["StoreName"] ?? "E-Commerce Store" }
        };

        return await SendTemplateEmailAsync(toEmail, EmailTemplateType.LowStock, placeholders);
    }

    public async Task<bool> SendNewReviewNotificationAsync(string toEmail, Review review)
    {
        var subject = "New Review Received";
        var htmlContent = $@"
            <h1>New Review Received</h1>
            <p>A new review has been submitted for product: <strong>{review.Product?.Name}</strong></p>
            <p><strong>Rating:</strong> {review.Rating}/5</p>
            <p><strong>Comment:</strong> {review.Comment}</p>
            <p><strong>Reviewer:</strong> {review.User?.FirstName} {review.User?.LastName}</p>
        ";

        return await SendEmailAsync(toEmail, subject, htmlContent);
    }

    public async Task<bool> SendBulkNewsletterAsync(string content, List<string>? specificEmails = null)
    {
        try
        {
            var emails = specificEmails ?? await _context.NewsletterSubscriptions
                .Where(ns => ns.IsActive)
                .Select(ns => ns.Email)
                .ToListAsync();

            var successCount = 0;
            var tasks = new List<Task<bool>>();

            foreach (var email in emails)
            {
                var subscription = await _context.NewsletterSubscriptions
                    .FirstOrDefaultAsync(ns => ns.Email == email);

                tasks.Add(SendNewsletterAsync(email, content, subscription?.FirstName));
            }

            var results = await Task.WhenAll(tasks);
            successCount = results.Count(r => r);

            _logger.LogInformation("Bulk newsletter sent to {SuccessCount}/{TotalCount} subscribers", 
                successCount, emails.Count);

            return successCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send bulk newsletter");
            return false;
        }
    }

    private string ReplacePlaceholders(string content, Dictionary<string, string> placeholders)
    {
        foreach (var placeholder in placeholders)
        {
            content = content.Replace($"{{{placeholder.Key}}}", placeholder.Value);
        }
        return content;
    }

    private async Task LogEmailAsync(string toEmail, string subject, string content, EmailStatus status, string? errorMessage = null, int? templateId = null)
    {
        try
        {
            var emailLog = new EmailLog
            {
                ToEmail = toEmail,
                Subject = subject,
                Content = content,
                Status = status,
                ErrorMessage = errorMessage,
                TemplateId = templateId,
                SentAt = DateTime.Now
            };

            _context.EmailLogs.Add(emailLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log email");
        }
    }
}
