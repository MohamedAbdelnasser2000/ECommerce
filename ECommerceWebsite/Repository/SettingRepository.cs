using ECommerceWebsite.Data;
using ECommerceWebsite.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerceWebsite.Repository;

public class SettingRepository : Repository<Setting>, ISettingRepository
{
    private readonly ApplicationDbContext _db;

    public SettingRepository(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<string?> GetValueAsync(string key)
    {
        var setting = await _db.Settings.FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value;
    }

    public async Task SetValueAsync(string key, string value, string? description = null)
    {
        var setting = await _db.Settings.FirstOrDefaultAsync(s => s.Key == key);
        
        if (setting == null)
        {
            setting = new Setting
            {
                Key = key,
                Value = value,
                Description = description,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _db.Settings.Add(setting);
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.Now;
            if (!string.IsNullOrEmpty(description))
            {
                setting.Description = description;
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task<Dictionary<string, string>> GetAllSettingsAsync()
    {
        var settings = await _db.Settings.ToListAsync();
        return settings.ToDictionary(s => s.Key, s => s.Value);
    }
}
