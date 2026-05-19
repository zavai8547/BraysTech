using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BraysTech.Data;
using BraysTech.Models;

namespace BraysTech.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class PerformanceController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public PerformanceController(AppDbContext db,
            UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(
            int? branchID, int? month, int? year)
        {
            var today = DateTime.Today;
            var selectedMonth = month ?? today.Month;
            var selectedYear = year ?? today.Year;
            var firstOfMonth = new DateTime(
                selectedYear, selectedMonth, 1);
            var lastOfMonth = firstOfMonth
                .AddMonths(1).AddDays(-1);

            // Get all staff
            var allStaff = await _userManager.Users
                .Include(u => u.Branch)
                .Where(u => u.IsActive)
                .ToListAsync();

            if (branchID.HasValue)
                allStaff = allStaff
                    .Where(u => u.BranchID == branchID)
                    .ToList();

            // Build performance per staff
            var performances = new List<StaffPerformance>();

            foreach (var staff in allStaff)
            {
                var salesThisMonth = await _db.PhoneSales
                    .Include(s => s.Items)
                    .Where(s => s.StaffID == staff.Id &&
                                s.CreatedAt >= firstOfMonth &&
                                s.CreatedAt <= lastOfMonth)
                    .ToListAsync();

                var salesToday = await _db.PhoneSales
                    .Where(s => s.StaffID == staff.Id &&
                                s.CreatedAt.Date == today)
                    .ToListAsync();

                var roles = await _userManager
                    .GetRolesAsync(staff);

                performances.Add(new StaffPerformance
                {
                    StaffID = staff.Id,
                    StaffName = staff.FullName,
                    Email = staff.Email ?? "",
                    Phone = staff.Phone,
                    Role = roles.FirstOrDefault() ?? "—",
                    BranchName = staff.Branch?.Name ?? "No Branch",
                    BranchID = staff.BranchID,

                    // This month
                    SalesCountMonth = salesThisMonth.Count,
                    RevenueMonth = salesThisMonth
                        .Sum(s => s.TotalAmount),
                    ProfitMonth = salesThisMonth
                        .Sum(s => s.TotalProfit),
                    DevicesSoldMonth = salesThisMonth
                        .Sum(s => s.Items.Count),

                    // Today
                    SalesToday = salesToday.Count,
                    RevenueToday = salesToday
                        .Sum(s => s.TotalAmount),
                });
            }

            // Sort by revenue this month
            performances = performances
                .OrderByDescending(p => p.RevenueMonth)
                .ToList();

            // Assign ranks
            for (int i = 0; i < performances.Count; i++)
                performances[i].Rank = i + 1;

            // Totals
            ViewBag.TotalRevenue = performances
                .Sum(p => p.RevenueMonth);
            ViewBag.TotalProfit = performances
                .Sum(p => p.ProfitMonth);
            ViewBag.TotalSales = performances
                .Sum(p => p.SalesCountMonth);
            ViewBag.TotalDevices = performances
                .Sum(p => p.DevicesSoldMonth);

            // Filters
            ViewBag.Branches = await _db.Branches
                .Where(b => b.IsActive).ToListAsync();
            ViewBag.SelectedBranch = branchID;
            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedMonthName = firstOfMonth
                .ToString("MMMM yyyy");

            return View(performances);
        }

        public async Task<IActionResult> StaffDetails(
            string id, int? month, int? year)
        {
            var today = DateTime.Today;
            var selectedMonth = month ?? today.Month;
            var selectedYear = year ?? today.Year;
            var firstOfMonth = new DateTime(
                selectedYear, selectedMonth, 1);
            var lastOfMonth = firstOfMonth
                .AddMonths(1).AddDays(-1);

            var staff = await _userManager.FindByIdAsync(id);
            if (staff == null) return NotFound();

            var sales = await _db.PhoneSales
                .Include(s => s.Items)
                    .ThenInclude(i => i.Phone)
                .Include(s => s.Branch)
                .Where(s => s.StaffID == id &&
                            s.CreatedAt >= firstOfMonth &&
                            s.CreatedAt <= lastOfMonth)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            // 6 month trend
            var trend = new List<object>();
            for (int i = 5; i >= 0; i--)
            {
                var mStart = new DateTime(
                    today.Year, today.Month, 1).AddMonths(-i);
                var mEnd = mStart.AddMonths(1).AddDays(-1);

                var mSales = await _db.PhoneSales
                    .Where(s => s.StaffID == id &&
                                s.CreatedAt >= mStart &&
                                s.CreatedAt <= mEnd)
                    .ToListAsync();

                trend.Add(new
                {
                    month = mStart.ToString("MMM"),
                    sales = mSales.Count,
                    revenue = mSales.Sum(s => s.TotalAmount),
                    profit = mSales.Sum(s => s.TotalProfit)
                });
            }

            ViewBag.Staff = staff;
            ViewBag.Sales = sales;
            ViewBag.Trend = trend;
            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedMonthName = firstOfMonth
                .ToString("MMMM yyyy");
            ViewBag.TotalRevenue = sales.Sum(s => s.TotalAmount);
            ViewBag.TotalProfit = sales.Sum(s => s.TotalProfit);
            ViewBag.TotalSales = sales.Count;
            ViewBag.TotalDevices = sales.Sum(s => s.Items.Count);

            return View();
        }
    }
}