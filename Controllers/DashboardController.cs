using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SplashCityCarwash.Data;
using SplashCityCarwash.Models;

namespace SplashCityCarwash.Controllers
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

            var vm = new DashboardViewModel
            {
                CarsWaiting = await _db.WashQueues
                    .CountAsync(q => q.Status == WashStatus.Waiting),

                CarsBeingWashed = await _db.WashQueues
                    .CountAsync(q => q.Status == WashStatus.Washing),

                CarsCompletedToday = await _db.Transactions
                    .CountAsync(t => (t.Status == WashStatus.Completed ||
                                      t.Status == WashStatus.Paid)
                                  && t.CreatedAt.Date == today),

                RevenueToday = await _db.Transactions
                    .Where(t => t.CreatedAt.Date == today &&
                               (t.Status == WashStatus.Completed ||
                                t.Status == WashStatus.Paid))
                    .SumAsync(t => (decimal?)t.TotalAmount) ?? 0,

                RevenueThisMonth = await _db.Transactions
                    .Where(t => t.CreatedAt >= firstDayOfMonth &&
                               (t.Status == WashStatus.Completed ||
                                t.Status == WashStatus.Paid))
                    .SumAsync(t => (decimal?)t.TotalAmount) ?? 0,

                TotalCustomers = await _db.Customers.CountAsync(),

                MostPopularService = await _db.TransactionServices
                    .GroupBy(ts => ts.Service.ServiceName)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefaultAsync() ?? "N/A",

                RecentTransactions = await _db.Transactions
                    .Include(t => t.Customer)
                    .Include(t => t.Vehicle)
                    .Include(t => t.TransactionServices)
                        .ThenInclude(ts => ts.Service)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(5)
                    .ToListAsync(),

                ActiveQueue = await _db.WashQueues
                    .Include(q => q.Transaction)
                        .ThenInclude(t => t.Customer)
                    .Include(q => q.Transaction)
                        .ThenInclude(t => t.Vehicle)
                    .Where(q => q.Status == WashStatus.Waiting
                             || q.Status == WashStatus.Washing)
                    .OrderBy(q => q.QueuePosition)
                    .ToListAsync()
            };

            return View(vm);
        }
    }
}