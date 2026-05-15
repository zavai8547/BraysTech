using BraysTech.Models;
using Microsoft.AspNetCore.Identity;

namespace BraysTech.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? BranchID { get; set; }
        public Branch? Branch { get; set; }
    }
}