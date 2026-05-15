using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BraysTech.Data;
using BraysTech.Models;

namespace BraysTech.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _db;

        public DashboardController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

            // 1. Fetch Top-Level Stats
            var totalBranches = await _db.Branches.CountAsync();
            var totalStaff = await _db.Users.CountAsync();

            // Fix: Changed 'IsSold' to 'Status == PhoneStatus.InStock'
            var totalPhonesInStock = await _db.IMEIStock
                .CountAsync(i => i.Status == PhoneStatus.InStock);

            var salesToday = await _db.PhoneSales
                .CountAsync(s => s.CreatedAt.Date == today);

            var revenueTodayAll = await _db.PhoneSales
                .Where(s => s.CreatedAt.Date == today)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

            var salesThisMonth = await _db.PhoneSales
                .CountAsync(s => s.CreatedAt >= firstDayOfMonth);

            var revenueThisMonthAll = await _db.PhoneSales
                .Where(s => s.CreatedAt >= firstDayOfMonth)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

            // Profit calculation using your model's 'TotalProfit' field
            var profitThisMonth = await _db.PhoneSales
                .Where(s => s.CreatedAt >= firstDayOfMonth)
                .SumAsync(s => (decimal?)s.TotalProfit) ?? 0;

            // 2. Fetch Branches and calculate summaries manually to avoid SQL translation issues
            var branches = await _db.Branches.ToListAsync();
            var branchSummaries = new List<BranchSummary>();

            foreach (var b in branches)
            {
                branchSummaries.Add(new BranchSummary
                {
                    BranchName = b.Name,

                    // FIXED HERE: Changed u.BranchId == b.Id  →  u.BranchID == b.BranchID
                    StaffCount = await _db.Users.CountAsync(u => u.BranchID == b.BranchID),

                    // Fix: Check Status enum instead of IsSold
                    PhonesInStock = await _db.IMEIStock
                        .CountAsync(i => i.BranchID == b.BranchID && i.Status == PhoneStatus.InStock),

                    RevenueToday = await _db.PhoneSales
                        .Where(s => s.BranchID == b.BranchID && s.CreatedAt.Date == today)
                        .SumAsync(s => (decimal?)s.TotalAmount) ?? 0,

                    RevenueThisMonth = await _db.PhoneSales
                        .Where(s => s.BranchID == b.BranchID && s.CreatedAt >= firstDayOfMonth)
                        .SumAsync(s => (decimal?)s.TotalAmount) ?? 0
                });
            }

            // 3. Bind to ViewModel
            var vm = new DashboardViewModel
            {
                TotalBranches = totalBranches,
                TotalStaff = totalStaff,
                TotalPhonesInStock = totalPhonesInStock,
                SalesToday = salesToday,
                RevenueTodayAll = revenueTodayAll,
                SalesThisMonth = salesThisMonth,
                RevenueThisMonthAll = revenueThisMonthAll,
                ProfitThisMonth = profitThisMonth,
                BranchSummaries = branchSummaries
            };

            return View(vm);
        }
    }
}