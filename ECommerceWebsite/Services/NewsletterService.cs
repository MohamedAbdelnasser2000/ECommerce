using System.Security.Cryptography;
using System.Text;
using ECommerceWebsite.Data;
using ECommerceWebsite.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerceWebsite.Services;

public class NewsletterService : INewsletterService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NewsletterService> _logger;

    public NewsletterService(ApplicationDbContext context, ILogger<NewsletterService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> SubscribeAsync(string email, string? firstName = null, string? lastName = null)
    {
        try
        {
            var existingSubscription = await _context.NewsletterSubscriptions
                .FirstOrDefaultAsync(ns => ns.Email.ToLower() == email.ToLower());

            if (existingSubscription != null)
            {
                if (!existingSubscription.IsActive)
                {
                    // Reactivate subscription
                    existingSubscription.IsActive = true;
                    existingSubscription.SubscribedAt = DateTime.Now;
                    existingSubscription.UnsubscribedAt = null;
                    existingSubscription.FirstName = firstName ?? existingSubscription.FirstName;
                    existingSubscription.LastName = lastName ?? existingSubscription.LastName;

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Newsletter subscription reactivated for {Email}", email);
                    return true;
                }
                else
                {
                    _logger.LogInformation("Email {Email} is already subscribed to newsletter", email);
                    return false; // Already subscribed
                }
            }

            var subscription = new NewsletterSubscription
            {
                Email = email.ToLower(),
                FirstName = firstName,
                LastName = lastName,
                IsActive = true,
                SubscribedAt = DateTime.Now,
                UnsubscribeToken = GenerateUnsubscribeToken(email)
            };

            _context.NewsletterSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New newsletter subscription created for {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe {Email} to newsletter", email);
            return false;
        }
    }

    public async Task<bool> UnsubscribeAsync(string email)
    {
        try
        {
            var subscription = await _context.NewsletterSubscriptions
                .FirstOrDefaultAsync(ns => ns.Email.ToLower() == email.ToLower() && ns.IsActive);

            if (subscription != null)
            {
                subscription.IsActive = false;
                subscription.UnsubscribedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Newsletter subscription cancelled for {Email}", email);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe {Email} from newsletter", email);
            return false;
        }
    }

    public async Task<bool> UnsubscribeByTokenAsync(string token)
    {
        try
        {
            var subscription = await _context.NewsletterSubscriptions
                .FirstOrDefaultAsync(ns => ns.UnsubscribeToken == token && ns.IsActive);

            if (subscription != null)
            {
                subscription.IsActive = false;
                subscription.UnsubscribedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Newsletter subscription cancelled by token for {Email}", subscription.Email);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe by token {Token}", token);
            return false;
        }
    }

    public async Task<bool> ResubscribeAsync(string email)
    {
        try
        {
            var subscription = await _context.NewsletterSubscriptions
                .FirstOrDefaultAsync(ns => ns.Email.ToLower() == email.ToLower());

            if (subscription != null)
            {
                subscription.IsActive = true;
                subscription.SubscribedAt = DateTime.Now;
                subscription.UnsubscribedAt = null;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Newsletter subscription reactivated for {Email}", email);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resubscribe {Email} to newsletter", email);
            return false;
        }
    }

    public async Task<IEnumerable<NewsletterSubscription>> GetActiveSubscriptionsAsync()
    {
        return await _context.NewsletterSubscriptions
            .Where(ns => ns.IsActive)
            .OrderBy(ns => ns.Email)
            .ToListAsync();
    }

    public async Task<IEnumerable<NewsletterSubscription>> GetAllSubscriptionsAsync()
    {
        return await _context.NewsletterSubscriptions
            .OrderBy(ns => ns.Email)
            .ToListAsync();
    }

    public async Task<NewsletterSubscription?> GetSubscriptionByEmailAsync(string email)
    {
        return await _context.NewsletterSubscriptions
            .FirstOrDefaultAsync(ns => ns.Email.ToLower() == email.ToLower());
    }

    public async Task<bool> IsSubscribedAsync(string email)
    {
        return await _context.NewsletterSubscriptions
            .AnyAsync(ns => ns.Email.ToLower() == email.ToLower() && ns.IsActive);
    }

    public async Task<int> GetSubscriberCountAsync()
    {
        return await _context.NewsletterSubscriptions
            .CountAsync(ns => ns.IsActive);
    }

    public string GenerateUnsubscribeToken(string email)
    {
        var input = $"{email}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid()}";
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hashBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}
