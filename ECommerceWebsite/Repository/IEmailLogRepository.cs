using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerceWebsite.Models;

namespace ECommerceWebsite.Repository
{
    public interface IEmailLogRepository : IRepository<EmailLog>
    {
        Task<IEnumerable<EmailLog>> GetByEmailAsync(string email);
        Task<IEnumerable<EmailLog>> GetByStatusAsync(EmailStatus status);
        Task<IEnumerable<EmailLog>> GetRecentLogsAsync(int count = 100);
        Task<int> GetSentCountAsync(System.DateTime? fromDate = null, System.DateTime? toDate = null);
        Task<int> GetFailedCountAsync(System.DateTime? fromDate = null, System.DateTime? toDate = null);
    }
}
