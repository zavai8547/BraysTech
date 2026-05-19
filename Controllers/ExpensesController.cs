using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BraysTech.Data;
using BraysTech.Models;

namespace BraysTech.Controllers
{
    [Authorize]
    public class ExpensesController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public ExpensesController(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var expenses = await _db.Expenses
                .Include(e => e.Branch)
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync();

            ViewBag.Branches = await _db.Branches.Where(b => b.IsActive).ToListAsync();
            return View(expenses);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string category, string description, decimal amount,
            DateTime expenseDate, int? branchID, string? notes)
        {
            var user = await _userManager.GetUserAsync(User);

            var expense = new Expense
            {
                Category = category,
                Description = description,
                Amount = amount,
                ExpenseDate = expenseDate,
                BranchID = branchID,
                Notes = notes,
                RecordedBy = user?.FullName ?? User.Identity?.Name,
                RecordedByID = user?.Id,
                CreatedAt = DateTime.Now
            };

            _db.Expenses.Add(expense);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Expense recorded successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var expense = await _db.Expenses.FindAsync(id);
            if (expense != null)
            {
                _db.Expenses.Remove(expense);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Expense deleted successfully.";
            }
            return RedirectToAction("Index");
        }
    }
}