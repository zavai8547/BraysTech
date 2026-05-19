using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BraysTech.Data;
using BraysTech.Models;

namespace BraysTech.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class StaffController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public StaffController(
            AppDbContext db,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
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

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

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
            await _userManager.RemoveFromRolesAsync(
                user, currentRoles);
            await _userManager.AddToRoleAsync(user, newRole);

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

            user.BranchID = branchID == 0 ? null : branchID;
            await _userManager.UpdateAsync(user);

            var branch = branchID == 0
                ? null
                : await _db.Branches.FindAsync(branchID);

            TempData["Success"] = branchID == 0
                ? $"{user.FullName} removed from branch."
                : $"{user.FullName} assigned to {branch?.Name}.";

            return RedirectToAction("Index");
        }
    }
}