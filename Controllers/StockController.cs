using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BraysTech.Data;
using BraysTech.Models;
using BraysTech.Services;
using System.Security.Claims;

namespace BraysTech.Controllers
{
    [Authorize]
    public class StockController : Controller
    {
        private readonly AppDbContext _db;
        private readonly AuditService _audit;

        public StockController(
            AppDbContext db, AuditService audit)
        {
            _db = db;
            _audit = audit;
        }

        // ── PHONE STOCK INDEX ──────────────────────────────
        public async Task<IActionResult> Index(
            string? brand, string? status,
            int? branchID, string? search)
        {
            var currentUserID = User
                .FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("Manager");
            var isSalesperson = !isAdmin && !isManager;

            var query = _db.IMEIStock
                .Include(i => i.Branch)
                .AsQueryable();

            if (!string.IsNullOrEmpty(brand))
                query = query.Where(i =>
                    i.Brand != null &&
                    i.Brand.ToLower() == brand.ToLower());

            if (!string.IsNullOrEmpty(status) &&
                Enum.TryParse<PhoneStatus>(
                    status, out var ps))
                query = query.Where(i =>
                    i.Status == ps);

            if (branchID.HasValue)
                query = query.Where(i =>
                    i.BranchID == branchID);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(i =>
                    i.IMEI.Contains(search) ||
                    i.PhoneName.Contains(search) ||
                    (i.Brand != null &&
                     i.Brand.Contains(search)) ||
                    (i.Model != null &&
                     i.Model.Contains(search)));

            var stock = await query
                .OrderByDescending(i => i.DateAdded)
                .ToListAsync();

            // Stat cards
            ViewBag.TotalInStock = await _db.IMEIStock
                .CountAsync(i =>
                    i.Status == PhoneStatus.InStock);
            ViewBag.TotalSold = await _db.IMEIStock
                .CountAsync(i =>
                    i.Status == PhoneStatus.Sold);
            ViewBag.TotalFaulty = await _db.IMEIStock
                .CountAsync(i =>
                    i.Status == PhoneStatus.Faulty);
            ViewBag.TotalDisplay = await _db.IMEIStock
                .CountAsync(i =>
                    i.Status == PhoneStatus.DisplayUnit);

            // Stock value — hide from salespersons
            if (!isSalesperson)
            {
                var inStockItems = await _db.IMEIStock
                    .Where(i =>
                        i.Status == PhoneStatus.InStock)
                    .ToListAsync();
                ViewBag.StockValue =
                    inStockItems.Sum(i => i.BuyingPrice);
                ViewBag.PotentialRevenue =
                    inStockItems.Sum(i => i.SellingPrice);
            }

            ViewBag.IsAdminOrManager =
                isAdmin || isManager;

            // Filter options
            ViewBag.Brands = await _db.IMEIStock
                .Where(i => i.Brand != null)
                .Select(i => i.Brand)
                .Distinct()
                .ToListAsync();
            ViewBag.Branches = await _db.Branches
                .Where(b => b.IsActive).ToListAsync();

            ViewBag.SelectedBrand = brand;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedBranch = branchID;
            ViewBag.Search = search;

            return View(stock);
        }

        // ── ADD DEVICE ─────────────────────────────────────
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Add()
        {
            ViewBag.Branches = await _db.Branches
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .ToListAsync();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Add(
            string IMEI, string PhoneName,
            string? Brand, string? Model,
            string? Color, string? Storage,
            decimal BuyingPrice, decimal SellingPrice,
            string? SupplierName, string? Notes,
            int BranchID)
        {
            if (BranchID == 0)
            {
                TempData["Error"] =
                    "Please select a branch.";
                ViewBag.Branches = await _db.Branches
                    .Where(b => b.IsActive)
                    .ToListAsync();
                return View();
            }

            var branchExists = await _db.Branches
                .AnyAsync(b => b.BranchID == BranchID);
            if (!branchExists)
            {
                TempData["Error"] =
                    $"Branch ID {BranchID} not found.";
                ViewBag.Branches = await _db.Branches
                    .Where(b => b.IsActive)
                    .ToListAsync();
                return View();
            }

            var exists = await _db.IMEIStock
                .AnyAsync(i => i.IMEI == IMEI.Trim());
            if (exists)
            {
                TempData["Error"] =
                    $"IMEI {IMEI} already exists.";
                ViewBag.Branches = await _db.Branches
                    .Where(b => b.IsActive)
                    .ToListAsync();
                return View();
            }

            var device = new IMEIStock
            {
                IMEI = IMEI.Trim(),
                PhoneName = PhoneName.Trim(),
                Brand = Brand?.Trim(),
                Model = Model?.Trim(),
                Color = Color?.Trim(),
                Storage = Storage?.Trim(),
                BuyingPrice = BuyingPrice,
                SellingPrice = SellingPrice,
                SupplierName = SupplierName?.Trim(),
                Notes = Notes?.Trim(),
                BranchID = BranchID,
                Status = PhoneStatus.InStock,
                DateAdded = DateTime.Now
            };

            _db.IMEIStock.Add(device);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                AuditAction.StockAdded,
                "Inventory",
                $"New device: {device.PhoneName} " +
                $"IMEI: {device.IMEI}. " +
                $"Buy: KES {device.BuyingPrice:N0}. " +
                $"Sell: KES {device.SellingPrice:N0}.",
                recordType: "IMEIStock",
                recordID: device.StockID.ToString());

            TempData["Success"] =
                $"{device.PhoneName} " +
                $"(IMEI: {device.IMEI}) added.";
            return RedirectToAction("Index");
        }

        // ── DEVICE DETAILS ─────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var device = await _db.IMEIStock
                .Include(i => i.Branch)
                .FirstOrDefaultAsync(i =>
                    i.StockID == id);

            if (device == null) return NotFound();
            return View(device);
        }

        // ── EDIT DEVICE ────────────────────────────────────
        // Admin and Manager can edit any phone
        // including Sold phones to fix data entry errors
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int id)
        {
            var device = await _db.IMEIStock
                .Include(i => i.Branch)
                .FirstOrDefaultAsync(i =>
                    i.StockID == id);

            if (device == null) return NotFound();

            ViewBag.Branches = await _db.Branches
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .ToListAsync();

            return View(device);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            IMEIStock device)
        {
            ModelState.Remove("Branch");

            if (device.BranchID <= 0)
            {
                TempData["Error"] =
                    "Please select a valid branch.";
                ViewBag.Branches = await _db.Branches
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.Name)
                    .ToListAsync();
                return View(device);
            }

            var existing = await _db.IMEIStock
                .FindAsync(device.StockID);

            if (existing == null) return NotFound();

            // Update editable fields only
            // Status, DateAdded, DateSold,
            // FaultReason etc are preserved
            existing.PhoneName = device.PhoneName;
            existing.Brand = device.Brand;
            existing.Model = device.Model;
            existing.Color = device.Color;
            existing.Storage = device.Storage;
            existing.BuyingPrice = device.BuyingPrice;
            existing.SellingPrice = device.SellingPrice;
            existing.SupplierName = device.SupplierName;
            existing.Notes = device.Notes;
            existing.BranchID = device.BranchID;

            // Admin only can correct a wrong IMEI
            // First check the new IMEI is not already
            // taken by another device
            if (User.IsInRole("Admin") &&
                !string.IsNullOrEmpty(device.IMEI) &&
                device.IMEI.Trim() != existing.IMEI)
            {
                var imeiTaken = await _db.IMEIStock
                    .AnyAsync(i =>
                        i.IMEI == device.IMEI.Trim() &&
                        i.StockID != existing.StockID);

                if (imeiTaken)
                {
                    TempData["Error"] =
                        $"IMEI {device.IMEI} is already " +
                        $"assigned to another device.";
                    ViewBag.Branches = await _db.Branches
                        .Where(b => b.IsActive)
                        .OrderBy(b => b.Name)
                        .ToListAsync();
                    return View(device);
                }

                existing.IMEI = device.IMEI.Trim();
            }

            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                AuditAction.StockEdited,
                "Inventory",
                $"{existing.PhoneName} " +
                $"(IMEI: {existing.IMEI}) edited.",
                recordType: "IMEIStock",
                recordID: existing.StockID.ToString());

            TempData["Success"] =
                "Device updated successfully.";
            return RedirectToAction("Index");
        }

        // ── MARK FAULTY ────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> MarkFaulty(
            int id)
        {
            var device = await _db.IMEIStock
                .Include(i => i.Branch)
                .FirstOrDefaultAsync(i =>
                    i.StockID == id);
            if (device == null) return NotFound();
            return View(device);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFaulty(
            int id, string faultReason,
            string? technicianNotes,
            bool warrantyClaim)
        {
            var device = await _db.IMEIStock
                .FindAsync(id);
            if (device == null) return NotFound();

            device.Status = PhoneStatus.Faulty;
            device.FaultReason = faultReason;
            device.DateMarkedFaulty = DateTime.Now;
            device.TechnicianNotes = technicianNotes;
            device.WarrantyClaim = warrantyClaim;
            device.RepairStatus = "Pending";

            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                AuditAction.StockMarkedFaulty,
                "Inventory",
                $"{device.PhoneName} " +
                $"(IMEI: {device.IMEI}) marked faulty. " +
                $"Reason: {faultReason}.",
                oldValue: "InStock",
                newValue: "Faulty",
                recordType: "IMEIStock",
                recordID: id.ToString());

            TempData["Success"] =
                $"{device.PhoneName} marked as faulty.";
            return RedirectToAction("Faulty");
        }

        // ── UPDATE REPAIR STATUS ───────────────────────────
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRepairStatus(
            int id, string repairStatus,
            string? technicianNotes)
        {
            var device = await _db.IMEIStock
                .FindAsync(id);
            if (device == null) return NotFound();

            device.RepairStatus = repairStatus;
            device.TechnicianNotes = technicianNotes;

            if (repairStatus == "Repaired")
            {
                device.Status = PhoneStatus.InStock;
                device.FaultReason = null;
                device.DateMarkedFaulty = null;
                device.RepairStatus = "Completed";
            }

            await _db.SaveChangesAsync();

            TempData["Success"] =
                $"Repair status updated for " +
                $"{device.PhoneName}.";
            return RedirectToAction("Faulty");
        }

        // ── MARK AS DISPLAY ────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkDisplay(
            int id)
        {
            var device = await _db.IMEIStock
                .FindAsync(id);
            if (device == null) return NotFound();

            device.Status =
                device.Status ==
                    PhoneStatus.DisplayUnit
                ? PhoneStatus.InStock
                : PhoneStatus.DisplayUnit;

            await _db.SaveChangesAsync();

            TempData["Success"] =
                device.Status ==
                    PhoneStatus.DisplayUnit
                ? $"{device.PhoneName} " +
                  $"set as display unit."
                : $"{device.PhoneName} " +
                  $"restored to stock.";

            return RedirectToAction("Index");
        }

        // ── FAULTY DEVICES PAGE ────────────────────────────
        public async Task<IActionResult> Faulty()
        {
            var devices = await _db.IMEIStock
                .Include(i => i.Branch)
                .Where(i =>
                    i.Status == PhoneStatus.Faulty)
                .OrderByDescending(i =>
                    i.DateMarkedFaulty)
                .ToListAsync();

            ViewBag.TotalFaulty = devices.Count;
            ViewBag.WarrantyClaims =
                devices.Count(d => d.WarrantyClaim);
            ViewBag.TotalLoss =
                devices.Sum(d => d.BuyingPrice);

            return View(devices);
        }

        // ── DISPLAY PHONES PAGE ────────────────────────────
        public async Task<IActionResult> Display()
        {
            var devices = await _db.IMEIStock
                .Include(i => i.Branch)
                .Where(i =>
                    i.Status == PhoneStatus.DisplayUnit)
                .OrderByDescending(i => i.DateAdded)
                .ToListAsync();

            return View(devices);
        }
    }
}