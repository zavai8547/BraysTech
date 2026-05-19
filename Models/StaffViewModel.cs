using System;

namespace BraysTech.Models
{
    public class StaffViewModel
    {
        public AppUser User { get; set; } = new AppUser();
        public string Role { get; set; } = string.Empty;
        public string? BranchName { get; set; }
    }
}