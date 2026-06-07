using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BraysTech.Data;
using BraysTech.Models;
using System.Security.Claims;

namespace BraysTech.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class FloatController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public FloatController(
            AppDbContext db,
            UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var floats = await _db.MpesaFloats
                .Include(f => f.Branch)
                .Include(f => f.Transactions
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(5))
                .Where(f => f.IsActive)
                .ToListAsync();

            // Add today's M-Pesa sales per branch
            var today = DateTime.Today;
            var todayMpesaSales = await _db.PhoneSales
                .Where(s => s.CreatedAt.Date == today &&
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

            ViewBag.TodayMpesaSales = todayMpesaSales;
            ViewBag.TotalFloat =
                floats.Sum(f => f.CurrentBalance);
            ViewBag.LowFloat = floats.Count(f =>
                f.CurrentBalance <= f.LowBalanceAlert);

            return View(floats);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Setup()
        {
            ViewBag.Branches = await _db.Branches
                .Where(b => b.IsActive).ToListAsync();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Setup(
            MpesaFloat model)
        {
            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;
            _db.MpesaFloats.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] =
                $"Float account for " +
                $"{model.TillNumber} created.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Topup(
            int? floatID)
        {
            ViewBag.Floats = await _db.MpesaFloats
                .Include(f => f.Branch)
                .Where(f => f.IsActive).ToListAsync();
            ViewBag.SelectedFloat = floatID;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Topup(
            int floatID, decimal amount,
            string? reference, string? notes)
        {
            var mpesaFloat = await _db.MpesaFloats
                .FindAsync(floatID);
            if (mpesaFloat == null) return NotFound();

            var staffID = User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            var balanceBefore = mpesaFloat.CurrentBalance;
            mpesaFloat.CurrentBalance += amount;
            mpesaFloat.UpdatedAt = DateTime.Now;

            _db.FloatTransactions.Add(
                new FloatTransaction
                {
                    FloatID = floatID,
                    Type = FloatTransactionType.TopUp,
                    Amount = amount,
                    BalanceBefore = balanceBefore,
                    BalanceAfter =
                        mpesaFloat.CurrentBalance,
                    Reference = reference,
                    Notes = notes,
                    StaffID = staffID!,
                    CreatedAt = DateTime.Now
                });

            await _db.SaveChangesAsync();

            TempData["Success"] =
                $"Float topped up by " +
                $"KES {amount:N0}. " +
                $"New balance: " +
                $"KES {mpesaFloat.CurrentBalance:N0}";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Withdrawal(
            int floatID, decimal amount,
            string? reference, string? notes)
        {
            var mpesaFloat = await _db.MpesaFloats
                .FindAsync(floatID);
            if (mpesaFloat == null) return NotFound();

            if (amount > mpesaFloat.CurrentBalance)
            {
                TempData["Error"] =
                    "Withdrawal amount exceeds " +
                    "current float balance.";
                return RedirectToAction("Index");
            }

            var staffID = User
                .FindFirstValue(ClaimTypes.NameIdentifier);
            var balanceBefore = mpesaFloat.CurrentBalance;
            mpesaFloat.CurrentBalance -= amount;
            mpesaFloat.UpdatedAt = DateTime.Now;

            _db.FloatTransactions.Add(
                new FloatTransaction
                {
                    FloatID = floatID,
                    Type = FloatTransactionType
                        .Withdrawal,
                    Amount = amount,
                    BalanceBefore = balanceBefore,
                    BalanceAfter =
                        mpesaFloat.CurrentBalance,
                    Reference = reference,
                    Notes = notes,
                    StaffID = staffID!,
                    CreatedAt = DateTime.Now
                });

            await _db.SaveChangesAsync();

            TempData["Success"] =
                $"Withdrawal of KES {amount:N0} recorded. " +
                $"Remaining balance: " +
                $"KES {mpesaFloat.CurrentBalance:N0}";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> History(
            int floatID)
        {
            var mpesaFloat = await _db.MpesaFloats
                .Include(f => f.Branch)
                .Include(f => f.Transactions
                    .OrderByDescending(t => t.CreatedAt))
                    .ThenInclude(t => t.Staff)
                .FirstOrDefaultAsync(f =>
                    f.FloatID == floatID);

            if (mpesaFloat == null) return NotFound();
            return View(mpesaFloat);
        }
    }
}