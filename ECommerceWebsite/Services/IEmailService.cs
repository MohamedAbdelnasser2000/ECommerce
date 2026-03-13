using ECommerceWebsite.Models;

namespace ECommerceWebsite.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent, string? textContent = null);
    Task<bool> SendTemplateEmailAsync(string toEmail, EmailTemplateType templateType, Dictionary<string, string> placeholders);
    Task<bool> SendWelcomeEmailAsync(string toEmail, string firstName);
    Task<bool> SendOrderConfirmationAsync(string toEmail, Order order);
    Task<bool> SendOrderStatusUpdateAsync(string toEmail, Order order);
    Task<bool> SendNewsletterAsync(string toEmail, string content, string? firstName = null);
    Task<bool> SendLowStockAlertAsync(string toEmail, Product product);
    Task<bool> SendNewReviewNotificationAsync(string toEmail, Review review);
    Task<bool> SendBulkNewsletterAsync(string content, List<string>? specificEmails = null);
}

public interface INewsletterService
{
    Task<bool> SubscribeAsync(string email, string? firstName = null, string? lastName = null);
    Task<bool> UnsubscribeAsync(string email);
    Task<bool> UnsubscribeByTokenAsync(string token);
    Task<bool> ResubscribeAsync(string email);
    Task<IEnumerable<NewsletterSubscription>> GetActiveSubscriptionsAsync();
    Task<IEnumerable<NewsletterSubscription>> GetAllSubscriptionsAsync();
    Task<NewsletterSubscription?> GetSubscriptionByEmailAsync(string email);
    Task<bool> IsSubscribedAsync(string email);
    Task<int> GetSubscriberCountAsync();
    string GenerateUnsubscribeToken(string email);
}
