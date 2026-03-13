using ECommerceWebsite.Models;

namespace ECommerceWebsite.Repository;

public interface ISettingRepository : IRepository<Setting>
{
    Task<string?> GetValueAsync(string key);
    Task SetValueAsync(string key, string value, string? description = null);
    Task<Dictionary<string, string>> GetAllSettingsAsync();
}
