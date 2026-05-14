using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SplashCityCarwash.Data;
using SplashCityCarwash.Models;

namespace SplashCityCarwash.Controllers
{
    [Authorize]
    public class QueueController : Controller
    {
        private readonly AppDbContext _db;
        public QueueController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            var queue = await _db.WashQueues
                .Include(q => q.Transaction)
                    .ThenInclude(t => t.Customer)
                .Include(q => q.Transaction)
                    .ThenInclude(t => t.Vehicle)
                .Include(q => q.Transaction)
                    .ThenInclude(t => t.TransactionServices)
                        .ThenInclude(ts => ts.Service)
                .Include(q => q.AssignedWasher)
                .Where(q => q.Status == WashStatus.Waiting
                         || q.Status == WashStatus.Washing)
                .OrderBy(q => q.QueuePosition)
                .ToListAsync();

            return View(queue);
        }

        // ── UPDATE STATUS ──────────────────────────────
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, WashStatus status)
        {
            var queue = await _db.WashQueues
                .Include(q => q.Transaction)
                .FirstOrDefaultAsync(q => q.QueueID == id);

            if (queue == null) return NotFound();

            queue.Status = status;
            queue.Transaction.Status = status;

            if (status == WashStatus.Washing)
                queue.StartedAt = DateTime.Now;

            if (status == WashStatus.Completed ||
                status == WashStatus.Cancelled)
                queue.CompletedAt = DateTime.Now;

            if (status == WashStatus.Completed)
                queue.Transaction.CompletedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            TempData["Success"] = $"✅ Status updated to {status}!";
            return RedirectToAction("Index");
        }

        // ── ASSIGN WASHER ──────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AssignWasher(int id, string washerID)
        {
            var queue = await _db.WashQueues.FindAsync(id);
            if (queue != null)
            {
                queue.AssignedWasherID = washerID;
                await _db.SaveChangesAsync();
                TempData["Success"] = "✅ Washer assigned!";
            }
            return RedirectToAction("Index");
        }
    }
}