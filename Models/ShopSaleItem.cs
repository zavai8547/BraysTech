using System.ComponentModel.DataAnnotations;

namespace SplashCityCarwash.Models
{
    public class ShopSaleItem
    {
        [Key]
        public int ItemID { get; set; }
        public int SaleID { get; set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal BuyingPrice { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Profit { get; set; }

        public ShopSale Sale { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}