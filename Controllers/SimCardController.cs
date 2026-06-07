using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BraysTech.Data;
using BraysTech.Models;
using BraysTech.Services;
using System.Security.Claims;

namespace BraysTech.Controllers
{
    [Authorize]
    public class SimCardController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly AuditService _audit;

        public SimCardController(
            AppDbContext db,
            UserManager<AppUser> userManager,
            AuditService audit)
        {
            _db = db;
            _userManager = userManager;
            _audit = audit;
        }

        public async Task<IActionResult> Index(
            string? network, string? status,
            int? branchID)
        {
            var currentUserID = User
                .FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("Manager");
            var currentUser = await _userManager
                .FindByIdAsync(currentUserID!);

            var query = _db.SimCards
                .Include(s => s.Branch)
                .AsQueryable();

            if (!isAdmin && currentUser?.BranchID != null)
                query = query.Where(s =>
                    s.BranchID == currentUser.BranchID);

            if (!string.IsNullOrEmpty(network) &&
                Enum.TryParse<SimNetwork>(
                    network, out var sn))
                query = query.Where(s =>
                    s.Network == sn);

            if (!string.IsNullOrEmpty(status) &&
                Enum.TryParse<SimCardStatus>(
                    status, out var ss))
                query = query.Where(s =>
                    s.Status == ss);

            if (branchID.HasValue)
                query = query.Where(s =>
                    s.BranchID == branchID);

            var sims = await query
                .OrderBy(s => s.Network)
                .ThenBy(s => s.Status)
                .ToListAsync();

            var today = DateTime.Today;

            ViewBag.TotalInStock = sims.Count(s =>
                s.Status == SimCardStatus.InStock);
            ViewBag.SoldToday = await _db.SimCards
                .CountAsync(s =>
                    s.DateSold.HasValue &&
                    s.DateSold.Value.Date == today);
            ViewBag.TotalSold = sims.Count(s =>
                s.Status == SimCardStatus.Sold);
            ViewBag.ReplacementsToday = await _db.SimCards
                .CountAsync(s =>
                    s.IsReplacement &&
                    s.DateSold.HasValue &&
                    s.DateSold.Value.Date == today);

            ViewBag.ByNetwork = sims
                .Where(s => s.Status == SimCardStatus.InStock)
                .GroupBy(s => s.Network)
                .Select(g => new
                {
                    Network = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToList();

            ViewBag.Branches = await _db.Branches
                .Where(b => b.IsActive).ToListAsync();
            ViewBag.Networks =
                Enum.GetNames(typeof(SimNetwork));
            ViewBag.Statuses =
                Enum.GetNames(typeof(SimCardStatus));
            ViewBag.SelectedNetwork = network;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedBranch = branchID;

            return View(sims);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Add()
        {
            ViewBag.Branches = await _db.Branches
                .Where(b => b.IsActive).ToListAsync();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Add(
            SimNetwork network,
            int quantity,
            decimal buyingPrice,
            decimal sellingPrice,
            int branchID,
            string? notes)
        {
            if (quantity <= 0)
            {
                TempData["Error"] = "Quantity must be at least 1.";
                ViewBag.Branches = await _db.Branches
                    .Where(b => b.IsActive).ToListAsync();
                return View();
            }

            for (int i = 0; i < quantity; i++)
            {
                _db.SimCards.Add(new SimCard
                {
                    Network = network,
                    BuyingPrice = buyingPrice,
                    SellingPrice = sellingPrice,
                    BranchID = branchID,
                    Status = SimCardStatus.InStock,
                    Notes = notes,
                    DateAdded = DateTime.Now
                });
            }

            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                AuditAction.StockAdded,
                "SimCards",
                $"{quantity} {network} SIM card(s) added. " +
                $"Sell: KES {sellingPrice:N0}.",
                recordType: "SimCard");

            TempData["Success"] =
                $"{quantity} {network} SIM card(s) added to stock.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Sell(
            int simCardID,
            string customerName,
            string customerPhone,
            string? customerIDNumber,
            bool isReplacement,
            string? oldSimNumber,
            string? newSimNumber,
            SalePaymentMethod paymentMethod,
            string? mpesaCode,
            int branchID)
        {
            var sim = await _db.SimCards.FindAsync(simCardID);
            if (sim == null ||
                sim.Status != SimCardStatus.InStock)
            {
                TempData["Error"] = "SIM card not available.";
                return RedirectToAction("Index");
            }

            // Update SIM record
            sim.Status = SimCardStatus.Sold;
            sim.DateSold = DateTime.Now;
            sim.SoldToName = customerName?.Trim();
            sim.SoldToPhone = customerPhone?.Trim();
            sim.CustomerIDNumber = customerIDNumber?.Trim();
            sim.IsReplacement = isReplacement;
            sim.OldSimNumber = oldSimNumber?.Trim();
            sim.NewSimNumber = newSimNumber?.Trim();
            sim.PaymentMethod = paymentMethod;
            sim.MpesaCode = paymentMethod ==
                SalePaymentMethod.MPesa
                ? mpesaCode?.Trim().ToUpper()
                : null;

            // Auto-create or update customer record
            if (!string.IsNullOrEmpty(customerPhone))
            {
                var existing = await _db.Customers
                    .FirstOrDefaultAsync(c =>
                        c.Phone == customerPhone.Trim());
                if (existing != null)
                {
                    existing.TotalPurchases++;
                    existing.TotalSpent += sim.SellingPrice;
                }
                else if (!string.IsNullOrEmpty(customerName))
                {
                    _db.Customers.Add(new Customer
                    {
                        FullName = customerName.Trim(),
                        Phone = customerPhone.Trim(),
                        TotalPurchases = 1,
                        TotalSpent = sim.SellingPrice,
                        CreatedAt = DateTime.Now
                    });
                }
            }

            // If replacement, create a service record too
            if (isReplacement)
            {
                var staffID = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);
                _db.ServiceRecords.Add(new ServiceRecord
                {
                    ServiceType = ServiceType.SimReplacement,
                    StaffID = staffID!,
                    BranchID = branchID,
                    CustomerName = customerName?.Trim()
                        ?? "Unknown",
                    CustomerPhone = customerPhone?.Trim()
                        ?? "N/A",
                    CustomerIDNumber = customerIDNumber,
                    OldSimNumber = oldSimNumber,
                    NewSimNumber = newSimNumber,
                    ChargeAmount = sim.SellingPrice,
                    PaymentMethod = paymentMethod,
                    MpesaCode = sim.MpesaCode,
                    CreatedAt = DateTime.Now
                });
            }

            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                AuditAction.SaleCreated,
                "SimCards",
                $"{sim.Network} SIM " +
                $"{(isReplacement ? "replacement" : "sale")} " +
                $"to {customerName}. " +
                $"KES {sim.SellingPrice:N0}.",
                recordType: "SimCard",
                recordID: sim.SimCardID.ToString());

            TempData["Success"] =
                $"{sim.Network} SIM " +
                $"{(isReplacement ? "replacement" : "sold")} " +
                $"to {customerName}. " +
                $"KES {sim.SellingPrice:N0}";
            return RedirectToAction("Index");
        }
    }
}