using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SplashCityCarwash.Data;
using SplashCityCarwash.Models;

namespace SplashCityCarwash.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class ReportsController : Controller
    {
        private readonly AppDbContext _db;
        public ReportsController(AppDbContext db) { _db = db; }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Index(int? month, int? year)
        {
            var today = DateTime.Today;
            var selectedMonth = month ?? today.Month;
            var selectedYear = year ?? today.Year;

            var firstOfMonth = new DateTime(selectedYear, selectedMonth, 1);
            var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);
            var firstOfYear = new DateTime(selectedYear, 1, 1);

            // ── CARWASH REVENUE ────────────────────────────
            var carwashRevenueToday = await _db.Transactions
                .Where(t => t.CreatedAt.Date == today &&
                           (t.Status == WashStatus.Completed ||
                            t.Status == WashStatus.Paid))
                .SumAsync(t => (decimal?)t.TotalAmount) ?? 0;

            var carwashRevenueMonth = await _db.Transactions
                .Where(t => t.CreatedAt >= firstOfMonth &&
                            t.CreatedAt <= lastOfMonth &&
                           (t.Status == WashStatus.Completed ||
                            t.Status == WashStatus.Paid))
                .SumAsync(t => (decimal?)t.TotalAmount) ?? 0;

            var carwashRevenueYear = await _db.Transactions
                .Where(t => t.CreatedAt >= firstOfYear &&
                           (t.Status == WashStatus.Completed ||
                            t.Status == WashStatus.Paid))
                .SumAsync(t => (decimal?)t.TotalAmount) ?? 0;

            var washesMonth = await _db.Transactions
                .CountAsync(t => t.CreatedAt >= firstOfMonth &&
                                 t.CreatedAt <= lastOfMonth &&
                                (t.Status == WashStatus.Completed ||
                                 t.Status == WashStatus.Paid));

            var washesToday = await _db.Transactions
                .CountAsync(t => t.CreatedAt.Date == today &&
                                (t.Status == WashStatus.Completed ||
                                 t.Status == WashStatus.Paid));

            // ── EXPENSES ───────────────────────────────────
            var expensesMonth = await _db.Expenses
                .Where(e => e.ExpenseDate >= firstOfMonth &&
                            e.ExpenseDate <= lastOfMonth)
                .SumAsync(e => (decimal?)e.Amount) ?? 0;

            var expensesYear = await _db.Expenses
                .Where(e => e.ExpenseDate >= firstOfYear)
                .SumAsync(e => (decimal?)e.Amount) ?? 0;

            // ── CARWASH PROFIT ─────────────────────────────
            var carwashProfitMonth = carwashRevenueMonth - expensesMonth;
            var carwashProfitYear = carwashRevenueYear - expensesYear;

            // ── SHOP ───────────────────────────────────────
            var shopRevenueToday = await _db.ShopSales
                .Where(s => s.CreatedAt.Date == today)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

            var shopRevenueMonth = await _db.ShopSales
                .Where(s => s.CreatedAt >= firstOfMonth &&
                            s.CreatedAt <= lastOfMonth)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

            var shopRevenueYear = await _db.ShopSales
                .Where(s => s.CreatedAt >= firstOfYear)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

            var shopProfitMonth = await _db.ShopSales
                .Where(s => s.CreatedAt >= firstOfMonth &&
                            s.CreatedAt <= lastOfMonth)
                .SumAsync(s => (decimal?)s.TotalProfit) ?? 0;

            var shopProfitYear = await _db.ShopSales
                .Where(s => s.CreatedAt >= firstOfYear)
                .SumAsync(s => (decimal?)s.TotalProfit) ?? 0;

            var shopCostMonth = shopRevenueMonth - shopProfitMonth;

            // ── COMBINED ───────────────────────────────────
            var totalRevenueMonth = carwashRevenueMonth + shopRevenueMonth;
            var totalCostsMonth = expensesMonth + shopCostMonth;
            var totalProfitMonth = carwashProfitMonth + shopProfitMonth;
            var totalRevenueYear = carwashRevenueYear + shopRevenueYear;
            var totalProfitYear = carwashProfitYear + shopProfitYear;

            // ── 6 MONTH TREND ──────────────────────────────
            var monthlyData = new List<object>();
            for (int i = 5; i >= 0; i--)
            {
                var mStart = new DateTime(today.Year, today.Month, 1).AddMonths(-i);
                var mEnd = mStart.AddMonths(1).AddDays(-1);

                var mCarwash = await _db.Transactions
                    .Where(t => t.CreatedAt >= mStart && t.CreatedAt <= mEnd &&
                               (t.Status == WashStatus.Completed ||
                                t.Status == WashStatus.Paid))
                    .SumAsync(t => (decimal?)t.TotalAmount) ?? 0;

                var mShop = await _db.ShopSales
                    .Where(s => s.CreatedAt >= mStart && s.CreatedAt <= mEnd)
                    .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

                var mExpenses = await _db.Expenses
                    .Where(e => e.ExpenseDate >= mStart && e.ExpenseDate <= mEnd)
                    .SumAsync(e => (decimal?)e.Amount) ?? 0;

                var mShopProfit = await _db.ShopSales
                    .Where(s => s.CreatedAt >= mStart && s.CreatedAt <= mEnd)
                    .SumAsync(s => (decimal?)s.TotalProfit) ?? 0;

                monthlyData.Add(new
                {
                    month = mStart.ToString("MMM yyyy"),
                    carwashRevenue = mCarwash,
                    shopRevenue = mShop,
                    totalRevenue = mCarwash + mShop,
                    expenses = mExpenses,
                    profit = (mCarwash - mExpenses) + mShopProfit
                });
            }

            // ── SERVICE POPULARITY ─────────────────────────
            var serviceStats = await _db.TransactionServices
                .Include(ts => ts.Service)
                .Include(ts => ts.Transaction)
                .Where(ts => ts.Transaction.CreatedAt >= firstOfMonth &&
                             ts.Transaction.CreatedAt <= lastOfMonth)
                .GroupBy(ts => ts.Service.ServiceName)
                .Select(g => new
                {
                    ServiceName = g.Key,
                    TimesUsed = g.Count(),
                    Revenue = g.Sum(ts => ts.PriceAtTime)
                })
                .OrderByDescending(x => x.TimesUsed)
                .ToListAsync();

            // ── PAYMENT BREAKDOWN ──────────────────────────
            var paymentStats = await _db.Transactions
                .Where(t => t.CreatedAt >= firstOfMonth &&
                            t.CreatedAt <= lastOfMonth &&
                           (t.Status == WashStatus.Completed ||
                            t.Status == WashStatus.Paid))
                .GroupBy(t => t.PaymentMethod)
                .Select(g => new
                {
                    Method = g.Key,
                    Count = g.Count(),
                    Total = g.Sum(t => t.TotalAmount)
                })
                .ToListAsync();

            // ── TOP CUSTOMERS ──────────────────────────────
            var topCustomers = await _db.Customers
                .OrderByDescending(c => c.TotalSpent)
                .Take(5)
                .ToListAsync();

            // ── TOTALS ─────────────────────────────────────
            var totalCustomers = await _db.Customers.CountAsync();
            var totalVehicles = await _db.Vehicles.CountAsync();

            // ── VIEWBAG ────────────────────────────────────
            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedMonthName = firstOfMonth.ToString("MMMM yyyy");

            // Today
            ViewBag.CarwashRevenueToday = carwashRevenueToday;
            ViewBag.ShopRevenueToday = shopRevenueToday;
            ViewBag.WashesToday = washesToday;

            // Carwash
            ViewBag.CarwashRevenueMonth = carwashRevenueMonth;
            ViewBag.CarwashRevenueYear = carwashRevenueYear;
            ViewBag.CarwashProfitMonth = carwashProfitMonth;
            ViewBag.CarwashProfitYear = carwashProfitYear;
            ViewBag.WashesMonth = washesMonth;

            // Expenses
            ViewBag.ExpensesMonth = expensesMonth;
            ViewBag.ExpensesYear = expensesYear;

            // Shop
            ViewBag.ShopRevenueMonth = shopRevenueMonth;
            ViewBag.ShopRevenueYear = shopRevenueYear;
            ViewBag.ShopProfitMonth = shopProfitMonth;
            ViewBag.ShopProfitYear = shopProfitYear;
            ViewBag.ShopCostMonth = shopCostMonth;

            // Combined
            ViewBag.TotalRevenueMonth = totalRevenueMonth;
            ViewBag.TotalCostsMonth = totalCostsMonth;
            ViewBag.TotalProfitMonth = totalProfitMonth;
            ViewBag.TotalRevenueYear = totalRevenueYear;
            ViewBag.TotalProfitYear = totalProfitYear;

            // Counts
            ViewBag.TotalCustomers = totalCustomers;
            ViewBag.TotalVehicles = totalVehicles;

            // Tables
            ViewBag.MonthlyData = monthlyData;
            ViewBag.ServiceStats = serviceStats;
            ViewBag.PaymentStats = paymentStats;
            ViewBag.TopCustomers = topCustomers;

            return View();
        }
    }
}