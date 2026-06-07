using System.ComponentModel.DataAnnotations;

namespace BraysTech.Models
{
    public class AccessorySale
    {
        [Key]
        public int SaleID { get; set; }
        public string StaffID { get; set; } = string.Empty;
        public AppUser? Staff { get; set; }
        public int BranchID { get; set; }
        public Branch? Branch { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalProfit { get; set; }
        public SalePaymentMethod PaymentMethod { get; set; }
        public string? MpesaCode { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<AccessorySaleItem> Items { get; set; }
            = new List<AccessorySaleItem>();
    }

    public class AccessorySaleItem
    {
        [Key]
        public int ItemID { get; set; }
        public int SaleID { get; set; }
        public AccessorySale? Sale { get; set; }
        public int AccessoryID { get; set; }
        public Accessory? Accessory { get; set; }
        public string AccessoryName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal BuyingPrice { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Profit { get; set; }
    }
}