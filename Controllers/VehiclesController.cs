using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SplashCityCarwash.Data;
using SplashCityCarwash.Models;

namespace SplashCityCarwash.Controllers
{
    [Authorize]
    public class VehiclesController : Controller
    {
        private readonly AppDbContext _db;

        public VehiclesController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var query = _db.Vehicles
                .Include(v => v.Customer)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(v =>
                    v.LicensePlate.Contains(search)
                    || v.Customer.FullName.Contains(search)
                    || v.Customer.Phone.Contains(search));

            var vehicles = await query
                .OrderByDescending(v => v.VehicleID)
                .ToListAsync();

            ViewBag.Search = search;
            return View(vehicles);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId)
        {
            await LoadCustomersDropdown(customerId);
            return View(new Vehicle
            {
                CustomerID = customerId ?? 0
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            int CustomerID, string LicensePlate,
            VehicleType VehicleType, string? Make,
            string? CarModel, string? Color)
        {
            if (string.IsNullOrWhiteSpace(LicensePlate))
            {
                TempData["Error"] = "❌ License plate is required.";
                await LoadCustomersDropdown(CustomerID);
                return View(new Vehicle { CustomerID = CustomerID });
            }

            if (CustomerID == 0)
            {
                TempData["Error"] = "❌ Please select a customer.";
                await LoadCustomersDropdown(CustomerID);
                return View(new Vehicle { CustomerID = CustomerID });
            }

            var plate = LicensePlate.Trim().ToUpper();

            if (await _db.Vehicles.AnyAsync(v => v.LicensePlate == plate))
            {
                TempData["Error"] = "❌ A vehicle with this plate already exists.";
                await LoadCustomersDropdown(CustomerID);
                return View(new Vehicle
                {
                    CustomerID = CustomerID,
                    LicensePlate = plate,
                    VehicleType = VehicleType,
                    Make = Make,
                    CarModel = CarModel,
                    Color = Color
                });
            }

            var vehicle = new Vehicle
            {
                CustomerID = CustomerID,
                LicensePlate = plate,
                VehicleType = VehicleType,
                Make = Make,
                CarModel = CarModel,
                Color = Color
            };

            _db.Vehicles.Add(vehicle);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"✅ Vehicle {plate} added successfully!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var vehicle = await _db.Vehicles
                .Include(v => v.Customer)
                .FirstOrDefaultAsync(v => v.VehicleID == id);

            if (vehicle == null) return NotFound();
            await LoadCustomersDropdown(vehicle.CustomerID);
            return View(vehicle);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(
            int VehicleID, int CustomerID,
            string LicensePlate, VehicleType VehicleType,
            string? Make, string? CarModel, string? Color)
        {
            var vehicle = await _db.Vehicles.FindAsync(VehicleID);
            if (vehicle == null) return NotFound();

            vehicle.CustomerID = CustomerID;
            vehicle.LicensePlate = LicensePlate.Trim().ToUpper();
            vehicle.VehicleType = VehicleType;
            vehicle.Make = Make;
            vehicle.CarModel = CarModel;
            vehicle.Color = Color;

            await _db.SaveChangesAsync();
            TempData["Success"] = "✅ Vehicle updated successfully!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Details(int id)
        {
            var vehicle = await _db.Vehicles
                .Include(v => v.Customer)
                .Include(v => v.Transactions)
                    .ThenInclude(t => t.TransactionServices)
                        .ThenInclude(ts => ts.Service)
                .FirstOrDefaultAsync(v => v.VehicleID == id);

            if (vehicle == null) return NotFound();
            return View(vehicle);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var vehicle = await _db.Vehicles.FindAsync(id);
            if (vehicle != null)
            {
                _db.Vehicles.Remove(vehicle);
                await _db.SaveChangesAsync();
                TempData["Success"] = "✅ Vehicle deleted.";
            }
            return RedirectToAction("Index");
        }

        private async Task LoadCustomersDropdown(int? selectedId = null)
        {
            var customers = await _db.Customers
                .OrderBy(c => c.FullName)
                .ToListAsync();

            ViewBag.Customers = new SelectList(
                customers, "CustomerID", "FullName", selectedId);
        }
    }
}