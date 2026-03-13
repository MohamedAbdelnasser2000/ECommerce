using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerceWebsite.Models;

namespace ECommerceWebsite.Repository
{
    public interface IEmailTemplateRepository : IRepository<EmailTemplate>
    {
        Task<EmailTemplate?> GetByTypeAsync(EmailTemplateType type);
        Task<IEnumerable<EmailTemplate>> GetActiveTemplatesAsync();
        Task<bool> IsTemplateNameUniqueAsync(string name, int? excludeId = null);
    }
}
