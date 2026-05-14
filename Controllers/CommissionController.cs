using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SplashCityCarwash.Data;
using SplashCityCarwash.Models;
using System.Security.Claims;

namespace SplashCityCarwash.Controllers
{
    [Authorize]
    public class CommissionController : Controller
    {
        private readonly AppDbContext _db;
        private const decimal COMMISSION_RATE = 0.30m; // 30%

        public CommissionController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index(string? period = "month")
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isManager = User.IsInRole("Admin") || User.IsInRole("Manager");

            var today = DateTime.Today;
            DateTime startDate = period switch
            {
                "today" => today,
                "week" => today.AddDays(-(int)today.DayOfWeek),
                "month" => new DateTime(today.Year, today.Month, 1),
                _ => new DateTime(today.Year, today.Month, 1)
            };

            DateTime endDate = period switch
            {
                "today" => today,
                "week" => today.AddDays(-(int)today.DayOfWeek).AddDays(6),
                "month" => new DateTime(today.Year, today.Month,
                           DateTime.DaysInMonth(today.Year, today.Month)),
                _ => new DateTime(today.Year, today.Month,
                           DateTime.DaysInMonth(today.Year, today.Month))
            };

            // Get all wash jobs in the period
            var washersQuery = _db.TransactionWashers
                .Include(tw => tw.Transaction)
                    .ThenInclude(t => t.Vehicle)
                .Include(tw => tw.Transaction)
                    .ThenInclude(t => t.TransactionServices)
                        .ThenInclude(ts => ts.Service)
                .Include(tw => tw.Washer)
                .Where(tw =>
                    tw.Transaction.CreatedAt.Date >= startDate &&
                    tw.Transaction.CreatedAt.Date <= endDate &&
                    (tw.Transaction.Status == WashStatus.Completed ||
                     tw.Transaction.Status == WashStatus.Paid));

            // If washer — only show their own
            if (!isManager)
                washersQuery = washersQuery
                    .Where(tw => tw.WasherID == currentUserId);

            var records = await washersQuery
                .OrderByDescending(tw => tw.Transaction.CreatedAt)
                .ToListAsync();

            // Group by washer for summary
            var summary = records
                .GroupBy(tw => new { tw.WasherID, tw.Washer.FullName })
                .Select(g => new WasherCommissionSummary
                {
                    WasherID = g.Key.WasherID,
                    WasherName = g.Key.FullName,
                    TotalWashes = g.Count(),
                    TotalWashValue = g.Sum(tw => tw.Transaction.TotalAmount),
                    Commission = g.Sum(tw => tw.Transaction.TotalAmount) * COMMISSION_RATE
                })
                .OrderByDescending(x => x.Commission)
                .ToList();

            ViewBag.Period = period;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.IsManager = isManager;
            ViewBag.CommissionRate = COMMISSION_RATE * 100;
            ViewBag.Records = records;
            ViewBag.CurrentUserID = currentUserId;

            return View(summary);
        }
    }
}