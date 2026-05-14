using System.ComponentModel.DataAnnotations;

namespace SplashCityCarwash.Models
{
    public class ShopSale
    {
        [Key]
        public int SaleID { get; set; }
        public string StaffID { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal TotalProfit { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? MpesaCode { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public AppUser Staff { get; set; } = null!;
        public ICollection<ShopSaleItem> Items { get; set; } = new List<ShopSaleItem>();
    }
}