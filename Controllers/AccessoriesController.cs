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
    public class AccessoriesController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly AuditService _audit;

        public AccessoriesController(
            AppDbContext db,
            UserManager<AppUser> userManager,
            AuditService audit)
        {
            _db = db;
            _userManager = userManager;
            _audit = audit;
        }

        // ── STOCK INDEX ────────────────────────────────
        public async Task<IActionResult> Index(
            string? category, int? branchID,
            string? search)
        {
            var query = _db.Accessories
                .Include(a => a.Branch)
                .Where(a => a.IsActive)
                .AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(a => a.Category == category);
            if (branchID.HasValue)
                query = query.Where(a => a.BranchID == branchID);
            if (!string.IsNullOrEmpty(search))
                query = query.Where(a =>
                    a.Name.Contains(search) ||
                    (a.Brand != null && a.Brand.Contains(search)));

            var items = await query
                .OrderBy(a => a.Category)
                .ThenBy(a => a.Name)
                .ToListAsync();

            ViewBag.TotalItems = items.Count;
            ViewBag.TotalValue = items.Sum(a => a.BuyingPrice * a.CurrentStock);
            ViewBag.LowStock = items.Count(a => a.CurrentStock <= a.LowStockAlert);
            ViewBag.Categories = await _db.Accessories
                .Where(a => a.Category != null)
                .Select(a => a.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
            ViewBag.Branches = await _db.Branches
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .ToListAsync();
            ViewBag.SelectedCategory = category;
            ViewBag.SelectedBranch = branchID;
            ViewBag.Search = search;

            return View(items);
        }

        // ── ADD ACCESSORY ──────────────────────────────
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
        public async Task<IActionResult> Add(Accessory model)
        {
            if (model.BranchID == 0)
            {
                ModelState.AddModelError("BranchID", "Please select a branch.");
                ViewBag.Branches = await _db.Branches
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.Name)
                    .ToListAsync();
                return View(model);
            }

            if (ModelState.IsValid)
            {
                model.DateAdded = DateTime.Now;
                model.IsActive = true;
                _db.Accessories.Add(model);
                await _db.SaveChangesAsync();

                await _audit.LogAsync(
                    AuditAction.StockAdded,
                    "Accessories",
                    $"Accessory added: {model.Name}. " +
                    $"Stock: {model.CurrentStock}. " +
                    $"Buy: KES {model.BuyingPrice:N0}. " +
                    $"Sell: KES {model.SellingPrice:N0}.",
                    recordType: "Accessory",
                    recordID: model.AccessoryID.ToString());

                TempData["Success"] = $"{model.Name} added to accessories stock.";
                return RedirectToAction("Index");
            }

            ViewBag.Branches = await _db.Branches
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .ToListAsync();
            return View(model);
        }

        // ── EDIT ACCESSORY ─────────────────────────────
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _db.Accessories.FindAsync(id);
            if (item == null) return NotFound();
            ViewBag.Branches = await _db.Branches
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .ToListAsync();
            return View(item);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(Accessory model)
        {
            // Remove navigation properties from validation
            ModelState.Remove("Branch");
            ModelState.Remove("SaleItems");

            if (model.BranchID == 0)
            {
                TempData["Error"] = "Please select a branch.";
                ViewBag.Branches = await _db.Branches
                    .Where(b => b.IsActive).ToListAsync();
                return View(model);
            }

            // Load existing record and update fields manually
            // to avoid overwriting CurrentStock or DateAdded
            var existing = await _db.Accessories
                .FindAsync(model.AccessoryID);

            if (existing == null) return NotFound();

            existing.Name = model.Name;
            existing.Category = model.Category;
            existing.Brand = model.Brand;
            existing.Description = model.Description;
            existing.BuyingPrice = model.BuyingPrice;
            existing.SellingPrice = model.SellingPrice;
            existing.LowStockAlert = model.LowStockAlert;
            existing.BranchID = model.BranchID;
            existing.SupplierName = model.SupplierName;
            // CurrentStock and DateAdded are NOT updated here
            // they are managed by Add and Restock actions

            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                AuditAction.StockEdited,
                "Accessories",
                $"Accessory updated: {existing.Name}. " +
                $"Sell: KES {existing.SellingPrice:N0}.",
                recordType: "Accessory",
                recordID: existing.AccessoryID.ToString());

            TempData["Success"] =
                $"{existing.Name} updated successfully.";
            return RedirectToAction("Index");
        }

        // ── RESTOCK ────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Restock(int id, int quantity, string? notes)
        {
            if (quantity <= 0)
            {
                TempData["Error"] = "Quantity must be greater than 0.";
                return RedirectToAction("Index");
            }

            var item = await _db.Accessories.FindAsync(id);
            if (item == null) return NotFound();

            int oldStock = item.CurrentStock;
            item.CurrentStock += quantity;
            await _db.SaveChangesAsync();

            // BUG FIX: Resolved undefined enum issue
            await _audit.LogAsync(
                AuditAction.StockAdded,
                "Accessories",
                $"Restocked {item.Name}: Added {quantity} units. " +
                $"Stock: {oldStock} → {item.CurrentStock}. Notes: {notes ?? "N/A"}",
                recordType: "Accessory",
                recordID: item.AccessoryID.ToString());

            TempData["Success"] = $"Added {quantity} units to {item.Name}. New stock: {item.CurrentStock}.";
            return RedirectToAction("Index");
        }

        // ── NEW SALE ───────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> NewSale()
        {
            var currentUserID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _userManager.FindByIdAsync(currentUserID!);
            var isManager = User.IsInRole("Manager");
            var isAdmin = User.IsInRole("Admin");

            var query = _db.Accessories
                .Include(a => a.Branch)
                .Where(a => a.IsActive && a.CurrentStock > 0);

            if (!isAdmin && isManager && currentUser?.BranchID != null)
                query = query.Where(a => a.BranchID == currentUser.BranchID);

            ViewBag.Accessories = await query
                .OrderBy(a => a.Category)
                .ThenBy(a => a.Name)
                .ToListAsync();

            ViewBag.Branches = await _db.Branches
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> NewSale(
            List<int> accessoryIDs,
            List<int> quantities,
            List<decimal> customPrices,
            SalePaymentMethod paymentMethod,
            string? mpesaCode,
            string? customerName,
            string? customerPhone,
            int branchID,
            string? notes)
        {
            if (accessoryIDs == null || !accessoryIDs.Any())
            {
                TempData["Error"] = "Add at least one item.";
                return RedirectToAction("NewSale");
            }

            if (branchID == 0)
            {
                TempData["Error"] = "Please select a branch.";
                return RedirectToAction("NewSale");
            }

            if (paymentMethod == SalePaymentMethod.MPesa && string.IsNullOrWhiteSpace(mpesaCode))
            {
                TempData["Error"] = "M-Pesa code is required for M-Pesa payments.";
                return RedirectToAction("NewSale");
            }

            var staffID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var saleItems = new List<AccessorySaleItem>();
            decimal totalAmount = 0;
            decimal totalProfit = 0;

            for (int i = 0; i < accessoryIDs.Count; i++)
            {
                var acc = await _db.Accessories.FindAsync(accessoryIDs[i]);
                if (acc == null) continue;

                int qty = quantities.Count > i ? quantities[i] : 1;
                if (qty <= 0 || qty > acc.CurrentStock) continue;

                decimal price = customPrices.Count > i && customPrices[i] > 0
                    ? customPrices[i]
                    : acc.SellingPrice;

                decimal subtotal = price * qty;
                decimal profit = (price - acc.BuyingPrice) * qty;

                totalAmount += subtotal;
                totalProfit += profit;

                acc.CurrentStock -= qty;

                saleItems.Add(new AccessorySaleItem
                {
                    AccessoryID = acc.AccessoryID,
                    AccessoryName = acc.Name,
                    Quantity = qty,
                    UnitPrice = price,
                    BuyingPrice = acc.BuyingPrice,
                    Subtotal = subtotal,
                    Profit = profit
                });
            }

            // Auto-create customer
            int? custID = null;
            if (!string.IsNullOrEmpty(customerPhone))
            {
                var existing = await _db.Customers
                    .FirstOrDefaultAsync(c => c.Phone == customerPhone);
                if (existing != null)
                {
                    custID = existing.CustomerID;
                    existing.TotalPurchases += saleItems.Count;
                    existing.TotalSpent += totalAmount;
                    // BUG FIX: Removed 'LastPurchase' assignment to align with your Customer Model definition
                }
                else if (!string.IsNullOrEmpty(customerName))
                {
                    var newCust = new Customer
                    {
                        FullName = customerName,
                        Phone = customerPhone,
                        TotalPurchases = saleItems.Count,
                        TotalSpent = totalAmount,
                        CreatedAt = DateTime.Now
                        // BUG FIX: Removed 'LastPurchase' assignment to align with your Customer Model definition
                    };
                    _db.Customers.Add(newCust);
                    await _db.SaveChangesAsync();
                    custID = newCust.CustomerID;
                }
            }

            var sale = new AccessorySale
            {
                StaffID = staffID!,
                BranchID = branchID,
                CustomerName = customerName ?? "Walk-in",
                CustomerPhone = customerPhone,
                TotalAmount = totalAmount,
                TotalProfit = totalProfit,
                PaymentMethod = paymentMethod,
                MpesaCode = paymentMethod == SalePaymentMethod.MPesa
                    ? mpesaCode?.Trim().ToUpper()
                    : null,
                Notes = notes,
                CreatedAt = DateTime.Now
            };

            _db.AccessorySales.Add(sale);
            await _db.SaveChangesAsync();

            foreach (var item in saleItems)
            {
                item.SaleID = sale.SaleID;
                _db.AccessorySaleItems.Add(item);
            }

            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                AuditAction.SaleCreated,
                "Accessories",
                $"Accessory sale #{sale.SaleID}. " +
                $"Customer: {sale.CustomerName}. " +
                $"Items: {saleItems.Count}. " +
                $"Total: KES {totalAmount:N0}. " +
                $"Profit: KES {totalProfit:N0}. " +
                $"Payment: {paymentMethod}",
                recordType: "AccessorySale",
                recordID: sale.SaleID.ToString());

            TempData["Success"] = $"Sale complete. Total: KES {totalAmount:N0}";
            return RedirectToAction("Sales");
        }

        // ── SALES HISTORY ──────────────────────────────
        public async Task<IActionResult> Sales()
        {
            var currentUserID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("Manager");
            var currentUser = await _userManager.FindByIdAsync(currentUserID!);

            var query = _db.AccessorySales
                .Include(s => s.Staff)
                .Include(s => s.Branch)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Accessory)
                .AsQueryable();

            if (!isAdmin && !isManager)
                query = query.Where(s => s.StaffID == currentUserID);
            else if (isManager && currentUser?.BranchID != null)
                query = query.Where(s => s.BranchID == currentUser.BranchID);

            var sales = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            ViewBag.TotalRevenue = sales.Sum(s => s.TotalAmount);
            ViewBag.TotalProfit = sales.Sum(s => s.TotalProfit);
            ViewBag.TotalSales = sales.Count;

            return View(sales);
        }

        // ── SALE DETAILS ───────────────────────────────
        public async Task<IActionResult> SaleDetails(int id)
        {
            var sale = await _db.AccessorySales
                .Include(s => s.Staff)
                .Include(s => s.Branch)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Accessory)
                .FirstOrDefaultAsync(s => s.SaleID == id);

            if (sale == null) return NotFound();

            // Authorization check
            var currentUserID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("Manager");
            var currentUser = await _userManager.FindByIdAsync(currentUserID!);

            if (!isAdmin && !isManager && sale.StaffID != currentUserID)
            {
                TempData["Error"] = "You don't have permission to view this sale.";
                return RedirectToAction("Sales");
            }

            if (isManager && currentUser?.BranchID != null && sale.BranchID != currentUser.BranchID)
            {
                TempData["Error"] = "You can only view sales from your branch.";
                return RedirectToAction("Sales");
            }

            return View(sale);
        }

        // ── RECEIPT ────────────────────────────────────
        public async Task<IActionResult> Receipt(int id)
        {
            var sale = await _db.AccessorySales
                .Include(s => s.Staff)
                .Include(s => s.Branch)
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.SaleID == id);

            if (sale == null) return NotFound();

            // Load settings for receipt header
            // CORRECT — handle nullable SettingValue
            var settings = await _db.Settings
                .ToDictionaryAsync(
                    s => s.SettingKey,
                    s => s.SettingValue ?? string.Empty);
            ViewBag.Settings = settings;

            return View(sale);
        }

        // ── AJAX SEARCH ────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Search(string q)
        {
            if (string.IsNullOrEmpty(q) || q.Length < 2)
                return Json(new List<object>());

            var currentUserID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _userManager.FindByIdAsync(currentUserID!);
            var isAdmin = User.IsInRole("Admin");

            var query = _db.Accessories
                .Include(a => a.Branch)
                .Where(a => a.IsActive &&
                            a.CurrentStock > 0 &&
                            (a.Name.Contains(q) ||
                             (a.Brand != null && a.Brand.Contains(q)) ||
                             (a.Category != null && a.Category.Contains(q))));

            if (!isAdmin && currentUser?.BranchID != null)
                query = query.Where(a => a.BranchID == currentUser.BranchID);

            var results = await query
                .Take(8)
                .Select(a => new
                {
                    a.AccessoryID,
                    a.Name,
                    a.Category,
                    a.Brand,
                    a.CurrentStock,
                    a.SellingPrice,
                    a.BuyingPrice,
                    BranchName = a.Branch!.Name
                })
                .ToListAsync();

            return Json(results);
        }

        // ── DELETE ACCESSORY (Soft Delete) ──────────────
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.Accessories
                .Include(a => a.SaleItems)
                .FirstOrDefaultAsync(a =>
                    a.AccessoryID == id);

            if (item == null) return NotFound();

            // If it has sales history, deactivate instead
            // of deleting to protect records
            if (item.SaleItems.Any())
            {
                item.IsActive = false;
                await _db.SaveChangesAsync();
                TempData["Success"] =
                    $"{item.Name} deactivated. " +
                    $"It has sales history so it cannot " +
                    $"be fully deleted.";
            }
            else
            {
                _db.Accessories.Remove(item);
                await _db.SaveChangesAsync();
                TempData["Success"] =
                    $"{item.Name} deleted successfully.";
            }

            return RedirectToAction("Index");
        }
    }
}