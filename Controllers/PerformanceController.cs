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

                // Phone revenue
                var phoneRevenue = salesThisMonth
                    .Sum(s => s.TotalAmount);

                // Accessory sales revenue
                var accRevenue = await _db.AccessorySales
                    .Where(s => s.StaffID == staff.Id &&
                                s.CreatedAt >= firstOfMonth &&
                                s.CreatedAt <= lastOfMonth)
                    .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

                // Service records revenue
                var svcRevenue = await _db.ServiceRecords
                    .Where(s => s.StaffID == staff.Id &&
                                s.CreatedAt >= firstOfMonth &&
                                s.CreatedAt <= lastOfMonth)
                    .SumAsync(s => (decimal?)s.ChargeAmount) ?? 0;

                // SIM card sales revenue (doesn't track StaffID yet)
                var simRevenue = await _db.SimCards
                    .Where(s => s.DateSold.HasValue &&
                                s.DateSold.Value >= firstOfMonth &&
                                s.DateSold.Value <= lastOfMonth)
                    .SumAsync(s => (decimal?)s.SellingPrice) ?? 0;

                // Total revenue per staff (including all modules)
                var totalRevenue = phoneRevenue + accRevenue + svcRevenue + simRevenue;

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
                    RevenueMonth = totalRevenue,
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

            // Get accessory sales for this staff
            var accessorySales = await _db.AccessorySales
                .Where(s => s.StaffID == id &&
                            s.CreatedAt >= firstOfMonth &&
                            s.CreatedAt <= lastOfMonth)
                .ToListAsync();

            // Get service records for this staff
            var serviceRecords = await _db.ServiceRecords
                .Where(s => s.StaffID == id &&
                            s.CreatedAt >= firstOfMonth &&
                            s.CreatedAt <= lastOfMonth)
                .ToListAsync();

            // Get SIM card sales (doesn't track StaffID yet)
            var simSales = await _db.SimCards
                .Where(s => s.DateSold.HasValue &&
                            s.DateSold.Value >= firstOfMonth &&
                            s.DateSold.Value <= lastOfMonth)
                .ToListAsync();

            // Calculate total revenue including all modules
            var phoneRevenue = sales.Sum(s => s.TotalAmount);
            var accRevenue = accessorySales.Sum(s => s.TotalAmount);
            var svcRevenue = serviceRecords.Sum(s => s.ChargeAmount);
            var simRevenue = simSales.Sum(s => s.SellingPrice);
            var totalRevenue = phoneRevenue + accRevenue + svcRevenue + simRevenue;

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

                var mAccessorySales = await _db.AccessorySales
                    .Where(s => s.StaffID == id &&
                                s.CreatedAt >= mStart &&
                                s.CreatedAt <= mEnd)
                    .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

                var mServiceRevenue = await _db.ServiceRecords
                    .Where(s => s.StaffID == id &&
                                s.CreatedAt >= mStart &&
                                s.CreatedAt <= mEnd)
                    .SumAsync(s => (decimal?)s.ChargeAmount) ?? 0;

                var mSimRevenue = await _db.SimCards
                    .Where(s => s.DateSold.HasValue &&
                                s.DateSold.Value >= mStart &&
                                s.DateSold.Value <= mEnd)
                    .SumAsync(s => (decimal?)s.SellingPrice) ?? 0;

                var mTotalRevenue = mSales.Sum(s => s.TotalAmount) + mAccessorySales + mServiceRevenue + mSimRevenue;

                trend.Add(new
                {
                    month = mStart.ToString("MMM"),
                    sales = mSales.Count,
                    revenue = mTotalRevenue,
                    profit = mSales.Sum(s => s.TotalProfit)
                });
            }

            ViewBag.Staff = staff;
            ViewBag.Sales = sales;
            ViewBag.AccessorySales = accessorySales;
            ViewBag.ServiceRecords = serviceRecords;
            ViewBag.SimSales = simSales;
            ViewBag.Trend = trend;
            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedMonthName = firstOfMonth
                .ToString("MMMM yyyy");
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalProfit = sales.Sum(s => s.TotalProfit);
            ViewBag.TotalSales = sales.Count;
            ViewBag.TotalDevices = sales.Sum(s => s.Items.Count);
            ViewBag.TotalAccessoryRevenue = accRevenue;
            ViewBag.TotalServiceRevenue = svcRevenue;
            ViewBag.TotalSimRevenue = simRevenue;

            return View();
        }
    }
}