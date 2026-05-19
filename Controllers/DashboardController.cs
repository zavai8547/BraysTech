using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<AppUser> _userManager;

        public DashboardController(
            AppDbContext db,
            UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var firstOfMonth = new DateTime(
                today.Year, today.Month, 1);

            // Top level stats
            var totalBranches = await _db.Branches
                .CountAsync(b => b.IsActive);
            var totalStaff = await _db.Users.CountAsync();
            var totalInStock = await _db.IMEIStock
                .CountAsync(i => i.Status == PhoneStatus.InStock);
            var totalFaulty = await _db.IMEIStock
                .CountAsync(i => i.Status == PhoneStatus.Faulty);
            var totalDisplay = await _db.IMEIStock
                .CountAsync(i => i.Status == PhoneStatus.DisplayUnit);

            var salesToday = await _db.PhoneSales
                .CountAsync(s => s.CreatedAt.Date == today);
            var revToday = await _db.PhoneSales
                .Where(s => s.CreatedAt.Date == today)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

            var salesMonth = await _db.PhoneSales
                .CountAsync(s => s.CreatedAt >= firstOfMonth);
            var revMonth = await _db.PhoneSales
                .Where(s => s.CreatedAt >= firstOfMonth)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;
            var profitMonth = await _db.PhoneSales
                .Where(s => s.CreatedAt >= firstOfMonth)
                .SumAsync(s => (decimal?)s.TotalProfit) ?? 0;

            // Branch summaries
            var branches = await _db.Branches
                .Where(b => b.IsActive).ToListAsync();
            var branchSummaries = new List<BranchSummary>();

            foreach (var b in branches)
            {
                branchSummaries.Add(new BranchSummary
                {
                    BranchName = b.Name,
                    StaffCount = await _db.Users
                        .CountAsync(u => u.BranchID == b.BranchID),
                    PhonesInStock = await _db.IMEIStock
                        .CountAsync(i =>
                            i.BranchID == b.BranchID &&
                            i.Status == PhoneStatus.InStock),
                    RevenueToday = await _db.PhoneSales
                        .Where(s => s.BranchID == b.BranchID &&
                                    s.CreatedAt.Date == today)
                        .SumAsync(s => (decimal?)s.TotalAmount) ?? 0,
                    RevenueThisMonth = await _db.PhoneSales
                        .Where(s => s.BranchID == b.BranchID &&
                                    s.CreatedAt >= firstOfMonth)
                        .SumAsync(s => (decimal?)s.TotalAmount) ?? 0
                });
            }

            // Recent sales
            var recentSales = await _db.PhoneSales
                .Include(s => s.Staff)
                .Include(s => s.Branch)
                .Include(s => s.Items)
                .OrderByDescending(s => s.CreatedAt)
                .Take(8)
                .ToListAsync();

            // Top staff today
            var allStaff = await _userManager.Users
                .Where(u => u.IsActive).ToListAsync();
            var topStaff = new List<StaffPerformance>();

            foreach (var u in allStaff)
            {
                var todaySales = await _db.PhoneSales
                    .Where(s => s.StaffID == u.Id &&
                                s.CreatedAt.Date == today)
                    .ToListAsync();

                if (!todaySales.Any()) continue;

                topStaff.Add(new StaffPerformance
                {
                    StaffID = u.Id,
                    StaffName = u.FullName,
                    SalesToday = todaySales.Count,
                    RevenueToday = todaySales
                        .Sum(s => s.TotalAmount)
                });
            }

            var vm = new DashboardViewModel
            {
                TotalBranches = totalBranches,
                TotalStaff = totalStaff,
                TotalPhonesInStock = totalInStock,
                TotalFaulty = totalFaulty,
                TotalDisplay = totalDisplay,
                SalesToday = salesToday,
                RevenueTodayAll = revToday,
                SalesThisMonth = salesMonth,
                RevenueThisMonthAll = revMonth,
                ProfitThisMonth = profitMonth,
                BranchSummaries = branchSummaries,
                RecentSales = recentSales,
                TopStaffToday = topStaff
                    .OrderByDescending(s => s.RevenueToday)
                    .Take(5).ToList()
            };

            return View(vm);
        }
    }
}