using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BraysTech.Data;
using BraysTech.Models;
using System.Security.Claims;

namespace BraysTech.Controllers
{
    [Authorize]
    public class StockController : Controller
    {
        private readonly AppDbContext _db;
        public StockController(AppDbContext db) { _db = db; }

        // ── PHONE STOCK INDEX ──────────────────────────────
        public async Task<IActionResult> Index(
            string? brand, string? status,
            int? branchID, string? search)
        {
            var query = _db.IMEIStock
                .Include(i => i.Branch)
                .AsQueryable();

            if (!string.IsNullOrEmpty(brand))
                query = query.Where(i =>
                    i.Brand != null &&
                    i.Brand.ToLower() == brand.ToLower());

            if (!string.IsNullOrEmpty(status) &&
                Enum.TryParse<PhoneStatus>(status, out var ps))
                query = query.Where(i => i.Status == ps);

            if (branchID.HasValue)
                query = query.Where(i => i.BranchID == branchID);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(i =>
                    i.IMEI.Contains(search) ||
                    i.PhoneName.Contains(search) ||
                    (i.Brand != null && i.Brand.Contains(search)) ||
                    (i.Model != null && i.Model.Contains(search)));

            var stock = await query
                .OrderByDescending(i => i.DateAdded)
                .ToListAsync();

            // Stats
            ViewBag.TotalInStock = await _db.IMEIStock
                .CountAsync(i => i.Status == PhoneStatus.InStock);
            ViewBag.TotalSold = await _db.IMEIStock
                .CountAsync(i => i.Status == PhoneStatus.Sold);
            ViewBag.TotalFaulty = await _db.IMEIStock
                .CountAsync(i => i.Status == PhoneStatus.Faulty);
            ViewBag.TotalDisplay = await _db.IMEIStock
                .CountAsync(i => i.Status == PhoneStatus.DisplayUnit);

            var inStockItems = await _db.IMEIStock
                .Where(i => i.Status == PhoneStatus.InStock)
                .ToListAsync();
            ViewBag.StockValue = inStockItems.Sum(i => i.BuyingPrice);
            ViewBag.PotentialRevenue = inStockItems.Sum(i => i.SellingPrice);

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
        public async Task<IActionResult> Add()
        {
            ViewBag.Branches = await _db.Branches
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Add(
            string IMEI, string PhoneName, string? Brand,
            string? Model, string? Color, string? Storage,
            decimal BuyingPrice, decimal SellingPrice,
            string? SupplierName, string? Notes, int BranchID)
        {
            // Branch validation
            if (BranchID == 0)
            {
                TempData["Error"] = "❌ Please select a branch.";
                ViewBag.Branches = await _db.Branches
                    .Where(b => b.IsActive).ToListAsync();
                return View();
            }

            var branchExists = await _db.Branches
                .AnyAsync(b => b.BranchID == BranchID);
            if (!branchExists)
            {
                TempData["Error"] =
                    $"❌ Branch ID {BranchID} not found. " +
                    $"Please select a valid branch.";
                ViewBag.Branches = await _db.Branches
                    .Where(b => b.IsActive).ToListAsync();
                return View();
            }

            // IMEI uniqueness
            var exists = await _db.IMEIStock
                .AnyAsync(i => i.IMEI == IMEI.Trim());
            if (exists)
            {
                TempData["Error"] =
                    $"❌ IMEI {IMEI} already exists in the system.";
                ViewBag.Branches = await _db.Branches
                    .Where(b => b.IsActive).ToListAsync();
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

            TempData["Success"] =
                $"✅ {device.PhoneName} (IMEI: {device.IMEI}) added to stock!";
            return RedirectToAction("Index");
        }

        // ── DEVICE DETAILS ─────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var device = await _db.IMEIStock
                .Include(i => i.Branch)
                .FirstOrDefaultAsync(i => i.StockID == id);

            if (device == null) return NotFound();
            return View(device);
        }

        // ── EDIT DEVICE ────────────────────────────────────
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int id)
        {
            var device = await _db.IMEIStock.FindAsync(id);
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
        public async Task<IActionResult> Edit(IMEIStock model)
        {
            // Remove navigation property validation errors
            ModelState.Remove("Branch");

            // Validate branch exists
            if (model.BranchID <= 0)
            {
                TempData["Error"] = "❌ Please select a valid branch.";
                ViewBag.Branches = await _db.Branches
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.Name)
                    .ToListAsync();
                return View(model);
            }

            // Check IMEI uniqueness (excluding current device)
            var existing = await _db.IMEIStock
                .FirstOrDefaultAsync(i =>
                    i.IMEI == model.IMEI &&
                    i.StockID != model.StockID);

            if (existing != null)
            {
                TempData["Error"] = "❌ Another device already has this IMEI.";
                ViewBag.Branches = await _db.Branches
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.Name)
                    .ToListAsync();
                return View(model);
            }

            try
            {
                _db.IMEIStock.Update(model);
                await _db.SaveChangesAsync();

                TempData["Success"] = "✅ Device updated!";
                return RedirectToAction("Index");
            }
            catch (DbUpdateException ex)
            {
                TempData["Error"] = $"❌ Database error: {ex.InnerException?.Message ?? ex.Message}";
                ViewBag.Branches = await _db.Branches
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.Name)
                    .ToListAsync();
                return View(model);
            }
        }

        // ── MARK FAULTY ────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> MarkFaulty(int id)
        {
            var device = await _db.IMEIStock
                .Include(i => i.Branch)
                .FirstOrDefaultAsync(i => i.StockID == id);
            if (device == null) return NotFound();
            return View(device);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFaulty(
            int id, string faultReason,
            string? technicianNotes, bool warrantyClaim)
        {
            var device = await _db.IMEIStock.FindAsync(id);
            if (device == null) return NotFound();

            device.Status = PhoneStatus.Faulty;
            device.FaultReason = faultReason;
            device.DateMarkedFaulty = DateTime.Now;
            device.TechnicianNotes = technicianNotes;
            device.WarrantyClaim = warrantyClaim;
            device.RepairStatus = "Pending";

            await _db.SaveChangesAsync();

            TempData["Success"] = $"⚠️ {device.PhoneName} marked as faulty.";
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
            var device = await _db.IMEIStock.FindAsync(id);
            if (device == null) return NotFound();

            device.RepairStatus = repairStatus;
            device.TechnicianNotes = technicianNotes;

            // If repaired — restore to InStock
            if (repairStatus == "Repaired")
            {
                device.Status = PhoneStatus.InStock;
                device.FaultReason = null;
                device.DateMarkedFaulty = null;
                device.RepairStatus = "Completed";
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = $"✅ Repair status updated for {device.PhoneName}.";
            return RedirectToAction("Faulty");
        }

        // ── MARK AS DISPLAY ────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkDisplay(int id)
        {
            var device = await _db.IMEIStock.FindAsync(id);
            if (device == null) return NotFound();

            device.Status = device.Status == PhoneStatus.DisplayUnit
                ? PhoneStatus.InStock
                : PhoneStatus.DisplayUnit;

            await _db.SaveChangesAsync();

            TempData["Success"] = device.Status == PhoneStatus.DisplayUnit
                ? $"🪟 {device.PhoneName} set as display unit."
                : $"✅ {device.PhoneName} restored to stock.";

            return RedirectToAction("Index");
        }

        // ── FAULTY DEVICES PAGE ────────────────────────────
        public async Task<IActionResult> Faulty()
        {
            var devices = await _db.IMEIStock
                .Include(i => i.Branch)
                .Where(i => i.Status == PhoneStatus.Faulty)
                .OrderByDescending(i => i.DateMarkedFaulty)
                .ToListAsync();

            ViewBag.TotalFaulty = devices.Count;
            ViewBag.WarrantyClaims = devices.Count(d => d.WarrantyClaim);
            ViewBag.TotalLoss = devices.Sum(d => d.BuyingPrice);

            return View(devices);
        }

        // ── DISPLAY PHONES PAGE ────────────────────────────
        public async Task<IActionResult> Display()
        {
            var devices = await _db.IMEIStock
                .Include(i => i.Branch)
                .Where(i => i.Status == PhoneStatus.DisplayUnit)
                .OrderByDescending(i => i.DateAdded)
                .ToListAsync();

            return View(devices);
        }
    }
}