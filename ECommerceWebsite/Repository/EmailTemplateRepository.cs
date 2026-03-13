using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerceWebsite.Data;
using ECommerceWebsite.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerceWebsite.Repository
{
    public class EmailTemplateRepository : Repository<EmailTemplate>, IEmailTemplateRepository
    {
        private readonly ApplicationDbContext _context;

        public EmailTemplateRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<EmailTemplate>> GetActiveTemplatesAsync()
        {
            return await _context.Set<EmailTemplate>()
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<EmailTemplate?> GetByTypeAsync(EmailTemplateType type)
        {
            return await _context.Set<EmailTemplate>()
                .FirstOrDefaultAsync(t => t.Type == type && t.IsActive);
        }

        public async Task<bool> IsTemplateNameUniqueAsync(string name, int? excludeId = null)
        {
            var query = _context.Set<EmailTemplate>().AsQueryable();
            if (excludeId.HasValue)
            {
                query = query.Where(t => t.Id != excludeId.Value);
            }
            return !await query.AnyAsync(t => t.Name == name);
        }
    }
}
