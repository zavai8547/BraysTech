using System.ComponentModel.DataAnnotations;

namespace SplashCityCarwash.Models
{
    public class Product
    {
        [Key]
        public int ProductID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Description { get; set; }
        public decimal BuyingPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public int CurrentStock { get; set; } = 0;
        public int LowStockAlert { get; set; } = 0;
        public string Unit { get; set; } = "piece";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
        public ICollection<ShopSaleItem> ShopSaleItems { get; set; } = new List<ShopSaleItem>();
    }
}