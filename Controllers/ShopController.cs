using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SplashCityCarwash.Data;
using SplashCityCarwash.Models;
using System.Security.Claims;

namespace SplashCityCarwash.Controllers
{
    [Authorize]
    public class ShopController : Controller
    {
        private readonly AppDbContext _db;
        public ShopController(AppDbContext db) { _db = db; }

        // ── DASHBOARD / ENTRY ──────────────────────────
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var firstOfMonth = new DateTime(today.Year, today.Month, 1);

            ViewBag.TotalProducts = await _db.Products.CountAsync(p => p.IsActive);
            ViewBag.LowStock = await _db.Products
                .CountAsync(p => p.IsActive && p.CurrentStock <= p.LowStockAlert);
            ViewBag.RevenueToday = await _db.ShopSales
                .Where(s => s.CreatedAt.Date == today)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;
            ViewBag.ProfitToday = await _db.ShopSales
                .Where(s => s.CreatedAt.Date == today)
                .SumAsync(s => (decimal?)s.TotalProfit) ?? 0;
            ViewBag.RevenueMonth = await _db.ShopSales
                .Where(s => s.CreatedAt >= firstOfMonth)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;
            ViewBag.ProfitMonth = await _db.ShopSales
                .Where(s => s.CreatedAt >= firstOfMonth)
                .SumAsync(s => (decimal?)s.TotalProfit) ?? 0;

            var recentSales = await _db.ShopSales
                .Include(s => s.Staff)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentSales = recentSales;

            var lowStockProducts = await _db.Products
                .Where(p => p.IsActive && p.CurrentStock <= p.LowStockAlert)
                .OrderBy(p => p.CurrentStock)
                .ToListAsync();

            ViewBag.LowStockProducts = lowStockProducts;

            return View();
        }

        // ── PRODUCTS ───────────────────────────────────
        public async Task<IActionResult> Products()
        {
            var products = await _db.Products
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToListAsync();
            return View(products);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult CreateProduct() => View();

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> CreateProduct(Product model)
        {
            ModelState.Remove("StockMovements");
            ModelState.Remove("ShopSaleItems");

            if (!ModelState.IsValid) return View(model);

            _db.Products.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"✅ Product '{model.Name}' added!";
            return RedirectToAction("Products");
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> EditProduct(Product model)
        {
            ModelState.Remove("StockMovements");
            ModelState.Remove("ShopSaleItems");

            if (!ModelState.IsValid) return View(model);

            var product = await _db.Products.FindAsync(model.ProductID);
            if (product == null) return NotFound();

            product.Name = model.Name;
            product.Category = model.Category;
            product.Description = model.Description;
            product.BuyingPrice = model.BuyingPrice;
            product.SellingPrice = model.SellingPrice;
            product.LowStockAlert = model.LowStockAlert;
            product.Unit = model.Unit;
            product.IsActive = model.IsActive;

            await _db.SaveChangesAsync();
            TempData["Success"] = "✅ Product updated!";
            return RedirectToAction("Products");
        }

        // ── STOCK IN ───────────────────────────────────
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> StockIn()
        {
            ViewBag.Products = await _db.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> StockIn(
            int productID, int quantity,
            decimal unitPrice, string? notes)
        {
            var product = await _db.Products.FindAsync(productID);
            if (product == null) return NotFound();

            var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Update stock
            product.CurrentStock += quantity;
            if (unitPrice > 0) product.BuyingPrice = unitPrice;

            // Record movement
            _db.StockMovements.Add(new StockMovement
            {
                ProductID = productID,
                Type = MovementType.StockIn,
                Quantity = quantity,
                UnitPrice = unitPrice > 0 ? unitPrice : product.BuyingPrice,
                TotalValue = quantity * (unitPrice > 0 ? unitPrice : product.BuyingPrice),
                Notes = notes,
                CreatedByID = staffId,
                CreatedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();
            TempData["Success"] = $"✅ Added {quantity} units of {product.Name}. New stock: {product.CurrentStock}";
            return RedirectToAction("Products");
        }

        // NEW SALE
        [HttpGet]
        public async Task<IActionResult> NewSale()
        {
            ViewBag.Products = await _db.Products
                .Where(p => p.IsActive && p.CurrentStock > 0)
                .OrderBy(p => p.Name)
                .ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> NewSale(
            List<int> productIDs,
            List<int> quantities,
            List<decimal> customPrices,
            PaymentMethod paymentMethod,
            string? mpesaCode,
            string? notes)
        {
            if (productIDs == null || !productIDs.Any())
            {
                TempData["Error"] = "❌ No products selected.";
                return RedirectToAction("NewSale");
            }

            var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            decimal totalAmount = 0;
            decimal totalProfit = 0;
            var saleItems = new List<ShopSaleItem>();

            for (int i = 0; i < productIDs.Count; i++)
            {
                var product = await _db.Products.FindAsync(productIDs[i]);
                if (product == null) continue;

                int qty = quantities[i];
                if (qty <= 0) continue;

                if (qty > product.CurrentStock)
                {
                    TempData["Error"] = $"❌ Not enough stock for {product.Name}. Available: {product.CurrentStock}";
                    return RedirectToAction("NewSale");
                }

                // Use custom price if provided, otherwise use default
                decimal unitPrice = (customPrices != null && i < customPrices.Count && customPrices[i] > 0)
                    ? customPrices[i]
                    : product.SellingPrice;

                decimal subtotal = qty * unitPrice;
                decimal profit = qty * (unitPrice - product.BuyingPrice);

                totalAmount += subtotal;
                totalProfit += profit;

                saleItems.Add(new ShopSaleItem
                {
                    ProductID = product.ProductID,
                    Quantity = qty,
                    UnitPrice = unitPrice,           // saves the actual charged price
                    BuyingPrice = product.BuyingPrice,
                    Subtotal = subtotal,
                    Profit = profit
                });

                // Deduct stock
                product.CurrentStock -= qty;

                // Record movement
                _db.StockMovements.Add(new StockMovement
                {
                    ProductID = product.ProductID,
                    Type = MovementType.Sale,
                    Quantity = qty,
                    UnitPrice = unitPrice,
                    TotalValue = subtotal,
                    Notes = unitPrice != product.SellingPrice
                        ? $"Custom price applied (default: KES {product.SellingPrice:N0})"
                        : "Sale",
                    CreatedByID = staffId
                });
            }

            var sale = new ShopSale
            {
                StaffID = staffId!,
                TotalAmount = totalAmount,
                TotalProfit = totalProfit,
                PaymentMethod = paymentMethod,
                MpesaCode = paymentMethod == PaymentMethod.MPesa
                    ? mpesaCode?.Trim().ToUpper() : null,
                Notes = notes,
                CreatedAt = DateTime.Now
            };

            _db.ShopSales.Add(sale);
            await _db.SaveChangesAsync();

            foreach (var item in saleItems)
            {
                item.SaleID = sale.SaleID;
                _db.ShopSaleItems.Add(item);
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = $"✅ Sale recorded! Total: KES {totalAmount:N0} | Profit: KES {totalProfit:N0}";
            return RedirectToAction("SaleDetails", new { id = sale.SaleID });
        }

        // SALES HISTORY 
        public async Task<IActionResult> Sales(string? date)
        {
            var query = _db.ShopSales
                .Include(s => s.Staff)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(date) &&
                DateTime.TryParse(date, out var filterDate))
                query = query.Where(s => s.CreatedAt.Date == filterDate.Date);

            var sales = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            ViewBag.Date = date;
            ViewBag.TotalRevenue = sales.Sum(s => s.TotalAmount);
            ViewBag.TotalProfit = sales.Sum(s => s.TotalProfit);

            return View(sales);
        }

        public async Task<IActionResult> SaleDetails(int id)
        {
            var sale = await _db.ShopSales
                .Include(s => s.Staff)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(s => s.SaleID == id);

            if (sale == null) return NotFound();
            return View(sale);
        }

        // SHOP REPORTS
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Reports()
        {
            var today = DateTime.Today;
            var firstOfMonth = new DateTime(today.Year, today.Month, 1);

            var vm = new ShopReportsViewModel
            {
                RevenueToday = await _db.ShopSales
                    .Where(s => s.CreatedAt.Date == today)
                    .SumAsync(s => (decimal?)s.TotalAmount) ?? 0,

                ProfitToday = await _db.ShopSales
                    .Where(s => s.CreatedAt.Date == today)
                    .SumAsync(s => (decimal?)s.TotalProfit) ?? 0,

                RevenueThisMonth = await _db.ShopSales
                    .Where(s => s.CreatedAt >= firstOfMonth)
                    .SumAsync(s => (decimal?)s.TotalAmount) ?? 0,

                ProfitThisMonth = await _db.ShopSales
                    .Where(s => s.CreatedAt >= firstOfMonth)
                    .SumAsync(s => (decimal?)s.TotalProfit) ?? 0,

                TotalSalesToday = await _db.ShopSales
                    .CountAsync(s => s.CreatedAt.Date == today),

                TotalSalesMonth = await _db.ShopSales
                    .CountAsync(s => s.CreatedAt >= firstOfMonth),

                TotalProducts = await _db.Products.CountAsync(p => p.IsActive),

                LowStockCount = await _db.Products
                    .CountAsync(p => p.IsActive &&
                                     p.CurrentStock <= p.LowStockAlert),

                LowStockProducts = await _db.Products
                    .Where(p => p.IsActive && p.CurrentStock <= p.LowStockAlert)
                    .OrderBy(p => p.CurrentStock)
                    .ToListAsync(),

                TopProducts = await _db.ShopSaleItems
                    .GroupBy(i => i.Product.Name)
                    .Select(g => new TopProductItem
                    {
                        ProductName = g.Key,
                        UnitsSold = g.Sum(i => i.Quantity),
                        Revenue = g.Sum(i => i.Subtotal),
                        Profit = g.Sum(i => i.Profit)
                    })
                    .OrderByDescending(x => x.Revenue)
                    .Take(10)
                    .ToListAsync(),

                RecentSales = await _db.ShopSales
                    .Include(s => s.Staff)
                    .Include(s => s.Items)
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(10)
                    .ToListAsync(),

                CarwashRevenueThisMonth = await _db.Transactions
                    .Where(t => t.CreatedAt >= firstOfMonth &&
                               (t.Status == WashStatus.Completed ||
                                t.Status == WashStatus.Paid))
                    .SumAsync(t => (decimal?)t.TotalAmount) ?? 0,

                CarwashProfitThisMonth = (await _db.Transactions
                    .Where(t => t.CreatedAt >= firstOfMonth &&
                               (t.Status == WashStatus.Completed ||
                                t.Status == WashStatus.Paid))
                    .SumAsync(t => (decimal?)t.TotalAmount) ?? 0)
                    - (await _db.Expenses
                    .Where(e => e.ExpenseDate >= firstOfMonth)
                    .SumAsync(e => (decimal?)e.Amount) ?? 0)
            };

            vm.TotalBusinessProfitThisMonth =
                vm.ProfitThisMonth + vm.CarwashProfitThisMonth;

            return View(vm);
        }

        // ── EXPORT SHOP SALES CSV 
        public async Task<IActionResult> Export(string? date)
        {
            var query = _db.ShopSales
                .Include(s => s.Staff)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(date) &&
                DateTime.TryParse(date, out var filterDate))
                query = query.Where(s => s.CreatedAt.Date == filterDate.Date);

            var sales = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("KUJARIBUTUU CARWASH & DETAILING — SHOP SALES");
            csv.AppendLine($"Exported: {DateTime.Now:dd MMM yyyy HH:mm}");
            csv.AppendLine($"Total Revenue: KES {sales.Sum(s => s.TotalAmount):N0}");
            csv.AppendLine($"Total Profit: KES {sales.Sum(s => s.TotalProfit):N0}");
            csv.AppendLine("");
            csv.AppendLine("Date,Product,Qty,Unit Price,Subtotal,Profit,Payment,M-Pesa Code,Staff");

            foreach (var sale in sales)
            {
                foreach (var item in sale.Items)
                {
                    csv.AppendLine(
                        $"{sale.CreatedAt:dd/MM/yyyy HH:mm}," +
                        $"{item.Product.Name}," +
                        $"{item.Quantity}," +
                        $"{item.UnitPrice}," +
                        $"{item.Subtotal}," +
                        $"{item.Profit}," +
                        $"{sale.PaymentMethod}," +
                        $"{sale.MpesaCode ?? "—"}," +
                        $"{sale.Staff.FullName}"
                    );
                }
            }

            var fileName = $"Kujaributuu_ShopSales_{DateTime.Now:yyyyMMdd}.csv";
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", fileName);
        }

        // ── DELETE ACTIONS 
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            // Check if product has sales history
            var hasSales = await _db.ShopSaleItems
                .AnyAsync(s => s.ProductID == id);

            if (hasSales)
            {
                // Deactivate instead of delete to preserve history
                product.IsActive = false;
                await _db.SaveChangesAsync();
                TempData["Success"] = $"✅ {product.Name} has been deactivated (it has sales history — cannot fully delete).";
            }
            else
            {
                _db.Products.Remove(product);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"✅ {product.Name} deleted successfully.";
            }
            return RedirectToAction("Products");
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteSale(int id)
        {
            var sale = await _db.ShopSales
                .Include(s => s.Staff) // Added inclusion for safety
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.SaleID == id);

            if (sale == null) return NotFound();

            // Restore stock for each item
            foreach (var item in sale.Items)
            {
                var product = await _db.Products.FindAsync(item.ProductID);
                if (product != null)
                    product.CurrentStock += item.Quantity;
            }

            _db.ShopSaleItems.RemoveRange(sale.Items);
            _db.ShopSales.Remove(sale);
            await _db.SaveChangesAsync();

            TempData["Success"] = "✅ Sale deleted and stock restored.";
            return RedirectToAction("Sales");
        }
    }
}