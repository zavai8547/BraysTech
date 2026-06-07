using System.ComponentModel.DataAnnotations;

namespace BraysTech.Models
{
    public enum ServiceType
    {
        SimSwap = 0,
        SimReplacement = 1,
        PhoneRepair = 2,
        ScreenReplacement = 3,
        BatteryReplacement = 4,
        DataTransfer = 5,
        Unlocking = 6,
        Other = 7
    }

    public class ServiceRecord
    {
        [Key]
        public int RecordID { get; set; }
        public ServiceType ServiceType { get; set; }
        public string StaffID { get; set; } = string.Empty;
        public AppUser? Staff { get; set; }
        public int BranchID { get; set; }
        public Branch? Branch { get; set; }

        // Customer
        public string CustomerName { get; set; }
            = string.Empty;
        public string CustomerPhone { get; set; }
            = string.Empty;
        public string? CustomerIDNumber { get; set; }
        // Required for sim swaps by Safaricom rules

        // Financial
        public decimal ChargeAmount { get; set; }
        public SalePaymentMethod PaymentMethod { get; set; }
        public string? MpesaCode { get; set; }

        // Service details
        public string? OldSimNumber { get; set; }
        public string? NewSimNumber { get; set; }
        public string? PhoneIMEI { get; set; }
        public string? FaultDescription { get; set; }
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;
    }
}