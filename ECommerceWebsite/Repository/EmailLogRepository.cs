using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerceWebsite.Data;
using ECommerceWebsite.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerceWebsite.Repository
{
    public class EmailLogRepository : Repository<EmailLog>, IEmailLogRepository
    {
        private readonly ApplicationDbContext _context;

        public EmailLogRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<EmailLog>> GetByEmailAsync(string email)
        {
            return await _context.Set<EmailLog>()
                .Where(l => l.ToEmail == email)
                .OrderByDescending(l => l.SentAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<EmailLog>> GetByStatusAsync(EmailStatus status)
        {
            return await _context.Set<EmailLog>()
                .Where(l => l.Status == status)
                .OrderByDescending(l => l.SentAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<EmailLog>> GetRecentLogsAsync(int count = 100)
        {
            return await _context.Set<EmailLog>()
                .OrderByDescending(l => l.SentAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> GetSentCountAsync(System.DateTime? fromDate = null, System.DateTime? toDate = null)
        {
            var query = _context.Set<EmailLog>().AsQueryable();
            if (fromDate.HasValue) query = query.Where(l => l.SentAt >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(l => l.SentAt <= toDate.Value);
            return await query.CountAsync(l => l.Status == EmailStatus.Sent);
        }

        public async Task<int> GetFailedCountAsync(System.DateTime? fromDate = null, System.DateTime? toDate = null)
        {
            var query = _context.Set<EmailLog>().AsQueryable();
            if (fromDate.HasValue) query = query.Where(l => l.SentAt >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(l => l.SentAt <= toDate.Value);
            return await query.CountAsync(l => l.Status == EmailStatus.Failed || l.Status == EmailStatus.Bounced);
        }
    }
}
