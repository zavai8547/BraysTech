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
    [Authorize(Roles = "Admin,Manager")]
    public class TransferController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly AuditService _audit;

        public TransferController(
            AppDbContext db,
            UserManager<AppUser> userManager,
            AuditService audit)
        {
            _db = db;
            _userManager = userManager;
            _audit = audit;
        }

        // ── INDEX ──────────────────────────────────────
        public async Task<IActionResult> Index(
            string? status, int? branchID)
        {
            var currentUserID = User
                .FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var currentUser = await _userManager
                .FindByIdAsync(currentUserID!);

            var query = _db.StockTransfers
                .Include(t => t.FromBranch)
                .Include(t => t.ToBranch)
                .Include(t => t.InitiatedBy)
                .Include(t => t.ReceivedBy)
                .Include(t => t.Items)
                .AsQueryable();

            // Manager sees only transfers involving
            // their branch
            if (!isAdmin && currentUser?.BranchID != null)
                query = query.Where(t =>
                    t.FromBranchID ==
                        currentUser.BranchID ||
                    t.ToBranchID ==
                        currentUser.BranchID);

            if (!string.IsNullOrEmpty(status) &&
                Enum.TryParse<TransferStatus>(
                    status, out var ts))
                query = query.Where(t =>
                    t.Status == ts);

            if (branchID.HasValue)
                query = query.Where(t =>
                    t.FromBranchID == branchID ||
                    t.ToBranchID == branchID);

            var transfers = await query
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            ViewBag.PendingCount = transfers.Count(t =>
                t.Status == TransferStatus.Pending);
            ViewBag.InTransitCount = transfers.Count(t =>
                t.Status == TransferStatus.InTransit);
            ViewBag.CompletedCount = transfers.Count(t =>
                t.Status == TransferStatus.Completed);

            ViewBag.Branches = await _db.Branches
                .Where(b => b.IsActive).ToListAsync();
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedBranch = branchID;
            ViewBag.IsAdmin = isAdmin;
            ViewBag.UserBranchID = currentUser?.BranchID;

            return View(transfers);
        }

        // ── NEW TRANSFER GET ───────────────────────────
        [HttpGet]
        public async Task<IActionResult> New()
        {
            var currentUserID = User
                .FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var currentUser = await _userManager
                .FindByIdAsync(currentUserID!);

            var branches = await _db.Branches
                .Where(b => b.IsActive).ToListAsync();
            ViewBag.Branches = branches;
            ViewBag.SourceBranches = isAdmin
                ? branches
                : branches.Where(b =>
                    b.BranchID == currentUser?.BranchID)
                    .ToList();

            // Pre-select from branch for managers
            ViewBag.UserBranchID =
                currentUser?.BranchID;
            ViewBag.IsAdmin = isAdmin;

            return View();
        }

        // ── NEW TRANSFER POST ──────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> New(
            int fromBranchID,
            int toBranchID,
            List<int> stockIDs,
            string? notes)
        {
            var currentUserID = User
                .FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var currentUser = await _userManager
                .FindByIdAsync(currentUserID!);

            if (!isAdmin &&
                currentUser?.BranchID != fromBranchID)
            {
                TempData["Error"] =
                    "You can only transfer stock from " +
                    "your assigned branch.";
                await LoadTransferFormData(isAdmin,
                    currentUser?.BranchID);
                return View();
            }

            if (fromBranchID == toBranchID)
            {
                TempData["Error"] =
                    "From and To branch cannot " +
                    "be the same.";
                await LoadTransferFormData(isAdmin,
                    currentUser?.BranchID);
                return View();
            }

            var selectedStockIDs = stockIDs?
                .Distinct()
                .ToList() ?? new List<int>();

            if (!selectedStockIDs.Any())
            {
                TempData["Error"] =
                    "Select at least one phone " +
                    "to transfer.";
                await LoadTransferFormData(isAdmin,
                    currentUser?.BranchID);
                return View();
            }

            var branchesAreValid = await _db.Branches
                .CountAsync(b =>
                    b.IsActive &&
                    (b.BranchID == fromBranchID ||
                     b.BranchID == toBranchID));

            if (branchesAreValid != 2)
            {
                TempData["Error"] =
                    "Select valid active branches for " +
                    "the transfer.";
                await LoadTransferFormData(isAdmin,
                    currentUser?.BranchID);
                return View();
            }

            var reservedStockIDs = await _db
                .StockTransferItems
                .Where(i =>
                    selectedStockIDs.Contains(i.StockID) &&
                    i.Transfer != null &&
                    (i.Transfer.Status ==
                        TransferStatus.Pending ||
                     i.Transfer.Status ==
                        TransferStatus.InTransit))
                .Select(i => i.StockID)
                .ToListAsync();

            if (reservedStockIDs.Any())
            {
                TempData["Error"] =
                    "One or more selected phones are " +
                    "already in an active transfer.";
                await LoadTransferFormData(isAdmin,
                    currentUser?.BranchID);
                return View();
            }

            var phones = await _db.IMEIStock
                .Where(p =>
                    selectedStockIDs.Contains(p.StockID))
                .ToListAsync();

            if (phones.Count != selectedStockIDs.Count ||
                phones.Any(p =>
                    p.Status != PhoneStatus.InStock ||
                    p.BranchID != fromBranchID))
            {
                TempData["Error"] =
                    "One or more selected phones are no " +
                    "longer in stock at the source branch.";
                await LoadTransferFormData(isAdmin,
                    currentUser?.BranchID);
                return View();
            }

            var transfer = new StockTransfer
            {
                FromBranchID = fromBranchID,
                ToBranchID = toBranchID,
                InitiatedByID = currentUserID!,
                Status = TransferStatus.Pending,
                Notes = notes?.Trim(),
                CreatedAt = DateTime.Now
            };

            await using var tx =
                await _db.Database.BeginTransactionAsync();

            _db.StockTransfers.Add(transfer);
            await _db.SaveChangesAsync();

            var items = phones.Select(phone =>
                new StockTransferItem
                {
                    TransferID = transfer.TransferID,
                    StockID = phone.StockID,
                    IMEI = phone.IMEI,
                    PhoneName = phone.PhoneName
                })
                .ToList();

            _db.StockTransferItems.AddRange(items);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            var fromBranch = await _db.Branches
                .FindAsync(fromBranchID);
            var toBranch = await _db.Branches
                .FindAsync(toBranchID);

            await _audit.LogAsync(
                AuditAction.StockEdited,
                "Transfers",
                $"Transfer #{transfer.TransferID} created. " +
                $"{items.Count} phone(s) from " +
                $"{fromBranch?.Name} to " +
                $"{toBranch?.Name}.",
                recordType: "StockTransfer",
                recordID: transfer.TransferID.ToString());

            TempData["Success"] =
                $"Transfer #{transfer.TransferID} " +
                $"created with {items.Count} phone(s). " +
                $"Awaiting confirmation from " +
                $"{toBranch?.Name}.";
            return RedirectToAction("Details",
                new { id = transfer.TransferID });
        }

        // ── DETAILS ────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var transfer = await _db.StockTransfers
                .Include(t => t.FromBranch)
                .Include(t => t.ToBranch)
                .Include(t => t.InitiatedBy)
                .Include(t => t.ReceivedBy)
                .Include(t => t.Items)
                    .ThenInclude(i => i.Phone)
                        .ThenInclude(p => p!.Branch)
                .FirstOrDefaultAsync(t =>
                    t.TransferID == id);

            if (transfer == null) return NotFound();

            var currentUserID = User
                .FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var currentUser = await _userManager
                .FindByIdAsync(currentUserID!);

            ViewBag.IsAdmin = isAdmin;
            ViewBag.UserBranchID = currentUser?.BranchID;

            if (!isAdmin &&
                currentUser?.BranchID !=
                    transfer.FromBranchID &&
                currentUser?.BranchID !=
                    transfer.ToBranchID)
                return Forbid();

            ViewBag.CanConfirm =
                isAdmin ||
                currentUser?.BranchID ==
                    transfer.ToBranchID;
            ViewBag.CanCancel =
                isAdmin ||
                currentUser?.BranchID ==
                    transfer.FromBranchID;

            return View(transfer);
        }

        // ── MARK IN TRANSIT ────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkInTransit(
            int id)
        {
            var transfer = await _db.StockTransfers
                .Include(t => t.ToBranch)
                .FirstOrDefaultAsync(t =>
                    t.TransferID == id);

            if (transfer == null ||
                transfer.Status !=
                    TransferStatus.Pending)
                return NotFound();

            if (!await UserCanActOnBranch(
                transfer.FromBranchID))
                return Forbid();

            transfer.Status = TransferStatus.InTransit;
            await _db.SaveChangesAsync();

            TempData["Success"] =
                $"Transfer #{id} marked as In Transit. " +
                $"Waiting for {transfer.ToBranch?.Name} " +
                $"to confirm receipt.";
            return RedirectToAction("Details",
                new { id });
        }

        // ── CONFIRM RECEIPT ────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var transfer = await _db.StockTransfers
                .Include(t => t.FromBranch)
                .Include(t => t.ToBranch)
                .Include(t => t.Items)
                    .ThenInclude(i => i.Phone)
                .FirstOrDefaultAsync(t =>
                    t.TransferID == id);

            if (transfer == null ||
                (transfer.Status !=
                    TransferStatus.Pending &&
                 transfer.Status !=
                    TransferStatus.InTransit))
            {
                TempData["Error"] =
                    "Transfer cannot be confirmed " +
                    "in its current state.";
                return RedirectToAction("Details",
                    new { id });
            }

            if (!await UserCanActOnBranch(
                transfer.ToBranchID))
                return Forbid();

            var staffID = User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            var invalidItems = transfer.Items
                .Where(item =>
                    item.Phone == null ||
                    item.Phone.Status !=
                        PhoneStatus.InStock ||
                    item.Phone.BranchID !=
                        transfer.FromBranchID)
                .ToList();

            if (invalidItems.Any())
            {
                TempData["Error"] =
                    "Transfer cannot be confirmed " +
                    "because one or more phones are no " +
                    "longer in stock at the source branch.";
                return RedirectToAction("Details",
                    new { id });
            }

            await using var tx =
                await _db.Database.BeginTransactionAsync();

            foreach (var item in transfer.Items)
            {
                item.Phone!.BranchID =
                    transfer.ToBranchID;
            }

            transfer.Status = TransferStatus.Completed;
            transfer.ReceivedByID = staffID;
            transfer.CompletedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            await _audit.LogAsync(
                AuditAction.StockEdited,
                "Transfers",
                $"Transfer #{id} confirmed by " +
                $"{transfer.ToBranch?.Name}. " +
                $"{transfer.Items.Count} phone(s) moved " +
                $"from {transfer.FromBranch?.Name} to " +
                $"{transfer.ToBranch?.Name}.",
                recordType: "StockTransfer",
                recordID: id.ToString());

            TempData["Success"] =
                $"Transfer #{id} confirmed. " +
                $"{transfer.Items.Count} phone(s) now " +
                $"assigned to " +
                $"{transfer.ToBranch?.Name}.";
            return RedirectToAction("Index");
        }

        // ── CANCEL ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(
            int id, string? reason)
        {
            var transfer = await _db.StockTransfers
                .FirstOrDefaultAsync(t =>
                    t.TransferID == id);

            if (transfer == null ||
                transfer.Status ==
                    TransferStatus.Completed)
            {
                TempData["Error"] =
                    "Cannot cancel a completed transfer.";
                return RedirectToAction("Details",
                    new { id });
            }

            if (!await UserCanActOnBranch(
                transfer.FromBranchID))
                return Forbid();

            transfer.Status = TransferStatus.Cancelled;
            transfer.CancellationReason =
                reason?.Trim();

            await _db.SaveChangesAsync();

            TempData["Success"] =
                $"Transfer #{id} cancelled.";
            return RedirectToAction("Index");
        }

        // ── AJAX — Search InStock phones by branch ──
        [HttpGet]
        public async Task<IActionResult> SearchPhones(
            int branchID, string? q)
        {
            if (!await UserCanActOnBranch(branchID))
                return Forbid();

            var reservedStockIDs = _db
                .StockTransferItems
                .Where(i =>
                    i.Transfer != null &&
                    (i.Transfer.Status ==
                        TransferStatus.Pending ||
                     i.Transfer.Status ==
                        TransferStatus.InTransit))
                .Select(i => i.StockID);

            var query = _db.IMEIStock
                .Where(p =>
                    p.BranchID == branchID &&
                    p.Status == PhoneStatus.InStock &&
                    !reservedStockIDs.Contains(p.StockID));

            if (!string.IsNullOrEmpty(q))
                query = query.Where(p =>
                    p.IMEI.Contains(q) ||
                    p.PhoneName.Contains(q) ||
                    (p.Brand != null &&
                     p.Brand.Contains(q)) ||
                    (p.Model != null &&
                     p.Model.Contains(q)));

            var results = await query
                .Take(10)
                .Select(p => new
                {
                    p.StockID,
                    p.IMEI,
                    p.PhoneName,
                    p.Brand,
                    p.Model,
                    p.Color,
                    p.Storage,
                    p.SellingPrice
                })
                .ToListAsync();

            return Json(results);
        }

        private async Task LoadTransferFormData(
            bool isAdmin, int? userBranchID)
        {
            var branches = await _db.Branches
                .Where(b => b.IsActive)
                .ToListAsync();

            ViewBag.Branches = branches;
            ViewBag.SourceBranches = isAdmin
                ? branches
                : branches.Where(b =>
                    b.BranchID == userBranchID)
                    .ToList();
            ViewBag.IsAdmin = isAdmin;
            ViewBag.UserBranchID = userBranchID;
        }

        private async Task<bool> UserCanActOnBranch(
            int branchID)
        {
            if (User.IsInRole("Admin"))
                return true;

            var currentUserID = User
                .FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _userManager
                .FindByIdAsync(currentUserID!);

            return currentUser?.BranchID == branchID;
        }
    }
}
