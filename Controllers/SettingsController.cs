using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BraysTech.Data;
using BraysTech.Models;

namespace BraysTech.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public SettingsController(AppDbContext db,
            UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _db.Settings
                .ToDictionaryAsync(s => s.SettingKey,
                                   s => s.SettingValue);
            return View(settings);
        }

        [HttpPost]
        public async Task<IActionResult> Save(
            IFormCollection form)
        {
            foreach (var key in form.Keys)
            {
                if (key == "__RequestVerificationToken")
                    continue;

                var value = form[key].ToString();
                var setting = await _db.Settings
                    .FirstOrDefaultAsync(s => s.SettingKey == key);

                if (setting != null)
                {
                    setting.SettingValue = value;
                    setting.UpdatedAt = DateTime.Now;
                }
                else
                {
                    _db.Settings.Add(new Setting
                    {
                        SettingKey = key,
                        SettingValue = value,
                        UpdatedAt = DateTime.Now
                    });
                }
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "✅ Settings saved!";
            return RedirectToAction("Index");
        }

        // Seed default settings if empty
        public async Task<IActionResult> SeedDefaults()
        {
            var defaults = new Dictionary<string, string>
            {
                { "business_name", "Brays Technologies Systems" },
                { "business_phone", "+254 758 265 242" },
                { "business_email", "zavairodney@gmail.com" },
                { "receipt_footer",
                  "Thank you for choosing Brays Technologies!" },
                { "currency", "KES" },
                { "theme", "light" },
                { "low_stock_threshold", "5" },
            };

            foreach (var kv in defaults)
            {
                var exists = await _db.Settings
                    .AnyAsync(s => s.SettingKey == kv.Key);
                if (!exists)
                {
                    _db.Settings.Add(new Setting
                    {
                        SettingKey = kv.Key,
                        SettingValue = kv.Value,
                        UpdatedAt = DateTime.Now
                    });
                }
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "✅ Default settings loaded!";
            return RedirectToAction("Index");
        }
    }
}