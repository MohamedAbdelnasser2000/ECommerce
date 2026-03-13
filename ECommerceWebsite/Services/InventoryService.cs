using ECommerceWebsite.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerceWebsite.Services;

public interface IInventoryService
{
    Task<bool> TryReserveStockAsync(int productId, int quantity, CancellationToken cancellationToken = default);
    Task<bool> ReserveStocksAsync(IEnumerable<(int productId, int quantity)> items, CancellationToken cancellationToken = default);
}

public class InventoryService : IInventoryService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(ApplicationDbContext db, ILogger<InventoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<bool> TryReserveStockAsync(int productId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0) return true;

        var rows = await _db.Database.ExecuteSqlRawAsync(
            "UPDATE Products SET StockQuantity = StockQuantity - {1} WHERE Id = {0} AND StockQuantity >= {1};",
            new object[] { productId, quantity }, cancellationToken);
        return rows > 0;
    }

    public async Task<bool> ReserveStocksAsync(IEnumerable<(int productId, int quantity)> items, CancellationToken cancellationToken = default)
    {
        using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var (productId, quantity) in items)
            {
                var ok = await TryReserveStockAsync(productId, quantity, cancellationToken);
                if (!ok)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return false;
                }
            }

            await tx.CommitAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reserve stocks");
            try { await tx.RollbackAsync(cancellationToken); } catch { }
            return false;
        }
    }
}


