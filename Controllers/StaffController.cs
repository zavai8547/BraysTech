using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BraysTech.Data;
using BraysTech.Models;
using BraysTech.Services; // Add this for AuditService and AuditAction

namespace BraysTech.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class StaffController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AuditService _audit;

        public StaffController(
            AppDbContext db,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            AuditService audit) // Added audit service injection
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _audit = audit;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .Include(u => u.Branch)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var staffList = new List<StaffViewModel>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                staffList.Add(new StaffViewModel
                {
                    User = u,
                    Role = roles.FirstOrDefault() ?? "No Role",
                    BranchName = u.Branch?.Name
                });
            }

            ViewBag.Branches = await _db.Branches
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .ToListAsync();

            return View(staffList);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(
            string fullName, string email,
            string? phone, string role,
            int branchID, string password)
        {
            // Check email not already taken
            var exists = await _userManager
                .FindByEmailAsync(email);
            if (exists != null)
            {
                TempData["Error"] =
                    "A staff account with this email already exists.";
                return RedirectToAction("Index");
            }

            // Ensure role exists
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(
                    new IdentityRole(role));

            var user = new AppUser
            {
                FullName = fullName,
                UserName = email,
                Email = email,
                Phone = phone,
                BranchID = branchID == 0 ? null : branchID,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            var result = await _userManager
                .CreateAsync(user, password);

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" ",
                    result.Errors.Select(e => e.Description));
                return RedirectToAction("Index");
            }

            await _userManager.AddToRoleAsync(user, role);

            // Log staff creation
            var branch = branchID > 0 ? await _db.Branches.FindAsync(branchID) : null;
            await _audit.LogAsync(
                AuditAction.StaffCreated,
                "Staff",
                $"New staff account created: {fullName} ({email}). " +
                $"Role: {role}. " +
                $"Branch: {branch?.Name ?? "Not assigned"}.",
                recordType: "AppUser",
                recordID: user.Id);

            TempData["Success"] =
                $"{fullName} account created successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var previousStatus = user.IsActive;
            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            // Log status change
            await _audit.LogAsync(
                user.IsActive ? AuditAction.StaffActivated : AuditAction.StaffDeactivated,
                "Staff",
                $"{user.FullName} account {(user.IsActive ? "activated" : "deactivated")}.",
                oldValue: previousStatus.ToString(),
                newValue: user.IsActive.ToString(),
                recordType: "AppUser",
                recordID: id);

            TempData["Success"] = user.IsActive
                ? $"{user.FullName} has been activated."
                : $"{user.FullName} has been deactivated.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeRole(
            string id, string newRole)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Ensure role exists in system
            if (!await _roleManager.RoleExistsAsync(newRole))
                await _roleManager.CreateAsync(
                    new IdentityRole(newRole));

            var currentRoles = await _userManager
                .GetRolesAsync(user);
            var currentRole = currentRoles.FirstOrDefault() ?? "No Role";

            await _userManager.RemoveFromRolesAsync(
                user, currentRoles);
            await _userManager.AddToRoleAsync(user, newRole);

            // Log role change
            await _audit.LogAsync(
                AuditAction.StaffRoleChanged,
                "Staff",
                $"{user.FullName} role changed from {currentRole} to {newRole}.",
                oldValue: currentRole,
                newValue: newRole,
                recordType: "AppUser",
                recordID: id);

            TempData["Success"] =
                $"{user.FullName} role changed to {newRole}.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResetPassword(
            string id, string newPassword)
        {
            if (string.IsNullOrEmpty(newPassword) ||
                newPassword.Length < 6)
            {
                TempData["Error"] =
                    "Password must be at least 6 characters.";
                return RedirectToAction("Index");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Staff member not found.";
                return RedirectToAction("Index");
            }

            var token = await _userManager
                .GeneratePasswordResetTokenAsync(user);
            var result = await _userManager
                .ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" ",
                    result.Errors.Select(e => e.Description));
                return RedirectToAction("Index");
            }

            await _userManager.UpdateSecurityStampAsync(user);

            // Log password reset
            await _audit.LogAsync(
                AuditAction.PasswordReset,
                "Staff",
                $"Password reset for {user.FullName} by {User.Identity!.Name}.",
                recordType: "AppUser",
                recordID: id);

            TempData["Success"] =
                $"Password reset for {user.FullName}.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignBranch(
            string id, int branchID)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var previousBranchID = user.BranchID;
            var previousBranch = previousBranchID.HasValue
                ? await _db.Branches.FindAsync(previousBranchID.Value)
                : null;

            user.BranchID = branchID == 0 ? null : branchID;
            await _userManager.UpdateAsync(user);

            var branch = branchID == 0
                ? null
                : await _db.Branches.FindAsync(branchID);

            // Log branch assignment change
            await _audit.LogAsync(
                AuditAction.StaffBranchChanged,
                "Staff",
                $"{user.FullName} assigned to {branch?.Name ?? "no branch"}. " +
                $"Previous: {previousBranch?.Name ?? "no branch"}.",
                oldValue: previousBranch?.Name ?? "No branch",
                newValue: branch?.Name ?? "No branch",
                recordType: "AppUser",
                recordID: id);

            TempData["Success"] = branchID == 0
                ? $"{user.FullName} removed from branch."
                : $"{user.FullName} assigned to {branch?.Name}.";

            return RedirectToAction("Index");
        }
    }
}