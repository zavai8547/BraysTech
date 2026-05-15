using System.ComponentModel.DataAnnotations;

namespace BraysTech.Models
{
    public enum SalePaymentMethod
    {
        Cash = 0,
        MPesa = 1,
        MKOPA = 2,
        Card = 3
    }

    public class PhoneSale
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

        public ICollection<PhoneSaleItem> Items { get; set; }
            = new List<PhoneSaleItem>();
    }

    public class PhoneSaleItem
    {
        [Key]
        public int ItemID { get; set; }
        public int SaleID { get; set; }
        public PhoneSale? Sale { get; set; }
        public int StockID { get; set; }
        public IMEIStock? Phone { get; set; }
        public string IMEI { get; set; } = string.Empty;
        public string PhoneName { get; set; } = string.Empty;
        public decimal SellingPrice { get; set; }
        public decimal BuyingPrice { get; set; }
        public decimal Profit { get; set; }
    }
}