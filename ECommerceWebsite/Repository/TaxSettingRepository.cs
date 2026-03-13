using ECommerceWebsite.Data;
using ECommerceWebsite.Models;
using ECommerceWebsite.Repository;

namespace ECommerceWebsite.Repository;

public class TaxSettingRepository : Repository<TaxSetting>, ITaxSettingRepository
{
    private ApplicationDbContext _db;

    public TaxSettingRepository(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }
}
