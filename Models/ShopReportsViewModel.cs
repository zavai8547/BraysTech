namespace SplashCityCarwash.Models
{
    public class ShopReportsViewModel
    {
        public decimal RevenueToday { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal ProfitToday { get; set; }
        public decimal ProfitThisMonth { get; set; }
        public decimal CostOfGoodsSold { get; set; }
        public int TotalSalesToday { get; set; }
        public int TotalSalesMonth { get; set; }
        public int TotalProducts { get; set; }
        public int LowStockCount { get; set; }
        public List<Product> LowStockProducts { get; set; } = new();
        public List<TopProductItem> TopProducts { get; set; } = new();
        public List<ShopSale> RecentSales { get; set; } = new();

        // Combined with carwash
        public decimal CarwashRevenueThisMonth { get; set; }
        public decimal CarwashProfitThisMonth { get; set; }
        public decimal TotalBusinessProfitThisMonth { get; set; }
    }

    public class TopProductItem
    {
        public string ProductName { get; set; } = string.Empty;
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
        public decimal Profit { get; set; }
    }
}