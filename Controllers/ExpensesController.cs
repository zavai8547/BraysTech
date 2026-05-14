using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SplashCityCarwash.Data;
using SplashCityCarwash.Models;
using System.Security.Claims;

namespace SplashCityCarwash.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class ExpensesController : Controller
    {
        private readonly AppDbContext _db;
        public ExpensesController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index(string? month)
        {
            var query = _db.Expenses
                .Include(e => e.RecordedBy)
                .AsQueryable();

            if (!string.IsNullOrEmpty(month) &&
                DateTime.TryParse(month + "-01", out var monthDate))
            {
                query = query.Where(e =>
                    e.ExpenseDate.Year == monthDate.Year &&
                    e.ExpenseDate.Month == monthDate.Month);
            }

            var expenses = await query
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync();

            ViewBag.TotalExpenses = expenses.Sum(e => e.Amount);
            ViewBag.Month = month;
            return View(expenses);
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Expense model)
        {
            ModelState.Remove("RecordedBy");
            ModelState.Remove("RecordedByID");

            if (!ModelState.IsValid) return View(model);

            model.RecordedByID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            model.CreatedAt = DateTime.Now;

            _db.Expenses.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "✅ Expense recorded!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var expense = await _db.Expenses.FindAsync(id);
            if (expense != null)
            {
                _db.Expenses.Remove(expense);
                await _db.SaveChangesAsync();
                TempData["Success"] = "✅ Expense deleted.";
            }
            return RedirectToAction("Index");
        }
    }
}