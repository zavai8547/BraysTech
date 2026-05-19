using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BraysTech.Data;
using BraysTech.Models;
using System.Security.Claims;

namespace BraysTech.Controllers
{
    [Authorize]
    public class ReceiptsController : Controller
    {
        private readonly AppDbContext _db;

        public ReceiptsController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(
            string? search, int? branchID,
            DateTime? from, DateTime? to)
        {
            var currentUserID = User
                .FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("Manager");

            var query = _db.PhoneSales
                .Include(s => s.Staff)
                .Include(s => s.Branch)
                .Include(s => s.Items)
                .AsQueryable();

            // Salesperson sees only their own
            if (!isAdmin && !isManager)
                query = query.Where(s =>
                    s.StaffID == currentUserID);

            if (branchID.HasValue)
                query = query.Where(s =>
                    s.BranchID == branchID);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(s =>
                    (s.CustomerName != null &&
                     s.CustomerName.Contains(search)) ||
                    (s.CustomerPhone != null &&
                     s.CustomerPhone.Contains(search)) ||
                    s.Items.Any(i =>
                        i.IMEI.Contains(search) ||
                        i.PhoneName.Contains(search)));

            if (from.HasValue)
                query = query.Where(s =>
                    s.CreatedAt.Date >= from.Value.Date);

            if (to.HasValue)
                query = query.Where(s =>
                    s.CreatedAt.Date <= to.Value.Date);

            var sales = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            ViewBag.Branches = await _db.Branches
                .Where(b => b.IsActive).ToListAsync();
            ViewBag.Search = search;
            ViewBag.SelectedBranch = branchID;
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");
            ViewBag.IsAdmin = isAdmin;
            ViewBag.IsManager = isManager;
            ViewBag.TotalReceipts = sales.Count;

            return View(sales);
        }

        public async Task<IActionResult> View(int id)
        {
            var sale = await _db.PhoneSales
                .Include(s => s.Staff)
                .Include(s => s.Branch)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Phone)
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.SaleID == id);

            if (sale == null) return NotFound();

            // Get business settings
            var settings = await _db.Settings
                .ToDictionaryAsync(s => s.SettingKey,
                                   s => s.SettingValue);

            ViewBag.BusinessName = settings
                .GetValueOrDefault("business_name",
                    "Brays Technologies Systems");
            ViewBag.BusinessPhone = settings
                .GetValueOrDefault("business_phone", "");
            ViewBag.BusinessEmail = settings
                .GetValueOrDefault("business_email", "");
            ViewBag.ReceiptFooter = settings
                .GetValueOrDefault("receipt_footer",
                    "Thank you for choosing Brays Technologies!");

            return View(sale);
        }
    }
}