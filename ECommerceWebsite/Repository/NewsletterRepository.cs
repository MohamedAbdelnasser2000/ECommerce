using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerceWebsite.Data;
using ECommerceWebsite.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerceWebsite.Repository
{
    public class NewsletterRepository : Repository<NewsletterSubscription>, INewsletterRepository
    {
        private readonly ApplicationDbContext _context;

        public NewsletterRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<NewsletterSubscription>> GetActiveSubscriptionsAsync()
        {
            return await _context.Set<NewsletterSubscription>()
                .Where(n => n.IsActive)
                .OrderByDescending(n => n.SubscribedAt)
                .ToListAsync();
        }

        public async Task<NewsletterSubscription?> GetByEmailAsync(string email)
        {
            return await _context.Set<NewsletterSubscription>()
                .FirstOrDefaultAsync(n => n.Email == email);
        }

        public async Task<bool> IsEmailSubscribedAsync(string email)
        {
            return await _context.Set<NewsletterSubscription>()
                .AnyAsync(n => n.Email == email && n.IsActive);
        }

        public async Task<NewsletterSubscription?> GetByTokenAsync(string token)
        {
            return await _context.Set<NewsletterSubscription>()
                .FirstOrDefaultAsync(n => n.UnsubscribeToken == token);
        }

        public async Task<int> GetActiveSubscriberCountAsync()
        {
            return await _context.Set<NewsletterSubscription>()
                .CountAsync(n => n.IsActive);
        }
    }
}
