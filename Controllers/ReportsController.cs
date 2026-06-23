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
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Daily(
            DateTime? date, int? branchID)
        {
            var selectedDate = date?.Date ?? DateTime.Today;
            var currentUserID = User
                .FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var currentUser = await _userManager
                .FindByIdAsync(currentUserID!);

            // Branch filter
            int? filterBranch = branchID;
            if (!isAdmin && currentUser?.BranchID != null)
                filterBranch = currentUser.BranchID;

            // ── PHONE SALES ───────────────────────────────
            var phoneSalesQuery = _db.PhoneSales
                .Include(s => s.Staff)
                .Include(s => s.Branch)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Phone)
                .Where(s => s.CreatedAt.Date == selectedDate);

            if (filterBranch.HasValue)
                phoneSalesQuery = phoneSalesQuery
                    .Where(s => s.BranchID == filterBranch);

            var phoneSales = await phoneSalesQuery
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            // ── ACCESSORY SALES ───────────────────────────
            var accSalesQuery = _db.AccessorySales
                .Include(s => s.Staff)
                .Include(s => s.Branch)
                .Include(s => s.Items)
                .Where(s => s.CreatedAt.Date == selectedDate);

            if (filterBranch.HasValue)
                accSalesQuery = accSalesQuery
                    .Where(s => s.BranchID == filterBranch);

            var accSales = await accSalesQuery
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            // ── SERVICE RECORDS ───────────────────────────
            var servicesQuery = _db.ServiceRecords
                .Include(s => s.Staff)
                .Include(s => s.Branch)
                .Where(s => s.CreatedAt.Date == selectedDate);

            if (filterBranch.HasValue)
                servicesQuery = servicesQuery
                    .Where(s => s.BranchID == filterBranch);

            var services = await servicesQuery
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            // ── SIM CARD SALES ────────────────────────────
            var simSalesQuery = _db.SimCards
                .Include(s => s.Branch)
                .Where(s =>
                    s.Status == SimCardStatus.Sold &&
                    s.DateSold.HasValue &&
                    s.DateSold.Value.Date == selectedDate);

            if (filterBranch.HasValue)
                simSalesQuery = simSalesQuery
                    .Where(s => s.BranchID == filterBranch);

            var simSales = await simSalesQuery
                .OrderByDescending(s => s.DateSold)
                .ToListAsync();

            // ── EXPENSES ──────────────────────────────────
            var expensesQuery = _db.Expenses
                .Where(e => e.ExpenseDate.Date == selectedDate);

            if (filterBranch.HasValue)
                expensesQuery = expensesQuery
                    .Where(e => e.BranchID == filterBranch);

            var expenses = await expensesQuery
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync();

            // ── CASH-UPS ──────────────────────────────────
            var cashUpsQuery = _db.CashUps
                .Include(c => c.Staff)
                .Include(c => c.Branch)
                .Where(c => c.CashUpDate == selectedDate);

            if (filterBranch.HasValue)
                cashUpsQuery = cashUpsQuery
                    .Where(c => c.BranchID == filterBranch);

            var cashUps = await cashUpsQuery
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            // ── TOTALS ────────────────────────────────────
            decimal phoneRevenue =
                phoneSales.Sum(s => s.TotalAmount);
            decimal phoneProfit =
                phoneSales.Sum(s => s.TotalProfit);

            decimal accRevenue =
                accSales.Sum(s => s.TotalAmount);
            decimal accProfit =
                accSales.Sum(s => s.TotalProfit);

            decimal serviceRevenue =
                services.Sum(s => s.ChargeAmount);

            decimal simRevenue =
                simSales.Sum(s => s.SellingPrice);
            decimal simProfit =
                simSales.Sum(s =>
                    s.SellingPrice - s.BuyingPrice);

            decimal totalRevenue = phoneRevenue +
                accRevenue + serviceRevenue + simRevenue;
            decimal grossProfit = phoneProfit +
                accProfit + simProfit + serviceRevenue;
            // Services are mostly profit
            decimal totalExpenses =
                expenses.Sum(e => e.Amount);
            decimal netProfit = grossProfit - totalExpenses;

            decimal totalCashDeclared =
                cashUps.Sum(c => c.CashAmount);
            decimal totalMpesaDeclared =
                cashUps.Sum(c => c.MpesaFloat);

            // Payment method breakdown (phone + accessories)
            decimal cashTotal =
                phoneSales.Where(s =>
                    s.PaymentMethod ==
                        SalePaymentMethod.Cash)
                    .Sum(s => s.TotalAmount) +
                accSales.Where(s =>
                    s.PaymentMethod ==
                        SalePaymentMethod.Cash)
                    .Sum(s => s.TotalAmount);

            decimal mpesaTotal =
                phoneSales.Where(s =>
                    s.PaymentMethod ==
                        SalePaymentMethod.MPesa)
                    .Sum(s => s.TotalAmount) +
                accSales.Where(s =>
                    s.PaymentMethod ==
                        SalePaymentMethod.MPesa)
                    .Sum(s => s.TotalAmount);

            decimal cardTotal =
                phoneSales.Where(s =>
                    s.PaymentMethod ==
                        SalePaymentMethod.Card)
                    .Sum(s => s.TotalAmount) +
                accSales.Where(s =>
                    s.PaymentMethod ==
                        SalePaymentMethod.Card)
                    .Sum(s => s.TotalAmount);

            // Pass everything to view
            ViewBag.SelectedDate = selectedDate;
            ViewBag.SelectedBranch = filterBranch;
            ViewBag.Branches = await _db.Branches
                .Where(b => b.IsActive).ToListAsync();
            ViewBag.IsAdmin = isAdmin;

            ViewBag.PhoneSales = phoneSales;
            ViewBag.AccSales = accSales;
            ViewBag.Services = services;
            ViewBag.SimSales = simSales;
            ViewBag.Expenses = expenses;
            ViewBag.CashUps = cashUps;

            ViewBag.PhoneRevenue = phoneRevenue;
            ViewBag.PhoneProfit = phoneProfit;
            ViewBag.AccRevenue = accRevenue;
            ViewBag.AccProfit = accProfit;
            ViewBag.ServiceRevenue = serviceRevenue;
            ViewBag.SimRevenue = simRevenue;
            ViewBag.SimProfit = simProfit;

            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.GrossProfit = grossProfit;
            ViewBag.TotalExpenses = totalExpenses;
            ViewBag.NetProfit = netProfit;

            ViewBag.CashTotal = cashTotal;
            ViewBag.MpesaTotal = mpesaTotal;
            ViewBag.CardTotal = cardTotal;

            ViewBag.TotalCashDeclared = totalCashDeclared;
            ViewBag.TotalMpesaDeclared = totalMpesaDeclared;

            ViewBag.PhoneSalesCount = phoneSales.Count;
            ViewBag.AccSalesCount = accSales.Count;
            ViewBag.ServicesCount = services.Count;
            ViewBag.SimSalesCount = simSales.Count;

            return View();
        }

        // ── MONTHLY REPORT ─────────────────────────────────
        public async Task<IActionResult> Monthly(
            int? month, int? year, int? branchID)
        {
            var today = DateTime.Today;
            var selectedMonth = month ?? today.Month;
            var selectedYear = year ?? today.Year;

            var firstOfMonth = new DateTime(selectedYear, selectedMonth, 1);
            var lastOfMonth = new DateTime(
                selectedYear,
                selectedMonth,
                DateTime.DaysInMonth(selectedYear, selectedMonth),
                23, 59, 59
            );

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
                var mEnd = new DateTime(
                    mStart.Year,
                    mStart.Month,
                    DateTime.DaysInMonth(mStart.Year, mStart.Month),
                    23, 59, 59
                );

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