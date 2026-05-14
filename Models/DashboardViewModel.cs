namespace SplashCityCarwash.Models
{
    public class DashboardViewModel
    {
        public int CarsWaiting { get; set; }
        public int CarsBeingWashed { get; set; }
        public int CarsCompletedToday { get; set; }
        public decimal RevenueToday { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public int TotalCustomers { get; set; }
        public string MostPopularService { get; set; } = "N/A";
        public List<Transaction> RecentTransactions { get; set; } = new();
        public List<WashQueue> ActiveQueue { get; set; } = new();
    }
}