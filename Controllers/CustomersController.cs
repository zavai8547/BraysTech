using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BraysTech.Data;
using BraysTech.Models;

namespace BraysTech.Controllers
{
    [Authorize]
    public class CustomersController : Controller
    {
        private readonly AppDbContext _db;
        public CustomersController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index(string? search)
        {
            var query = _db.Customers.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(c =>
                    c.FullName.Contains(search) ||
                    c.Phone.Contains(search) ||
                    (c.Email != null && c.Email.Contains(search)));

            var customers = await query
                .OrderByDescending(c => c.TotalSpent)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.TotalCustomers = await _db.Customers.CountAsync();
            ViewBag.TotalSpent = await _db.Customers
                .SumAsync(c => (decimal?)c.TotalSpent) ?? 0;

            return View(customers);
        }

        public async Task<IActionResult> Details(int id)
        {
            var customer = await _db.Customers
                .Include(c => c.Sales)
                    .ThenInclude(s => s.Items)
                .Include(c => c.Sales)
                    .ThenInclude(s => s.Branch)
                .Include(c => c.Sales)
                    .ThenInclude(s => s.Staff)
                .FirstOrDefaultAsync(c => c.CustomerID == id);

            if (customer == null) return NotFound();

            return View(customer);
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Customer model)
        {
            // Check duplicate phone
            var exists = await _db.Customers
                .AnyAsync(c => c.Phone == model.Phone);
            if (exists)
            {
                TempData["Error"] =
                    "❌ A customer with this phone number already exists.";
                return View(model);
            }

            model.CreatedAt = DateTime.Now;
            _db.Customers.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] =
                $"✅ Customer {model.FullName} added!";
            return RedirectToAction("Index");
        }

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
            var existing = await _db.Customers
                .FirstOrDefaultAsync(c =>
                    c.Phone == model.Phone &&
                    c.CustomerID != model.CustomerID);
            if (existing != null)
            {
                TempData["Error"] =
                    "❌ Another customer already has this phone number.";
                return View(model);
            }

            _db.Customers.Update(model);
            await _db.SaveChangesAsync();

            TempData["Success"] =
                $"✅ {model.FullName} updated!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _db.Customers
                .Include(c => c.Sales)
                .FirstOrDefaultAsync(c => c.CustomerID == id);

            if (customer == null) return NotFound();

            if (customer.Sales.Any())
            {
                TempData["Error"] =
                    "❌ Cannot delete — customer has sales history. " +
                    "This protects your records.";
                return RedirectToAction("Index");
            }

            _db.Customers.Remove(customer);
            await _db.SaveChangesAsync();

            TempData["Success"] =
                $"✅ Customer {customer.FullName} deleted.";
            return RedirectToAction("Index");
        }

        // AJAX search for New Sale page
        [HttpGet]
        public async Task<IActionResult> Search(string q)
        {
            if (string.IsNullOrEmpty(q))
                return Json(new List<object>());

            var results = await _db.Customers
                .Where(c => c.FullName.Contains(q) ||
                            c.Phone.Contains(q))
                .Take(8)
                .Select(c => new
                {
                    c.CustomerID,
                    c.FullName,
                    c.Phone,
                    c.TotalPurchases,
                    c.TotalSpent
                })
                .ToListAsync();

            return Json(results);
        }
    }
}