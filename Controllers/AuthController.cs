using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BraysTech.Models;
using BraysTech.Services;

namespace BraysTech.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AuditService _audit;
        private readonly IWebHostEnvironment _env;

        public AuthController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            AuditService audit,
            IWebHostEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _audit = audit;
            _env = env;
        }

        // ── LOGIN ──────────────────────────────────────
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity!.IsAuthenticated)
                return RedirectToAction("Index", "Dashboard");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // FIND USER FIRST
            var user = await _userManager.FindByEmailAsync(model.Email);

            // USER NOT FOUND
            if (user == null)
            {
                ModelState.AddModelError("",
                    "Invalid email or password.");

                return View(model);
            }

            // BLOCK DEACTIVATED USERS
            if (!user.IsActive)
            {
                ModelState.AddModelError("",
                    "Your account has been deactivated. Please contact administrator.");

                return View(model);
            }

            // ATTEMPT LOGIN
            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // LOG LOGIN
                await _audit.LogAsync(
                    AuditAction.Login,
                    "Auth",
                    $"{user.FullName} logged in.",
                    recordType: "AppUser",
                    recordID: user.Id);

                return RedirectToAction("Index", "Dashboard");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("",
                    "Account locked. Try again later.");
            }
            else
            {
                ModelState.AddModelError("",
                    "Invalid email or password.");
            }

            return View(model);
        }

        // ── REGISTER (Admin only) ──────────────────────
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                await _roleManager.CreateAsync(
                    new IdentityRole(model.Role));
            }

            var user = new AppUser
            {
                FullName = model.FullName,
                UserName = model.Email,
                Email = model.Email,
                Phone = model.Phone,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            var result = await _userManager.CreateAsync(
                user,
                model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(
                    user,
                    model.Role);

                // LOG STAFF CREATION
                var currentUser =
                    await _userManager.GetUserAsync(User);

                await _audit.LogAsync(
                    AuditAction.StaffCreated,
                    "Staff",
                    $"{currentUser?.FullName ?? "System"} created staff account for {model.FullName} with role {model.Role}.",
                    recordType: "AppUser",
                    recordID: user.Id);

                TempData["Success"] =
                    $"Staff account created for {model.FullName}";

                return RedirectToAction("Index", "Staff");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("",
                    error.Description);
            }

            return View(model);
        }

        // ── LOGOUT ─────────────────────────────────────
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                await _audit.LogAsync(
                    AuditAction.Logout,
                    "Auth",
                    $"{user.FullName} logged out.",
                    recordType: "AppUser",
                    recordID: user.Id);
            }

            await _signInManager.SignOutAsync();

            return RedirectToAction("Login");
        }

        // ── SEED ADMIN (first time setup) ──────────────
        [HttpGet]
        public async Task<IActionResult> SeedAdmin()
        {
            if (!_env.IsDevelopment())
                return NotFound();

            // ONLY RUN IF NO ADMIN EXISTS
            var adminExists =
                await _userManager.GetUsersInRoleAsync("Admin");

            if (adminExists.Any())
                return Content("Admin already exists.");

            // ENSURE ROLES EXIST
            foreach (var role in new[]
                     {
                         "Admin",
                         "Manager",
                         "Salesperson"
                     })
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(
                        new IdentityRole(role));
                }
            }

            var admin = new AppUser
            {
                FullName = "System Administrator",
                UserName = "admin@braystech.store",
                Email = "admin@braystech.store",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            var result = await _userManager.CreateAsync(
                admin,
                "Admin@1234");

            if (!result.Succeeded)
            {
                return Content(
                    string.Join(", ",
                        result.Errors.Select(e => e.Description)));
            }

            await _userManager.AddToRoleAsync(
                admin,
                "Admin");

            // LOG ADMIN CREATION
            await _audit.LogAsync(
                AuditAction.StaffCreated,
                "Auth",
                $"System created admin account for {admin.FullName} (admin@braystech.store).",
                recordType: "AppUser",
                recordID: admin.Id);

            return Content(
                "Admin created. Email: admin@braystech.store Password: Admin@1234 Login and change password immediately.");
        }

        // ── CHANGE PASSWORD ─────────────────────────────
        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangePassword(
            string currentPassword,
            string newPassword,
            string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["Error"] =
                    "❌ New passwords do not match.";

                return View();
            }

            if (newPassword.Length < 6)
            {
                TempData["Error"] =
                    "❌ Password must be at least 6 characters.";

                return View();
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login");

            var result =
                await _userManager.ChangePasswordAsync(
                    user,
                    currentPassword,
                    newPassword);

            if (result.Succeeded)
            {
                // LOG PASSWORD CHANGE
                await _audit.LogAsync(
                    AuditAction.PasswordReset,
                    "Auth",
                    $"{user.FullName} changed their password.",
                    recordType: "AppUser",
                    recordID: user.Id);

                await _signInManager.RefreshSignInAsync(user);

                TempData["Success"] =
                    "✅ Password changed successfully!";

                return RedirectToAction("ChangePassword");
            }

            foreach (var error in result.Errors)
            {
                TempData["Error"] =
                    "❌ " + error.Description;
            }

            return View();
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
