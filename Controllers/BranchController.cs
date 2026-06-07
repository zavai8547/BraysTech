using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BraysTech.Data;
using BraysTech.Models;

namespace BraysTech.Controllers
{
    [Authorize(Roles = "Admin,Manager")]  // CHANGED: Restrict to Admin and Manager only
    public class BranchController : Controller
    {
        private readonly AppDbContext _db;
        public BranchController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            var branches = await _db.Branches
                .Include(b => b.Staff)
                .Include(b => b.Stock)
                .Include(b => b.Sales)
                .OrderBy(b => b.Name)
                .ToListAsync();
            return View(branches);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Branch model)
        {
            if (!ModelState.IsValid) return View(model);

            model.CreatedAt = DateTime.Now;
            model.IsActive = true;
            _db.Branches.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] =
                $"✅ Branch '{model.Name}' created successfully!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var branch = await _db.Branches.FindAsync(id);
            if (branch == null) return NotFound();
            return View(branch);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(
            int BranchID,
            string Name,
            string Location,
            string? Phone,
            string? Notes)
        {
            // Don't accept CreatedAt or IsActive as parameters - security risk!

            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Location))
            {
                TempData["Error"] = "Name and Location are required fields.";
                return RedirectToAction("Edit", new { id = BranchID });
            }

            var branch = await _db.Branches.FindAsync(BranchID);
            if (branch == null) return NotFound();

            // Only update the allowed fields
            branch.Name = Name;
            branch.Location = Location;
            branch.Phone = Phone;
            branch.Notes = Notes;

            // CreatedAt remains unchanged
            // IsActive remains unchanged (use ToggleActive for that)

            await _db.SaveChangesAsync();

            TempData["Success"] = $"✅ Branch '{branch.Name}' updated successfully!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Details(int id)
        {
            var branch = await _db.Branches
                .Include(b => b.Staff)
                .Include(b => b.Stock)
                .Include(b => b.Sales)
                    .ThenInclude(s => s.Items)
                .FirstOrDefaultAsync(b => b.BranchID == id);

            if (branch == null) return NotFound();

            var today = DateTime.Today;
            var firstOfMonth = new DateTime(today.Year, today.Month, 1);

            ViewBag.SalesToday = branch.Sales
                .Where(s => s.CreatedAt.Date == today)
                .Sum(s => s.TotalAmount);

            ViewBag.SalesMonth = branch.Sales
                .Where(s => s.CreatedAt >= firstOfMonth)
                .Sum(s => s.TotalAmount);

            ViewBag.PhonesInStock = branch.Stock
                .Count(s => s.Status == PhoneStatus.InStock);

            ViewBag.RecentSales = branch.Sales
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .ToList();

            return View(branch);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var branch = await _db.Branches.FindAsync(id);
            if (branch == null) return NotFound();

            branch.IsActive = !branch.IsActive;
            await _db.SaveChangesAsync();

            TempData["Success"] = branch.IsActive
                ? $"✅ Branch '{branch.Name}' activated!"
                : $"⚠️ Branch '{branch.Name}' deactivated!";

            return RedirectToAction("Index");
        }

        // Helper method to check if branch exists (optional, for future use)
        private async Task<bool> BranchExistsAsync(int id)
        {
            return await _db.Branches.AnyAsync(b => b.BranchID == id);
        }
    }
}