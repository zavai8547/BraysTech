using System.ComponentModel.DataAnnotations;

namespace BraysTech.Models
{
    public enum PhoneStatus
    {
        InStock = 0,
        Sold = 1,
        OnDisplay = 2,
        Faulty = 3
    }

    public class IMEIStock
    {
        [Key]
        public int StockID { get; set; }
        public string IMEI { get; set; } = string.Empty;
        public string PhoneName { get; set; } = string.Empty;
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? Color { get; set; }
        public string? Storage { get; set; }
        public decimal BuyingPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public PhoneStatus Status { get; set; } = PhoneStatus.InStock;
        public int BranchID { get; set; }
        public Branch? Branch { get; set; }
        public string? SupplierName { get; set; }
        public string? Notes { get; set; }
        public DateTime DateAdded { get; set; } = DateTime.Now;
        public DateTime? DateSold { get; set; }
    }
}