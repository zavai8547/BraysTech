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
    public class SalesController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly AuditService _audit;

        public SalesController(
            AppDbContext db,
            UserManager<AppUser> userManager,
            AuditService audit)
        {
            _db = db;
            _userManager = userManager;
            _audit = audit;
        }

        public async Task<IActionResult> Index(
            int? branchID, string? staffID,
            string? payment, DateTime? from,
            DateTime? to, string? search)
        {
            var currentUserID = User
                .FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _userManager
                .FindByIdAsync(currentUserID!);
            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("Manager");

            var query = _db.PhoneSales
                .Include(s => s.Staff)
                .Include(s => s.Branch)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Phone)
                .AsQueryable();

            if (!isAdmin && !isManager)
                query = query.Where(s =>
                    s.StaffID == currentUserID);
            else if (isManager &&
                     currentUser?.BranchID != null)
                query = query.Where(s =>
                    s.BranchID == currentUser.BranchID);

            if (branchID.HasValue)
                query = query.Where(s =>
                    s.BranchID == branchID);
            if (!string.IsNullOrEmpty(staffID))
                query = query.Where(s =>
                    s.StaffID == staffID);
            if (!string.IsNullOrEmpty(payment) &&
                Enum.TryParse<SalePaymentMethod>(
                    payment, out var pm))
                query = query.Where(s =>
                    s.PaymentMethod == pm);
            if (from.HasValue)
                query = query.Where(s =>
                    s.CreatedAt.Date >= from.Value.Date);
            if (to.HasValue)
                query = query.Where(s =>
                    s.CreatedAt.Date <= to.Value.Date);
            if (!string.IsNullOrEmpty(search))
                query = query.Where(s =>
                    (s.CustomerName != null &&
                     s.CustomerName.Contains(search)) ||
                    (s.CustomerPhone != null &&
                     s.CustomerPhone.Contains(search)) ||
                    s.Items.Any(i =>
                        i.IMEI.Contains(search) ||
                        i.PhoneName.Contains(search)));

            var sales = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            ViewBag.TotalSales = sales.Count;
            ViewBag.TotalRevenue =
                sales.Sum(s => s.TotalAmount);
            ViewBag.TotalProfit =
                sales.Sum(s => s.TotalProfit);
            ViewBag.Branches = await _db.Branches
                .Where(b => b.IsActive).ToListAsync();
            ViewBag.Staff = await _userManager.Users
                .Where(u => u.IsActive).ToListAsync();
            ViewBag.SelectedBranch = branchID;
            ViewBag.SelectedStaff = staffID;
            ViewBag.SelectedPayment = payment;
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");
            ViewBag.Search = search;
            ViewBag.IsAdmin = isAdmin;
            ViewBag.IsManager = isManager;

            return View(sales);
        }

        public async Task<IActionResult> Details(int id)
        {
            var sale = await _db.PhoneSales
                .Include(s => s.Staff)
                .Include(s => s.Branch)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Phone)
                .FirstOrDefaultAsync(s => s.SaleID == id);

            if (sale == null) return NotFound();

            var currentUserID = User
                .FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Admin") &&
                !User.IsInRole("Manager") &&
                sale.StaffID != currentUserID)
                return Forbid();

            return View(sale);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var sale = await _db.PhoneSales
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.SaleID == id);

            if (sale == null) return NotFound();

            var saleID = sale.SaleID;
            var totalAmount = sale.TotalAmount;
            var customerName = sale.CustomerName ?? "Walk-in";

            // Load and reset each phone directly
            // Do NOT rely on item.Phone navigation property
            // — load each phone explicitly by StockID
            foreach (var item in sale.Items)
            {
                var phone = await _db.IMEIStock
                    .FindAsync(item.StockID);

                if (phone != null)
                {
                    phone.Status = PhoneStatus.InStock;
                    phone.DateSold = null;
                }
            }

            // Fix customer totals
            if (sale.CustomerID.HasValue)
            {
                var customer = await _db.Customers
                    .FindAsync(sale.CustomerID.Value);
                if (customer != null)
                {
                    customer.TotalPurchases = Math.Max(0,
                        customer.TotalPurchases -
                        sale.Items.Count);
                    customer.TotalSpent = Math.Max(0,
                        customer.TotalSpent - totalAmount);
                }
            }

            _db.PhoneSaleItems.RemoveRange(sale.Items);
            _db.PhoneSales.Remove(sale);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                AuditAction.SaleDeleted,
                "Sales",
                $"Sale #{saleID} deleted. " +
                $"KES {totalAmount:N0}. " +
                $"Customer: {customerName}. " +
                $"Stock restored for {sale.Items.Count} " +
                $"device(s).",
                recordType: "PhoneSale",
                recordID: saleID.ToString());

            TempData["Success"] =
                $"Sale #{saleID} deleted and " +
                $"{sale.Items.Count} phone(s) " +
                $"returned to stock.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> New()
        {
            var currentUserID = User
                .FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _userManager
                .FindByIdAsync(currentUserID!);
            var isManager = User.IsInRole("Manager");

            if (isManager && currentUser?.BranchID != null)
                ViewBag.Branches = await _db.Branches
                    .Where(b => b.IsActive &&
                                b.BranchID ==
                                    currentUser.BranchID)
                    .ToListAsync();
            else
                ViewBag.Branches = await _db.Branches
                    .Where(b => b.IsActive).ToListAsync();

            return View();
        }

        // ── BUG 2 FIX ─────────────────────────────────────
        // Added customerPhone trim and null safety.
        // CustomerPhone is now always saved even for
        // walk-in customers who have a phone number.
        [HttpPost]
        public async Task<IActionResult> New(
            string? customerName,
            string? customerPhone,
            int? customerID,
            List<int> stockIDs,
            List<decimal> customPrices,
            List<decimal> discounts,
            SalePaymentMethod paymentMethod,
            string? mpesaCode,
            int branchID,
            string? notes)
        {
            if (stockIDs == null || !stockIDs.Any())
            {
                TempData["Error"] =
                    "Add at least one device.";
                ViewBag.Branches = await _db.Branches
                    .Where(b => b.IsActive).ToListAsync();
                return View();
            }

            // Clean string inputs — this was the root cause
            // of customerPhone arriving as null.
            // Trim whitespace and treat empty string as null.
            customerName = string.IsNullOrWhiteSpace(
                customerName)
                ? null : customerName.Trim();
            customerPhone = string.IsNullOrWhiteSpace(
                customerPhone)
                ? null : customerPhone.Trim();

            var staffID = User
                .FindFirstValue(ClaimTypes.NameIdentifier);
            var saleItems = new List<PhoneSaleItem>();
            decimal totalAmount = 0;
            decimal totalProfit = 0;

            for (int i = 0; i < stockIDs.Count; i++)
            {
                var phone = await _db.IMEIStock
                    .FirstOrDefaultAsync(p =>
                        p.StockID == stockIDs[i] &&
                        p.Status == PhoneStatus.InStock);

                if (phone == null)
                {
                    TempData["Error"] =
                        "One or more devices are no " +
                        "longer available.";
                    ViewBag.Branches = await _db.Branches
                        .Where(b => b.IsActive).ToListAsync();
                    return View();
                }

                var sellingPrice =
                    customPrices != null &&
                    customPrices.Count > i
                        ? customPrices[i]
                        : phone.SellingPrice;
                var discount =
                    discounts != null &&
                    discounts.Count > i
                        ? discounts[i] : 0;
                var finalPrice = sellingPrice - discount;
                var profit = finalPrice - phone.BuyingPrice;

                totalAmount += finalPrice;
                totalProfit += profit;

                // Log price override if below minimum
                if (finalPrice < phone.SellingPrice)
                {
                    await _audit.LogAsync(
                        AuditAction.PriceOverride,
                        "Sales",
                        $"Price override: " +
                        $"{phone.PhoneName} " +
                        $"IMEI: {phone.IMEI}. " +
                        $"Min: KES {phone.SellingPrice:N0}." +
                        $" Sold: KES {finalPrice:N0}.",
                        oldValue: phone.SellingPrice
                            .ToString("N0"),
                        newValue: finalPrice.ToString("N0"),
                        recordType: "IMEIStock",
                        recordID: phone.StockID.ToString());
                }

                phone.Status = PhoneStatus.Sold;
                phone.DateSold = DateTime.Now;

                saleItems.Add(new PhoneSaleItem
                {
                    StockID = phone.StockID,
                    IMEI = phone.IMEI,
                    PhoneName = phone.PhoneName,
                    BuyingPrice = phone.BuyingPrice,
                    SellingPrice = finalPrice,
                    Profit = profit
                });
            }

            // Handle customer linking
            int? resolvedCustomerID = customerID;
            string resolvedCustomerName =
                customerName ?? "Walk-in";

            if (resolvedCustomerID == null &&
                !string.IsNullOrEmpty(customerPhone))
            {
                // Look for existing customer by phone
                var existing = await _db.Customers
                    .FirstOrDefaultAsync(c =>
                        c.Phone == customerPhone);

                if (existing != null)
                {
                    resolvedCustomerID =
                        existing.CustomerID;
                    existing.TotalPurchases +=
                        saleItems.Count;
                    existing.TotalSpent += totalAmount;
                    resolvedCustomerName = existing.FullName;
                }
                else if (!string.IsNullOrEmpty(
                    customerName))
                {
                    // Create new customer record
                    var newCust = new Customer
                    {
                        FullName = customerName,
                        Phone = customerPhone,
                        TotalPurchases = saleItems.Count,
                        TotalSpent = totalAmount,
                        CreatedAt = DateTime.Now
                    };
                    _db.Customers.Add(newCust);
                    await _db.SaveChangesAsync();
                    resolvedCustomerID =
                        newCust.CustomerID;
                    resolvedCustomerName = newCust.FullName;
                }
            }
            else if (resolvedCustomerID.HasValue)
            {
                var cust = await _db.Customers
                    .FindAsync(resolvedCustomerID.Value);
                if (cust != null)
                {
                    cust.TotalPurchases += saleItems.Count;
                    cust.TotalSpent += totalAmount;
                    resolvedCustomerName = cust.FullName;
                }
            }

            var sale = new PhoneSale
            {
                StaffID = staffID!,
                BranchID = branchID,
                CustomerID = resolvedCustomerID,
                CustomerName = resolvedCustomerName,
                CustomerPhone = customerPhone,
                TotalAmount = totalAmount,
                TotalProfit = totalProfit,
                PaymentMethod = paymentMethod,
                MpesaCode = paymentMethod ==
                    SalePaymentMethod.MPesa
                    ? mpesaCode?.Trim().ToUpper()
                    : null,
                Notes = notes,
                CreatedAt = DateTime.Now
            };

            _db.PhoneSales.Add(sale);
            await _db.SaveChangesAsync();

            foreach (var item in saleItems)
            {
                item.SaleID = sale.SaleID;
                _db.PhoneSaleItems.Add(item);
            }

            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                AuditAction.SaleCreated,
                "Sales",
                $"Sale #{sale.SaleID}. " +
                $"Customer: {resolvedCustomerName}. " +
                $"KES {totalAmount:N0}. " +
                $"Profit: KES {totalProfit:N0}. " +
                $"{saleItems.Count} device(s). " +
                $"Payment: {paymentMethod}.",
                recordType: "PhoneSale",
                recordID: sale.SaleID.ToString());

            TempData["Success"] =
                $"Sale complete. " +
                $"Total: KES {totalAmount:N0} | " +
                $"Profit: KES {totalProfit:N0}";
            return RedirectToAction("Receipt",
                new { id = sale.SaleID });
        }

        public async Task<IActionResult> Receipt(int id)
        {
            var sale = await _db.PhoneSales
                .Include(s => s.Staff)
                .Include(s => s.Branch)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Phone)
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.SaleID == id);

            if (sale == null) return NotFound();

            var currentUserID = User
                .FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Admin") &&
                !User.IsInRole("Manager") &&
                sale.StaffID != currentUserID)
                return Forbid();

            return View(sale);
        }

        [HttpGet]
        public async Task<IActionResult> SearchDevice(
            string q)
        {
            if (string.IsNullOrEmpty(q) || q.Length < 2)
                return Json(new List<object>());

            var currentUserID = User
                .FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _userManager
                .FindByIdAsync(currentUserID!);
            var isAdmin = User.IsInRole("Admin");

            var query = _db.IMEIStock
                .Include(p => p.Branch)
                .Where(p =>
                    p.Status == PhoneStatus.InStock &&
                    (p.IMEI.Contains(q) ||
                     p.PhoneName.Contains(q) ||
                     (p.Brand != null &&
                      p.Brand.Contains(q)) ||
                     (p.Model != null &&
                      p.Model.Contains(q))));

            if (!isAdmin && currentUser?.BranchID != null)
                query = query.Where(p =>
                    p.BranchID == currentUser.BranchID);

            var results = await query
                .Take(8)
                .Select(p => new
                {
                    p.StockID,
                    p.IMEI,
                    p.PhoneName,
                    p.Brand,
                    p.Model,
                    p.Color,
                    p.Storage,
                    p.BuyingPrice,
                    p.SellingPrice,
                    BranchName = p.Branch!.Name
                })
                .ToListAsync();

            return Json(results);
        }

        [HttpGet]
        public async Task<IActionResult> SearchCustomer(
            string q)
        {
            if (string.IsNullOrEmpty(q) || q.Length < 2)
                return Json(new List<object>());

            var results = await _db.Customers
                .Where(c =>
                    c.FullName.Contains(q) ||
                    c.Phone.Contains(q))
                .Take(10)
                .Select(c => new
                {
                    c.CustomerID,
                    c.FullName,
                    c.Phone,
                    c.TotalPurchases,
                    c.TotalSpent
                })
                .ToListAsync();

            return Json(results);
        }
    }
}