using System.Linq.Expressions;

namespace ECommerceWebsite.Repository;

public interface IRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, 
        string? includeProperties = null);
    
    Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> filter, 
        string? includeProperties = null);
    
    Task<T?> GetByIdAsync(int id);
    
    Task AddAsync(T entity);
    
    void Update(T entity);
    
    void Remove(T entity);
    
    void RemoveRange(IEnumerable<T> entities);
    
    Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);
    
    Task<bool> AnyAsync(Expression<Func<T, bool>> filter);
    
    Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize, 
        Expression<Func<T, bool>>? filter = null, 
        string? includeProperties = null,
        Expression<Func<T, object>>? orderBy = null,
        bool isDescending = false);
}
