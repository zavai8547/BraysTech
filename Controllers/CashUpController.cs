using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BraysTech.Data;
using BraysTech.Models;
using System.Security.Claims;

namespace BraysTech.Controllers
{
    [Authorize]
    public class CashUpController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public CashUpController(
            AppDbContext db,
            UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> New()
        {
            var currentUserID = User
                .FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _userManager
                .FindByIdAsync(currentUserID!);
            var isAdmin = User.IsInRole("Admin");

            int? branchID = currentUser?.BranchID;

            if (branchID == null && !isAdmin)
            {
                TempData["Error"] =
                    "You are not assigned to a branch. " +
                    "Contact your administrator.";
                return RedirectToAction("Index",
                    "Dashboard");
            }

            ViewBag.BranchID = branchID;
            ViewBag.BranchName = branchID != null
                ? (await _db.Branches.FindAsync(branchID))
                    ?.Name
                : "All Branches";

            if (isAdmin)
            {
                ViewBag.Branches = await _db.Branches
                    .Where(b => b.IsActive).ToListAsync();
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> New(
            decimal cashAmount,
            decimal mpesaFloat,
            int branchID,
            string? notes)
        {
            var staffID = User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            var cashUp = new CashUp
            {
                StaffID = staffID!,
                BranchID = branchID,
                CashAmount = cashAmount,
                MpesaFloat = mpesaFloat,
                ExpectedCash = 0,
                ExpectedMpesa = 0,
                Notes = notes,
                CashUpDate = DateTime.Today,
                CreatedAt = DateTime.Now
            };

            _db.CashUps.Add(cashUp);
            await _db.SaveChangesAsync();

            TempData["Success"] =
                "Cash-up submitted. " +
                $"Cash: KES {cashAmount:N0} | " +
                $"M-Pesa: KES {mpesaFloat:N0}";
            return RedirectToAction("MyHistory");
        }

        public async Task<IActionResult> MyHistory()
        {
            var staffID = User
                .FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("Manager");
            var currentUser = await _userManager
                .FindByIdAsync(staffID!);

            var query = _db.CashUps
                .Include(c => c.Staff)
                .Include(c => c.Branch)
                .AsQueryable();

            if (!isAdmin && !isManager)
                query = query.Where(c =>
                    c.StaffID == staffID);
            else if (isManager &&
                     currentUser?.BranchID != null)
                query = query.Where(c =>
                    c.BranchID == currentUser.BranchID);

            var records = await query
                .OrderByDescending(c => c.CreatedAt)
                .Take(50)
                .ToListAsync();

            return View(records);
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Overview(
            DateTime? date)
        {
            var targetDate = date?.Date ?? DateTime.Today;
            var currentUserID = User
                .FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var currentUser = await _userManager
                .FindByIdAsync(currentUserID!);

            var query = _db.CashUps
                .Include(c => c.Staff)
                .Include(c => c.Branch)
                .Where(c => c.CashUpDate == targetDate);

            if (!isAdmin && currentUser?.BranchID != null)
                query = query.Where(c =>
                    c.BranchID == currentUser.BranchID);

            var cashUps = await query
                .OrderBy(c => c.Branch!.Name)
                .ThenByDescending(c => c.CreatedAt)
                .ToListAsync();

            // M-Pesa sales for the day per branch
            var mpesaSales = await _db.PhoneSales
                .Where(s =>
                    s.CreatedAt.Date == targetDate &&
                    s.PaymentMethod ==
                        SalePaymentMethod.MPesa)
                .GroupBy(s => s.BranchID)
                .Select(g => new
                {
                    BranchID = g.Key,
                    Amount = g.Sum(s => s.TotalAmount),
                    Count = g.Count()
                })
                .ToListAsync();

            var cashSales = await _db.PhoneSales
                .Where(s =>
                    s.CreatedAt.Date == targetDate &&
                    s.PaymentMethod ==
                        SalePaymentMethod.Cash)
                .GroupBy(s => s.BranchID)
                .Select(g => new
                {
                    BranchID = g.Key,
                    Amount = g.Sum(s => s.TotalAmount),
                    Count = g.Count()
                })
                .ToListAsync();

            ViewBag.MpesaSalesByBranch = mpesaSales;
            ViewBag.CashSalesByBranch = cashSales;
            ViewBag.TargetDate = targetDate;
            ViewBag.TotalCashDeclared =
                cashUps.Sum(c => c.CashAmount);
            ViewBag.TotalMpesaDeclared =
                cashUps.Sum(c => c.MpesaFloat);

            return View(cashUps);
        }
    }
}