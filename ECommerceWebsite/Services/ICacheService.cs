using System;
using System.Threading.Tasks;

namespace ECommerceWebsite.Services
{
    public interface ICacheService
    {
        Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null);
        Task RemoveAsync(string key);
    }
}