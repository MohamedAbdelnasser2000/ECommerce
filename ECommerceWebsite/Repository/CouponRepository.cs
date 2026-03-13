using Microsoft.EntityFrameworkCore;
using ECommerceWebsite.Data;
using ECommerceWebsite.Models;

namespace ECommerceWebsite.Repository;

public class CouponRepository : Repository<Coupon>, ICouponRepository
{
    private readonly ApplicationDbContext _context;

    public CouponRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Coupon?> GetByCodeAsync(string code)
    {
        return await _context.Coupons
            .Include(c => c.Category)
            .Include(c => c.Product)
            .FirstOrDefaultAsync(c => c.Code.ToUpper() == code.ToUpper());
    }

    public async Task<bool> IsCodeUniqueAsync(string code, int? excludeId = null)
    {
        var query = _context.Coupons.Where(c => c.Code.ToUpper() == code.ToUpper());

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return !await query.AnyAsync();
    }

    public async Task<IEnumerable<Coupon>> GetActiveCouponsAsync()
    {
        return await _context.Coupons
            .Include(c => c.Category)
            .Include(c => c.Product)
            .Where(c => c.IsActive &&
                       c.StartDate <= DateTime.Now &&
                       (c.EndDate == null || c.EndDate >= DateTime.Now))
            .ToListAsync();
    }

    public async Task<IEnumerable<Coupon>> GetExpiredCouponsAsync()
    {
        return await _context.Coupons
            .Include(c => c.Category)
            .Include(c => c.Product)
            .Where(c => c.EndDate != null && c.EndDate < DateTime.Now)
            .ToListAsync();
    }

    public async Task<IEnumerable<Coupon>> GetCouponsByCategoryAsync(int categoryId)
    {
        return await _context.Coupons
            .Include(c => c.Category)
            .Where(c => c.CategoryId == categoryId && c.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<Coupon>> GetCouponsByProductAsync(int productId)
    {
        return await _context.Coupons
            .Include(c => c.Product)
            .Where(c => c.ProductId == productId && c.IsActive)
            .ToListAsync();
    }

    public async Task<bool> CanUserUseCouponAsync(string userId, int couponId)
    {
        var coupon = await _context.Coupons.FindAsync(couponId);
        if (coupon == null || !coupon.IsActive) return false;

        // Check if coupon has usage limit
        if (coupon.UsageLimit.HasValue)
        {
            var usageCount = await GetCouponUsageCountAsync(couponId);
            if (usageCount >= coupon.UsageLimit.Value) return false;
        }

        // Check if user has already used this coupon (for single-use coupons)
        var hasUsed = await _context.CouponUsages
            .AnyAsync(cu => cu.UserId == userId && cu.CouponId == couponId);

        return !hasUsed;
    }

    public async Task<int> GetCouponUsageCountAsync(int couponId)
    {
        return await _context.CouponUsages
            .CountAsync(cu => cu.CouponId == couponId);
    }

    public async Task<CouponValidationResult> ValidateCouponAsync(string code, string userId, decimal orderAmount, int? categoryId = null, int? productId = null)
    {
        var result = new CouponValidationResult();

        // Get coupon by code
        var coupon = await GetByCodeAsync(code);
        if (coupon == null)
        {
            result.Message = "Invalid coupon code";
            return result;
        }

        // Check if coupon is active
        if (!coupon.IsActive)
        {
            result.Message = "This coupon is no longer active";
            return result;
        }

        // Check date validity
        if (coupon.StartDate > DateTime.Now)
        {
            result.Message = "This coupon is not yet valid";
            return result;
        }

        if (coupon.EndDate.HasValue && coupon.EndDate < DateTime.Now)
        {
            result.Message = "This coupon has expired";
            return result;
        }

        // Check usage limit
        if (coupon.UsageLimit.HasValue)
        {
            var usageCount = await GetCouponUsageCountAsync(coupon.Id);
            if (usageCount >= coupon.UsageLimit.Value)
            {
                result.Message = "This coupon has reached its usage limit";
                return result;
            }
        }

        // Check if user can use this coupon
        if (!await CanUserUseCouponAsync(userId, coupon.Id))
        {
            result.Message = "You have already used this coupon";
            return result;
        }

        // Check minimum order amount
        if (coupon.MinimumOrderAmount.HasValue && orderAmount < coupon.MinimumOrderAmount.Value)
        {
            result.Message = $"Minimum order amount is ${coupon.MinimumOrderAmount.Value:F2}";
            return result;
        }

        // Check category restriction
        if (coupon.CategoryId.HasValue && categoryId != coupon.CategoryId)
        {
            result.Message = "This coupon is only valid for specific categories";
            return result;
        }

        // Check product restriction
        if (coupon.ProductId.HasValue && productId != coupon.ProductId)
        {
            result.Message = "This coupon is only valid for specific products";
            return result;
        }

        // Calculate discount amount
        decimal discountAmount = 0;
        switch (coupon.Type)
        {
            case CouponType.Percentage:
                discountAmount = orderAmount * (coupon.Value / 100);
                if (coupon.MaximumDiscountAmount.HasValue && discountAmount > coupon.MaximumDiscountAmount.Value)
                {
                    discountAmount = coupon.MaximumDiscountAmount.Value;
                }
                break;

            case CouponType.FixedAmount:
                discountAmount = Math.Min(coupon.Value, orderAmount);
                break;

            case CouponType.FreeShipping:
                // This will be handled in checkout logic
                discountAmount = 0;
                break;

            case CouponType.BOGO:
                // This would need more complex logic based on cart items
                discountAmount = 0;
                break;
        }

        result.IsValid = true;
        result.DiscountAmount = discountAmount;
        result.Coupon = coupon;
        result.Message = "Coupon applied successfully";

        return result;
    }
}
