using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SplashCityCarwash.Data;
using SplashCityCarwash.Models;
using System.Security.Claims;

namespace SplashCityCarwash.Controllers
{
    [Authorize]
    public class WashController : Controller
    {
        private readonly AppDbContext _db;
        public WashController(AppDbContext db) { _db = db; }

        [HttpGet]
        public async Task<IActionResult> New()
        {
            ViewBag.Services = await _db.ServicePackages
                .Where(s => s.IsActive)
                .OrderBy(s => s.ServiceName)
                .ToListAsync();

            ViewBag.Washers = await _db.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SearchCustomer(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Json(new { found = false });

            var customers = await _db.Customers
                .Include(c => c.Vehicles)
                .Where(c => c.Phone.Contains(query)
                         || c.FullName.Contains(query))
                .Take(5)
                .ToListAsync();

            if (!customers.Any())
                return Json(new { found = false });

            return Json(new
            {
                found = true,
                customers = customers.Select(c => new
                {
                    customerID = c.CustomerID,
                    fullName = c.FullName,
                    phone = c.Phone,
                    vehicles = c.Vehicles.Select(v => new
                    {
                        vehicleID = v.VehicleID,
                        licensePlate = v.LicensePlate,
                        make = v.Make,
                        carModel = v.CarModel,
                        color = v.Color
                    })
                })
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(NewWashViewModel model)
        {
            if (model.SelectedServiceIDs == null || !model.SelectedServiceIDs.Any())
            {
                TempData["Error"] = "❌ Please select at least one service.";
                return RedirectToAction("New");
            }

            var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var services = await _db.ServicePackages
                .Where(s => model.SelectedServiceIDs.Contains(s.ServiceID))
                .ToListAsync();

            decimal total = services.Sum(s => s.Price);

            // Handle wash date
            DateTime washDateTime = DateTime.Now;
            if (!string.IsNullOrEmpty(model.WashDate) &&
                DateTime.TryParse(model.WashDate, out var parsedDate))
            {
                washDateTime = parsedDate.Date.Add(DateTime.Now.TimeOfDay);
            }

            var transaction = new Transaction
            {
                CustomerID = model.CustomerID,
                VehicleID = model.VehicleID,
                StaffID = staffId!,
                TotalAmount = total,
                PaymentMethod = model.PaymentMethod,
                MpesaCode = model.PaymentMethod == PaymentMethod.MPesa
                    ? model.MpesaCode?.Trim().ToUpper() : null,
                Status = WashStatus.Waiting,
                Notes = model.Notes,
                CreatedAt = washDateTime
            };

            _db.Transactions.Add(transaction);
            await _db.SaveChangesAsync();

            // Add services
            foreach (var service in services)
            {
                _db.TransactionServices.Add(new TransactionService
                {
                    TransactionID = transaction.TransactionID,
                    ServiceID = service.ServiceID,
                    PriceAtTime = service.Price
                });
            }

            // Add washers
            if (model.SelectedWasherIDs != null && model.SelectedWasherIDs.Any())
            {
                foreach (var washerID in model.SelectedWasherIDs)
                {
                    _db.TransactionWashers.Add(new TransactionWasher
                    {
                        TransactionID = transaction.TransactionID,
                        WasherID = washerID
                    });
                }
            }

            // Add to queue
            var queuePosition = await _db.WashQueues
                .CountAsync(q => q.Status == WashStatus.Waiting
                              || q.Status == WashStatus.Washing) + 1;

            _db.WashQueues.Add(new WashQueue
            {
                TransactionID = transaction.TransactionID,
                Status = WashStatus.Waiting,
                QueuePosition = queuePosition
            });

            // Update customer stats
            var customer = await _db.Customers.FindAsync(model.CustomerID);
            if (customer != null)
            {
                customer.TotalVisits++;
                customer.TotalSpent += total;
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Wash started! Total: KES {total:N0}. Queue: #{queuePosition}";
            return RedirectToAction("Index", "Queue");
        }
    }
}