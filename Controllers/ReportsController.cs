using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BraysTech.Data;
using BraysTech.Models;

namespace BraysTech.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class ReportsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public ReportsController(
            AppDbContext db,
            UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ── DAILY REPORT ───────────────────────────────────
        public async Task<IActionResult> Daily(
            DateTime? date, int? branchID)
        {
            var selectedDate = date ?? DateTime.Today;

            var salesQuery = _db.PhoneSales
                .Include(s => s.Staff)
                .Include(s => s.Branch)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Phone)
                .Where(s => s.CreatedAt.Date == selectedDate.Date);

            if (branchID.HasValue)
                salesQuery = salesQuery.Where(s =>
                    s.BranchID == branchID);

            var sales = await salesQuery
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var expensesQuery = _db.Expenses
                .Where(e => e.ExpenseDate.Date == selectedDate.Date);

            if (branchID.HasValue)
                expensesQuery = expensesQuery.Where(e =>
                    e.BranchID == branchID);

            var expenses = await expensesQuery.ToListAsync();

            // Payment breakdown
            var paymentBreakdown = sales
                .GroupBy(s => s.PaymentMethod.ToString())
                .Select(g => new
                {
                    Method = g.Key,
                    Count = g.Count(),
                    Total = g.Sum(s => s.TotalAmount)
                }).ToList();

            // Staff breakdown
            var staffBreakdown = sales
                .GroupBy(s => s.Staff?.FullName ?? "Unknown")
                .Select(g => new
                {
                    StaffName = g.Key,
                    SalesCount = g.Count(),
                    Revenue = g.Sum(s => s.TotalAmount),
                    Profit = g.Sum(s => s.TotalProfit)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            // Top selling devices
            var deviceBreakdown = sales
                .SelectMany(s => s.Items)
                .GroupBy(i => i.PhoneName)
                .Select(g => new
                {
                    PhoneName = g.Key,
                    UnitsSold = g.Count(),
                    Revenue = g.Sum(i => i.SellingPrice),
                    Profit = g.Sum(i => i.Profit)
                })
                .OrderByDescending(x => x.UnitsSold)
                .ToList();

            decimal totalRevenue = sales.Sum(s => s.TotalAmount);
            decimal totalProfit = sales.Sum(s => s.TotalProfit);
            decimal totalExpenses = expenses.Sum(e => e.Amount);
            decimal netProfit = totalProfit - totalExpenses;

            ViewBag.SelectedDate = selectedDate;
            ViewBag.SelectedBranch = branchID;
            ViewBag.Branches = await _db.Branches
                .Where(b => b.IsActive).ToListAsync();
            ViewBag.Sales = sales;
            ViewBag.Expenses = expenses;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalProfit = totalProfit;
            ViewBag.TotalExpenses = totalExpenses;
            ViewBag.NetProfit = netProfit;
            ViewBag.TotalSales = sales.Count;
            ViewBag.TotalDevices = sales.Sum(s => s.Items.Count);
            ViewBag.PaymentBreakdown = paymentBreakdown;
            ViewBag.StaffBreakdown = staffBreakdown;
            ViewBag.DeviceBreakdown = deviceBreakdown;

            return View();
        }

        // ── MONTHLY REPORT ─────────────────────────────────
        public async Task<IActionResult> Monthly(
            int? month, int? year, int? branchID)
        {
            var today = DateTime.Today;
            var selectedMonth = month ?? today.Month;
            var selectedYear = year ?? today.Year;
            var firstOfMonth = new DateTime(
                selectedYear, selectedMonth, 1);
            var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);

            var salesQuery = _db.PhoneSales
                .Include(s => s.Staff)
                .Include(s => s.Branch)
                .Include(s => s.Items)
                .Where(s => s.CreatedAt >= firstOfMonth &&
                            s.CreatedAt <= lastOfMonth);

            if (branchID.HasValue)
                salesQuery = salesQuery.Where(s =>
                    s.BranchID == branchID);

            var sales = await salesQuery
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var expensesQuery = _db.Expenses
                .Where(e => e.ExpenseDate >= firstOfMonth &&
                            e.ExpenseDate <= lastOfMonth);

            if (branchID.HasValue)
                expensesQuery = expensesQuery.Where(e =>
                    e.BranchID == branchID);

            var expenses = await expensesQuery.ToListAsync();

            // Daily trend within the month
            var dailyTrend = sales
                .GroupBy(s => s.CreatedAt.Day)
                .Select(g => new
                {
                    Day = g.Key,
                    Revenue = g.Sum(s => s.TotalAmount),
                    Profit = g.Sum(s => s.TotalProfit),
                    Count = g.Count()
                })
                .OrderBy(x => x.Day)
                .ToList();

            // Branch breakdown (admin only)
            var branchBreakdown = sales
                .GroupBy(s => s.Branch?.Name ?? "Unknown")
                .Select(g => new
                {
                    BranchName = g.Key,
                    SalesCount = g.Count(),
                    Revenue = g.Sum(s => s.TotalAmount),
                    Profit = g.Sum(s => s.TotalProfit),
                    Devices = g.Sum(s => s.Items.Count)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            // Staff leaderboard
            var staffLeaderboard = sales
                .GroupBy(s => s.Staff?.FullName ?? "Unknown")
                .Select(g => new
                {
                    StaffName = g.Key,
                    SalesCount = g.Count(),
                    Revenue = g.Sum(s => s.TotalAmount),
                    Profit = g.Sum(s => s.TotalProfit),
                    Devices = g.Sum(s => s.Items.Count)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            // Top devices
            var topDevices = sales
                .SelectMany(s => s.Items)
                .GroupBy(i => i.PhoneName)
                .Select(g => new
                {
                    PhoneName = g.Key,
                    UnitsSold = g.Count(),
                    Revenue = g.Sum(i => i.SellingPrice),
                    Profit = g.Sum(i => i.Profit)
                })
                .OrderByDescending(x => x.UnitsSold)
                .ToList();

            // Payment methods
            var paymentBreakdown = sales
                .GroupBy(s => s.PaymentMethod.ToString())
                .Select(g => new
                {
                    Method = g.Key,
                    Count = g.Count(),
                    Total = g.Sum(s => s.TotalAmount)
                }).ToList();

            // 6 month comparison
            var sixMonthTrend = new List<object>();
            for (int i = 5; i >= 0; i--)
            {
                var mStart = new DateTime(
                    today.Year, today.Month, 1).AddMonths(-i);
                var mEnd = mStart.AddMonths(1).AddDays(-1);

                var mSales = await _db.PhoneSales
                    .Where(s => s.CreatedAt >= mStart &&
                                s.CreatedAt <= mEnd)
                    .ToListAsync();
                var mExp = await _db.Expenses
                    .Where(e => e.ExpenseDate >= mStart &&
                                e.ExpenseDate <= mEnd)
                    .SumAsync(e => (decimal?)e.Amount) ?? 0;

                sixMonthTrend.Add(new
                {
                    month = mStart.ToString("MMM yyyy"),
                    revenue = mSales.Sum(s => s.TotalAmount),
                    profit = mSales.Sum(s => s.TotalProfit),
                    expenses = mExp,
                    sales = mSales.Count
                });
            }

            decimal totalRevenue = sales.Sum(s => s.TotalAmount);
            decimal totalProfit = sales.Sum(s => s.TotalProfit);
            decimal totalExpenses = expenses.Sum(e => e.Amount);
            decimal netProfit = totalProfit - totalExpenses;

            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedBranch = branchID;
            ViewBag.SelectedMonthName = firstOfMonth
                .ToString("MMMM yyyy");
            ViewBag.Branches = await _db.Branches
                .Where(b => b.IsActive).ToListAsync();
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalProfit = totalProfit;
            ViewBag.TotalExpenses = totalExpenses;
            ViewBag.NetProfit = netProfit;
            ViewBag.TotalSales = sales.Count;
            ViewBag.TotalDevices = sales.Sum(s => s.Items.Count);
            ViewBag.DailyTrend = dailyTrend;
            ViewBag.BranchBreakdown = branchBreakdown;
            ViewBag.StaffLeaderboard = staffLeaderboard;
            ViewBag.TopDevices = topDevices;
            ViewBag.PaymentBreakdown = paymentBreakdown;
            ViewBag.SixMonthTrend = sixMonthTrend;

            return View();
        }
    }
}