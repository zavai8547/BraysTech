using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BraysTech.Data;
using BraysTech.Models;
using System.Security.Claims;

namespace BraysTech.Controllers
{
    [Authorize]
    public class SalesController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public SalesController(AppDbContext db,
            UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
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

            // Salesperson sees only own sales
            if (!isAdmin && !isManager)
                query = query.Where(s =>
                    s.StaffID == currentUserID);

            // Manager sees only their branch
            else if (isManager && currentUser?.BranchID != null)
                query = query.Where(s =>
                    s.BranchID == currentUser.BranchID);

            // Filters
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

            // Summary stats
            ViewBag.TotalSales = sales.Count;
            ViewBag.TotalRevenue = sales.Sum(s => s.TotalAmount);
            ViewBag.TotalProfit = sales.Sum(s => s.TotalProfit);

            // Filter dropdowns
            ViewBag.Branches = await _db.Branches
                .Where(b => b.IsActive).ToListAsync();
            ViewBag.Staff = await _userManager.Users
                .Where(u => u.IsActive).ToListAsync();

            // Pass filters back to view
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

            // Only admin/manager or the staff who made the sale
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
                    .ThenInclude(i => i.Phone)
                .FirstOrDefaultAsync(s => s.SaleID == id);

            if (sale == null) return NotFound();

            // Restore phone status back to InStock
            foreach (var item in sale.Items)
            {
                if (item.Phone != null)
                {
                    item.Phone.Status = PhoneStatus.InStock;
                    item.Phone.DateSold = null;
                }
            }

            _db.PhoneSaleItems.RemoveRange(sale.Items);
            _db.PhoneSales.Remove(sale);
            await _db.SaveChangesAsync();

            TempData["Success"] =
                $"✅ Sale #{sale.SaleID} deleted and " +
                $"stock restored.";
            return RedirectToAction("Index");
        }

        // ========== NEW SALES ACTIONS ==========

        [HttpGet]
        public async Task<IActionResult> New()
        {
            var currentUser = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("Manager");

            // If manager, only show their branch
            if (isManager && currentUser?.BranchID != null)
            {
                ViewBag.Branches = await _db.Branches
                    .Where(b => b.IsActive && b.BranchID == currentUser.BranchID)
                    .ToListAsync();
            }
            else
            {
                ViewBag.Branches = await _db.Branches
                    .Where(b => b.IsActive).ToListAsync();
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> New(
            string? customerName, string? customerPhone,
            int? customerID, List<int> stockIDs,
            List<decimal> customPrices, List<decimal> discounts,
            SalePaymentMethod paymentMethod, string? mpesaCode,
            int branchID, string? notes)
        {
            if (stockIDs == null || !stockIDs.Any())
            {
                TempData["Error"] = "❌ Add at least one device.";
                return RedirectToAction("New");
            }

            var staffID = User.FindFirstValue(ClaimTypes.NameIdentifier);
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
                        $"❌ Device not available or already sold.";
                    return RedirectToAction("New");
                }

                var sellingPrice = customPrices != null && customPrices.Count > i
                    ? customPrices[i] : phone.SellingPrice;
                var discount = discounts != null && discounts.Count > i
                    ? discounts[i] : 0;
                var finalPrice = sellingPrice - discount;
                var profit = finalPrice - phone.BuyingPrice;

                totalAmount += finalPrice;
                totalProfit += profit;

                // Mark phone as sold
                phone.Status = PhoneStatus.Sold;
                phone.DateSold = DateTime.Now;

                saleItems.Add(new PhoneSaleItem
                {
                    StockID = phone.StockID,
                    IMEI = phone.IMEI,
                    PhoneName = $"{phone.Brand} {phone.Model}".Trim(),
                    BuyingPrice = phone.BuyingPrice,
                    SellingPrice = finalPrice,
                    Profit = profit
                });
            }

            // Handle customer
            int? custID = customerID;
            if (custID == null && !string.IsNullOrEmpty(customerPhone))
            {
                var existing = await _db.Customers
                    .FirstOrDefaultAsync(c => c.Phone == customerPhone);
                if (existing != null)
                {
                    custID = existing.CustomerID;
                    existing.TotalPurchases += stockIDs.Count;
                    existing.TotalSpent += totalAmount;
                }
                else if (!string.IsNullOrEmpty(customerName))
                {
                    var newCust = new Customer
                    {
                        FullName = customerName,
                        Phone = customerPhone,
                        TotalPurchases = stockIDs.Count,
                        TotalSpent = totalAmount,
                        CreatedAt = DateTime.Now
                    };
                    _db.Customers.Add(newCust);
                    await _db.SaveChangesAsync();
                    custID = newCust.CustomerID;
                }
            }
            else if (custID.HasValue)
            {
                var cust = await _db.Customers.FindAsync(custID);
                if (cust != null)
                {
                    cust.TotalPurchases += stockIDs.Count;
                    cust.TotalSpent += totalAmount;
                }
            }

            var sale = new PhoneSale
            {
                StaffID = staffID!,
                BranchID = branchID,
                CustomerID = custID,
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                TotalAmount = totalAmount,
                TotalProfit = totalProfit,
                PaymentMethod = paymentMethod,
                MpesaCode = paymentMethod == SalePaymentMethod.MPesa
                    ? mpesaCode?.Trim().ToUpper() : null,
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

            TempData["Success"] =
                $"✅ Sale recorded! Total: KES {totalAmount:N0} | " +
                $"Profit: KES {totalProfit:N0}";
            return RedirectToAction("Receipt", new { id = sale.SaleID });
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

            // Security check
            var currentUserID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager") && sale.StaffID != currentUserID)
                return Forbid();

            return View(sale);
        }

        // AJAX — Search phone by IMEI or name
        [HttpGet]
        public async Task<IActionResult> SearchDevice(string q)
        {
            if (string.IsNullOrEmpty(q) || q.Length < 2)
                return Json(new List<object>());

            var currentUser = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("Manager");

            var query = _db.IMEIStock
                .Include(p => p.Branch)
                .Where(p => p.Status == PhoneStatus.InStock &&
                           (p.IMEI.Contains(q) ||
                            p.PhoneName.Contains(q) ||
                            (p.Brand != null && p.Brand.Contains(q)) ||
                            (p.Model != null && p.Model.Contains(q))));

            // Restrict by branch for non-admin
            if (!isAdmin && currentUser?.BranchID != null)
            {
                query = query.Where(p => p.BranchID == currentUser.BranchID);
            }

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

        // AJAX — Search customers by name or phone
        [HttpGet]
        public async Task<IActionResult> SearchCustomer(string q)
        {
            if (string.IsNullOrEmpty(q) || q.Length < 2)
                return Json(new List<object>());

            var results = await _db.Customers
                .Where(c => c.FullName.Contains(q) || c.Phone.Contains(q))
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