using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BraysTech.Data;
using BraysTech.Models;

namespace BraysTech.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AuditController : Controller
    {
        private readonly AppDbContext _db;

        public AuditController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(
            string? userID, string? module,
            string? action, DateTime? from,
            DateTime? to, int? branchID)
        {
            var query = _db.AuditLogs
                .AsQueryable();

            if (!string.IsNullOrEmpty(userID))
                query = query.Where(l =>
                    l.UserID == userID);

            if (!string.IsNullOrEmpty(module))
                query = query.Where(l =>
                    l.Module == module);

            if (!string.IsNullOrEmpty(action) &&
                Enum.TryParse<AuditAction>(
                    action, out var parsed))
                query = query.Where(l =>
                    l.Action == parsed);

            if (branchID.HasValue)
                query = query.Where(l =>
                    l.BranchID == branchID);

            if (from.HasValue)
                query = query.Where(l =>
                    l.CreatedAt.Date >= from.Value.Date);

            if (to.HasValue)
                query = query.Where(l =>
                    l.CreatedAt.Date <= to.Value.Date);

            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Take(500)
                .ToListAsync();

            ViewBag.Branches = await _db.Branches
                .Where(b => b.IsActive).ToListAsync();
            ViewBag.Modules = new[]
            {
                "Sales", "Inventory", "Staff",
                "Auth", "Settings"
            };
            ViewBag.Actions = Enum.GetNames(
                typeof(AuditAction));

            ViewBag.SelectedUser = userID;
            ViewBag.SelectedModule = module;
            ViewBag.SelectedAction = action;
            ViewBag.SelectedBranch = branchID;
            ViewBag.From =
                from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");
            ViewBag.TotalLogs = logs.Count;

            return View(logs);
        }
    }
}