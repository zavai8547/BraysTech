using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BraysTech.Data;
using BraysTech.Models;

namespace BraysTech.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MpesaController : Controller
    {
        private readonly AppDbContext _db;

        public MpesaController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Performance(
            DateTime? from, DateTime? to)
        {
            var dateFrom = from?.Date
                ?? DateTime.Today.AddDays(-30);
            var dateTo = to?.Date
                ?? DateTime.Today;
            var dateToEnd = dateTo
                .AddHours(23).AddMinutes(59)
                .AddSeconds(59);

            var branches = await _db.Branches
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .ToListAsync();

            // Phone sales by payment method per branch
            var phoneSales = await _db.PhoneSales
                .Where(s => s.CreatedAt >= dateFrom &&
                            s.CreatedAt <= dateToEnd)
                .GroupBy(s => new
                {
                    s.BranchID,
                    s.PaymentMethod
                })
                .Select(g => new
                {
                    g.Key.BranchID,
                    g.Key.PaymentMethod,
                    Total = g.Sum(s => s.TotalAmount),
                    Count = g.Count()
                })
                .ToListAsync();

            // Accessory sales by payment method per branch
            var accSales = await _db.AccessorySales
                .Where(s => s.CreatedAt >= dateFrom &&
                            s.CreatedAt <= dateToEnd)
                .GroupBy(s => new
                {
                    s.BranchID,
                    s.PaymentMethod
                })
                .Select(g => new
                {
                    g.Key.BranchID,
                    g.Key.PaymentMethod,
                    Total = g.Sum(s => s.TotalAmount),
                    Count = g.Count()
                })
                .ToListAsync();

            // Service fees by payment method per branch
            var svcSales = await _db.ServiceRecords
                .Where(s => s.CreatedAt >= dateFrom &&
                            s.CreatedAt <= dateToEnd)
                .GroupBy(s => new
                {
                    s.BranchID,
                    s.PaymentMethod
                })
                .Select(g => new
                {
                    g.Key.BranchID,
                    g.Key.PaymentMethod,
                    Total = g.Sum(s => s.ChargeAmount),
                    Count = g.Count()
                })
                .ToListAsync();

            // Build branch summary
            var summary = branches.Select(b =>
            {
                decimal phoneMpesa = phoneSales
                    .Where(s => s.BranchID == b.BranchID &&
                                s.PaymentMethod ==
                                    SalePaymentMethod.MPesa)
                    .Sum(s => s.Total);
                decimal phoneCash = phoneSales
                    .Where(s => s.BranchID == b.BranchID &&
                                s.PaymentMethod ==
                                    SalePaymentMethod.Cash)
                    .Sum(s => s.Total);

                decimal accMpesa = accSales
                    .Where(s => s.BranchID == b.BranchID &&
                                s.PaymentMethod ==
                                    SalePaymentMethod.MPesa)
                    .Sum(s => s.Total);
                decimal accCash = accSales
                    .Where(s => s.BranchID == b.BranchID &&
                                s.PaymentMethod ==
                                    SalePaymentMethod.Cash)
                    .Sum(s => s.Total);

                decimal svcMpesa = svcSales
                    .Where(s => s.BranchID == b.BranchID &&
                                s.PaymentMethod ==
                                    SalePaymentMethod.MPesa)
                    .Sum(s => s.Total);
                decimal svcCash = svcSales
                    .Where(s => s.BranchID == b.BranchID &&
                                s.PaymentMethod ==
                                    SalePaymentMethod.Cash)
                    .Sum(s => s.Total);

                return new
                {
                    Branch = b,
                    MpesaTotal = phoneMpesa +
                        accMpesa + svcMpesa,
                    CashTotal = phoneCash +
                        accCash + svcCash,
                    PhoneMpesa = phoneMpesa,
                    PhoneCash = phoneCash,
                    AccMpesa = accMpesa,
                    AccCash = accCash,
                    SvcMpesa = svcMpesa,
                    SvcCash = svcCash
                };
            })
            .OrderByDescending(b => b.MpesaTotal)
            .ToList();

            ViewBag.Summary = summary;
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;
            ViewBag.TotalMpesa =
                summary.Sum(s => s.MpesaTotal);
            ViewBag.TotalCash =
                summary.Sum(s => s.CashTotal);
            ViewBag.TopMpesaBranch =
                summary.FirstOrDefault()?.Branch.Name
                ?? "—";

            return View();
        }
    }
}