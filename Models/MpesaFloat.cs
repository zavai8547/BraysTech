using System.ComponentModel.DataAnnotations;

namespace BraysTech.Models
{
    public enum FloatTransactionType
    {
        TopUp = 0,       // Adding money to float
        Sale = 1,        // Customer paid via M-Pesa
        Withdrawal = 2,  // Taking money out
        Transfer = 3,    // Moving float between lines
        Adjustment = 4   // Manual correction
    }

    public class MpesaFloat
    {
        [Key]
        public int FloatID { get; set; }
        public int BranchID { get; set; }
        public Branch? Branch { get; set; }
        public string TillNumber { get; set; }
            = string.Empty;
        public string? TillName { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal LowBalanceAlert { get; set; }
            = 5000;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
            = DateTime.Now;
        public DateTime UpdatedAt { get; set; }
            = DateTime.Now;

        public ICollection<FloatTransaction>
            Transactions
        { get; set; }
            = new List<FloatTransaction>();
    }

    public class FloatTransaction
    {
        [Key]
        public int TransactionID { get; set; }
        public int FloatID { get; set; }
        public MpesaFloat? Float { get; set; }
        public FloatTransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public string? Reference { get; set; }
        // M-Pesa code or bank ref
        public string? Notes { get; set; }
        public string StaffID { get; set; }
            = string.Empty;
        public AppUser? Staff { get; set; }
        public DateTime CreatedAt { get; set; }
            = DateTime.Now;
    }
}