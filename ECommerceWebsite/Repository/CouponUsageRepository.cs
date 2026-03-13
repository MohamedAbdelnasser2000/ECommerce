using Microsoft.EntityFrameworkCore;
using ECommerceWebsite.Data;
using ECommerceWebsite.Models;

namespace ECommerceWebsite.Repository;

public class CouponUsageRepository : Repository<CouponUsage>, ICouponUsageRepository
{
    private readonly ApplicationDbContext _context;

    public CouponUsageRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CouponUsage>> GetByUserIdAsync(string userId)
    {
        return await _context.CouponUsages
            .Include(cu => cu.Coupon)
            .Include(cu => cu.Order)
            .Where(cu => cu.UserId == userId)
            .OrderByDescending(cu => cu.UsedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<CouponUsage>> GetByCouponIdAsync(int couponId)
    {
        return await _context.CouponUsages
            .Include(cu => cu.User)
            .Include(cu => cu.Order)
            .Where(cu => cu.CouponId == couponId)
            .OrderByDescending(cu => cu.UsedAt)
            .ToListAsync();
    }

    public async Task<bool> HasUserUsedCouponAsync(string userId, int couponId)
    {
        return await _context.CouponUsages
            .AnyAsync(cu => cu.UserId == userId && cu.CouponId == couponId);
    }

    public async Task<decimal> GetTotalDiscountByUserAsync(string userId)
    {
        return await _context.CouponUsages
            .Where(cu => cu.UserId == userId)
            .SumAsync(cu => cu.DiscountAmount);
    }

    public async Task<decimal> GetTotalDiscountByCouponAsync(int couponId)
    {
        return await _context.CouponUsages
            .Where(cu => cu.CouponId == couponId)
            .SumAsync(cu => cu.DiscountAmount);
    }
}
