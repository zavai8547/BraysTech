using System.ComponentModel.DataAnnotations;

namespace BraysTech.Models
{
    public class CashUp
    {
        [Key]
        public int CashUpID { get; set; }

        public string StaffID { get; set; } = string.Empty;
        public AppUser? Staff { get; set; }

        public int BranchID { get; set; }
        public Branch? Branch { get; set; }

        // What the staff member is declaring
        public decimal CashAmount { get; set; }
        public decimal MpesaFloat { get; set; }

        // Auto-calculated from sales records
        public decimal ExpectedCash { get; set; }
        public decimal ExpectedMpesa { get; set; }

        // Variance (declared vs expected)
        public decimal CashVariance =>
            CashAmount - ExpectedCash;
        public decimal MpesaVariance =>
            MpesaFloat - ExpectedMpesa;

        public string? Notes { get; set; }
        public DateTime CashUpDate { get; set; }
            = DateTime.Today;
        public DateTime CreatedAt { get; set; }
            = DateTime.Now;
    }
}