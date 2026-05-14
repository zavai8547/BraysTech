using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SplashCityCarwash.Data;
using SplashCityCarwash.Models;

namespace SplashCityCarwash.Controllers
{
    [Authorize]
    public class CustomersController : Controller
    {
        private readonly AppDbContext _db;

        public CustomersController(AppDbContext db)
        {
            _db = db;
        }

        // ── LIST ALL CUSTOMERS ─────────────────────────
        public async Task<IActionResult> Index(string? search)
        {
            var query = _db.Customers.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(c => c.FullName.Contains(search)
                                      || c.Phone.Contains(search)
                                      || (c.Email != null && c.Email.Contains(search)));

            var customers = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.Search = search;
            return View(customers);
        }

        // ── ADD CUSTOMER ───────────────────────────────
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Customer model)
        {
            if (!ModelState.IsValid) return View(model);

            // Check duplicate phone
            if (await _db.Customers.AnyAsync(c => c.Phone == model.Phone))
            {
                ModelState.AddModelError("Phone", "A customer with this phone number already exists.");
                return View(model);
            }

            _db.Customers.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Customer {model.FullName} added successfully!";
            return RedirectToAction("Index");
        }

        // ── EDIT CUSTOMER ──────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _db.Customers.FindAsync(id);
            if (customer == null) return NotFound();
            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Customer model)
        {
            if (!ModelState.IsValid) return View(model);

            _db.Customers.Update(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Customer updated successfully!";
            return RedirectToAction("Index");
        }

        // ── CUSTOMER DETAILS + HISTORY ─────────────────
        public async Task<IActionResult> Details(int id)
        {
            var customer = await _db.Customers
                .Include(c => c.Vehicles)
                .Include(c => c.Transactions)
                    .ThenInclude(t => t.TransactionServices)
                        .ThenInclude(ts => ts.Service)
                .Include(c => c.Transactions)
                    .ThenInclude(t => t.Vehicle)
                .FirstOrDefaultAsync(c => c.CustomerID == id);

            if (customer == null) return NotFound();
            return View(customer);
        }

        // ── DELETE CUSTOMER ────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _db.Customers.FindAsync(id);
            if (customer != null)
            {
                _db.Customers.Remove(customer);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Customer deleted.";
            }
            return RedirectToAction("Index");
        }
    }
}