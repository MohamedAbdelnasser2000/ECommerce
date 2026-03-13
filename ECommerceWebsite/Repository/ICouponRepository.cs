using ECommerceWebsite.Models;

namespace ECommerceWebsite.Repository;

public interface ICouponRepository : IRepository<Coupon>
{
    Task<Coupon?> GetByCodeAsync(string code);
    Task<bool> IsCodeUniqueAsync(string code, int? excludeId = null);
    Task<IEnumerable<Coupon>> GetActiveCouponsAsync();
    Task<IEnumerable<Coupon>> GetExpiredCouponsAsync();
    Task<IEnumerable<Coupon>> GetCouponsByCategoryAsync(int categoryId);
    Task<IEnumerable<Coupon>> GetCouponsByProductAsync(int productId);
    Task<bool> CanUserUseCouponAsync(string userId, int couponId);
    Task<int> GetCouponUsageCountAsync(int couponId);
    Task<CouponValidationResult> ValidateCouponAsync(string code, string userId, decimal orderAmount, int? categoryId = null, int? productId = null);
}

public interface ICouponUsageRepository : IRepository<CouponUsage>
{
    Task<IEnumerable<CouponUsage>> GetByUserIdAsync(string userId);
    Task<IEnumerable<CouponUsage>> GetByCouponIdAsync(int couponId);
    Task<bool> HasUserUsedCouponAsync(string userId, int couponId);
    Task<decimal> GetTotalDiscountByUserAsync(string userId);
    Task<decimal> GetTotalDiscountByCouponAsync(int couponId);
}
