using System.ComponentModel.DataAnnotations;

namespace BraysTech.Models
{
    public enum SimNetwork
    {
        Safaricom = 0,
        Airtel = 1,
        Telkom = 2
    }

    public enum SimCardStatus
    {
        InStock = 0,
        Sold = 1,
        Damaged = 2
    }

    public class SimCard
    {
        [Key]
        public int SimCardID { get; set; }
        public SimNetwork Network { get; set; }
        public string? SerialNumber { get; set; }
        public SimCardStatus Status { get; set; }
            = SimCardStatus.InStock;
        public int BranchID { get; set; }
        public Branch? Branch { get; set; }
        public decimal BuyingPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public string? Notes { get; set; }
        public DateTime DateAdded { get; set; }
            = DateTime.Now;
        public DateTime? DateSold { get; set; }
        public string? SoldToName { get; set; }
        public string? SoldToPhone { get; set; }
        public string? CustomerIDNumber { get; set; }
        public bool IsReplacement { get; set; } = false;
        // Set to true at point of sale if it is a replacement
        public string? OldSimNumber { get; set; }
        public string? NewSimNumber { get; set; }
        public SalePaymentMethod? PaymentMethod { get; set; }
        public string? MpesaCode { get; set; }
    }
}