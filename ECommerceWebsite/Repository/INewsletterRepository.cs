using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerceWebsite.Models;

namespace ECommerceWebsite.Repository
{
    public interface INewsletterRepository : IRepository<NewsletterSubscription>
    {
        Task<NewsletterSubscription?> GetByEmailAsync(string email);
        Task<IEnumerable<NewsletterSubscription>> GetActiveSubscriptionsAsync();
        Task<bool> IsEmailSubscribedAsync(string email);
        Task<NewsletterSubscription?> GetByTokenAsync(string token);
        Task<int> GetActiveSubscriberCountAsync();
    }
}
