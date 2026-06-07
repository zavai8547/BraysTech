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
    public class ServicesController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly AuditService _audit;

        public ServicesController(
            AppDbContext db,
            UserManager<AppUser> userManager,
            AuditService audit)
        {
            _db = db;
            _userManager = userManager;
            _audit = audit;
        }

        public async Task<IActionResult> Index(
            string? type, int? branchID,
            DateTime? from, DateTime? to)
        {
            var currentUserID = User
                .FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("Manager");
            var currentUser = await _userManager
                .FindByIdAsync(currentUserID!);

            var query = _db.ServiceRecords
                .Include(s => s.Staff)
                .Include(s => s.Branch)
                .AsQueryable();

            if (!isAdmin && !isManager)
                query = query.Where(s =>
                    s.StaffID == currentUserID);
            else if (isManager &&
                     currentUser?.BranchID != null)
                query = query.Where(s =>
                    s.BranchID == currentUser.BranchID);

            if (!string.IsNullOrEmpty(type) &&
                Enum.TryParse<ServiceType>(
                    type, out var st))
                query = query.Where(s =>
                    s.ServiceType == st);

            if (branchID.HasValue)
                query = query.Where(s =>
                    s.BranchID == branchID);

            if (from.HasValue)
                query = query.Where(s =>
                    s.CreatedAt.Date >= from.Value.Date);

            if (to.HasValue)
                query = query.Where(s =>
                    s.CreatedAt.Date <= to.Value.Date);

            var records = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var today = DateTime.Today;

            ViewBag.TotalRevenue =
                records.Sum(s => s.ChargeAmount);
            ViewBag.TotalRecords = records.Count;
            ViewBag.TodayCount = records.Count(s =>
                s.CreatedAt.Date == today);
            ViewBag.TodayRevenue = records
                .Where(s => s.CreatedAt.Date == today)
                .Sum(s => s.ChargeAmount);

            // Counts by type
            ViewBag.SimSwapCount = records.Count(s =>
                s.ServiceType == ServiceType.SimSwap);
            ViewBag.SimNewCount = records.Count(s =>
                s.ServiceType == ServiceType.SimReplacement);
            ViewBag.RepairCount = records.Count(s =>
                s.ServiceType == ServiceType.PhoneRepair ||
                s.ServiceType ==
                    ServiceType.ScreenReplacement ||
                s.ServiceType ==
                    ServiceType.BatteryReplacement);

            ViewBag.Branches = await _db.Branches
                .Where(b => b.IsActive).ToListAsync();
            ViewBag.SelectedType = type;
            ViewBag.SelectedBranch = branchID;
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");

            return View(records);
        }

        [HttpGet]
        public async Task<IActionResult> New()
        {
            var currentUserID = User
                .FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _userManager
                .FindByIdAsync(currentUserID!);
            var isManager = User.IsInRole("Manager");
            var isAdmin = User.IsInRole("Admin");

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

        [HttpPost]
        public async Task<IActionResult> New(
            ServiceType serviceType,
            string customerName,
            string customerPhone,
            string? customerIDNumber,
            string? oldSimNumber,
            string? newSimNumber,
            string? phoneIMEI,
            string? faultDescription,
            decimal chargeAmount,
            SalePaymentMethod paymentMethod,
            string? mpesaCode,
            int branchID,
            string? notes)
        {
            var staffID = User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            // Clean inputs
            customerName = customerName?.Trim()
                ?? "Unknown";
            customerPhone = customerPhone?.Trim()
                ?? "N/A";

            var record = new ServiceRecord
            {
                ServiceType = serviceType,
                StaffID = staffID!,
                BranchID = branchID,
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                CustomerIDNumber =
                    customerIDNumber?.Trim(),
                OldSimNumber = oldSimNumber?.Trim(),
                NewSimNumber = newSimNumber?.Trim(),
                PhoneIMEI = phoneIMEI?.Trim(),
                FaultDescription =
                    faultDescription?.Trim(),
                ChargeAmount = chargeAmount,
                PaymentMethod = paymentMethod,
                MpesaCode = paymentMethod ==
                    SalePaymentMethod.MPesa
                    ? mpesaCode?.Trim().ToUpper()
                    : null,
                Notes = notes?.Trim(),
                CreatedAt = DateTime.Now
            };

            _db.ServiceRecords.Add(record);

            // Auto-create or update customer
            if (customerPhone != "N/A")
            {
                var existing = await _db.Customers
                    .FirstOrDefaultAsync(c =>
                        c.Phone == customerPhone);
                if (existing != null)
                {
                    if (chargeAmount > 0)
                    {
                        existing.TotalPurchases++;
                        existing.TotalSpent +=
                            chargeAmount;
                    }
                }
                else if (chargeAmount > 0)
                {
                    _db.Customers.Add(new Customer
                    {
                        FullName = customerName,
                        Phone = customerPhone,
                        TotalPurchases = 1,
                        TotalSpent = chargeAmount,
                        CreatedAt = DateTime.Now
                    });
                }
            }

            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                AuditAction.SaleCreated,
                "Services",
                $"{serviceType} for {customerName}. " +
                $"KES {chargeAmount:N0}.",
                recordType: "ServiceRecord",
                recordID: record.RecordID.ToString());

            TempData["Success"] =
                $"{serviceType} recorded for " +
                $"{customerName}. " +
                $"KES {chargeAmount:N0}";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Details(int id)
        {
            var record = await _db.ServiceRecords
                .Include(s => s.Staff)
                .Include(s => s.Branch)
                .FirstOrDefaultAsync(s =>
                    s.RecordID == id);

            if (record == null) return NotFound();
            return View(record);
        }
    }
}