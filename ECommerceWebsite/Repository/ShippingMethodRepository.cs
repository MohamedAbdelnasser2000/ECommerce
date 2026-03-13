using ECommerceWebsite.Data;
using ECommerceWebsite.Models;
using ECommerceWebsite.Repository;

namespace ECommerceWebsite.Repository;

public class ShippingMethodRepository : Repository<ShippingMethod>, IShippingMethodRepository
{
    private ApplicationDbContext _db;

    public ShippingMethodRepository(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }
}
