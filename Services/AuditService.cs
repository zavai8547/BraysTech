using System.Security.Claims;
using BraysTech.Data;
using BraysTech.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BraysTech.Services
{
    public class AuditService
    {
        private readonly AppDbContext _db;
        private readonly IHttpContextAccessor _http;
        private readonly UserManager<AppUser> _userManager;

        public AuditService(
            AppDbContext db,
            IHttpContextAccessor http,
            UserManager<AppUser> userManager)
        {
            _db = db;
            _http = http;
            _userManager = userManager;
        }

        public async Task LogAsync(
            AuditAction action,
            string module,
            string description,
            string? oldValue = null,
            string? newValue = null,
            string? recordType = null,
            string? recordID = null)
        {
            try
            {
                var context = _http.HttpContext;
                var userID = context?.User
                    .FindFirstValue(ClaimTypes.NameIdentifier);

                string userName = "System";
                string? userRole = null;
                int? branchID = null;
                string? branchName = null;

                if (!string.IsNullOrEmpty(userID))
                {
                    var user = await _userManager
                        .FindByIdAsync(userID);
                    if (user != null)
                    {
                        userName = user.FullName;
                        branchID = user.BranchID;

                        var roles = await _userManager
                            .GetRolesAsync(user);
                        userRole = roles.FirstOrDefault();

                        if (user.BranchID != null)
                        {
                            var branch = await _db.Branches
                                .FirstOrDefaultAsync(b =>
                                    b.BranchID == user.BranchID);
                            branchName = branch?.Name;
                        }
                    }
                }

                var ip = context?.Connection
                    .RemoteIpAddress?.ToString();

                var log = new AuditLog
                {
                    UserID = userID ?? "system",
                    UserName = userName,
                    UserRole = userRole,
                    BranchID = branchID,
                    BranchName = branchName,
                    Action = action,
                    Module = module,
                    Description = description,
                    OldValue = oldValue,
                    NewValue = newValue,
                    RecordType = recordType,
                    RecordID = recordID,
                    IPAddress = ip,
                    CreatedAt = DateTime.Now
                };

                _db.AuditLogs.Add(log);
                await _db.SaveChangesAsync();
            }
            catch
            {
                // Never let audit logging crash the main app
            }
        }
    }
}