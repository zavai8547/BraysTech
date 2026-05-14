using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SplashCityCarwash.Data;
using SplashCityCarwash.Models;

namespace SplashCityCarwash.Controllers
{
    [Authorize]
    public class ServicesController : Controller
    {
        private readonly AppDbContext _db;
        public ServicesController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            var services = await _db.ServicePackages
                .OrderBy(s => s.ServiceName)
                .ToListAsync();
            return View(services);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Create() => View();

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create(ServicePackage model)
        {
            ModelState.Remove("TransactionServices");
            if (!ModelState.IsValid) return View(model);

            _db.ServicePackages.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"✅ Service '{model.ServiceName}' added!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int id)
        {
            var service = await _db.ServicePackages.FindAsync(id);
            if (service == null) return NotFound();
            return View(service);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(ServicePackage model)
        {
            ModelState.Remove("TransactionServices");
            if (!ModelState.IsValid) return View(model);

            var service = await _db.ServicePackages.FindAsync(model.ServiceID);
            if (service == null) return NotFound();

            service.ServiceName = model.ServiceName;
            service.Description = model.Description;
            service.Price = model.Price;
            service.EstimatedDuration = model.EstimatedDuration;
            service.IsActive = model.IsActive;

            await _db.SaveChangesAsync();
            TempData["Success"] = "✅ Service updated!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var service = await _db.ServicePackages.FindAsync(id);
            if (service != null)
            {
                service.IsActive = !service.IsActive;
                await _db.SaveChangesAsync();
                TempData["Success"] = $"✅ Service '{service.ServiceName}' " +
                    (service.IsActive ? "activated!" : "deactivated!");
            }
            return RedirectToAction("Index");
        }
    }
}