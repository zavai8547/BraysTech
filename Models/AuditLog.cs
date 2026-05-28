using System.ComponentModel.DataAnnotations;

namespace BraysTech.Models
{
    public enum AuditAction
    {
        Login = 0,
        Logout = 1,
        SaleCreated = 2,
        SaleDeleted = 3,
        StockAdded = 4,
        StockEdited = 5,
        StockMarkedFaulty = 6,
        StockRepaired = 7,
        CustomerCreated = 8,
        CustomerEdited = 9,
        CustomerDeleted = 10,
        BranchCreated = 11,
        BranchEdited = 12,
        StaffCreated = 13,
        StaffRoleChanged = 14,
        StaffBranchChanged = 15,
        StaffDeactivated = 16,
        StaffActivated = 17,
        PasswordReset = 18,
        PriceOverride = 19,
        SettingsChanged = 20,
        ExpenseAdded = 21,
        ExpenseDeleted = 22
    }

    public class AuditLog
    {
        [Key]
        public int LogID { get; set; }

        public string UserID { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? UserRole { get; set; }
        public int? BranchID { get; set; }
        public string? BranchName { get; set; }

        public AuditAction Action { get; set; }
        public string Module { get; set; } = string.Empty;

        // Human readable description
        public string Description { get; set; } = string.Empty;

        // What changed — before and after
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        // Reference to the affected record
        public string? RecordType { get; set; }
        public string? RecordID { get; set; }

        public string? IPAddress { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}