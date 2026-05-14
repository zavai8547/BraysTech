namespace SplashCityCarwash.Models
{
    public class ReportsViewModel
    {
        public decimal RevenueToday { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal RevenueThisYear { get; set; }
        public decimal ExpensesThisMonth { get; set; }
        public decimal ProfitThisMonth { get; set; }
        public int TotalWashesToday { get; set; }
        public int TotalWashesMonth { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalVehicles { get; set; }
        public List<ServicePopularityItem> ServicePopularity { get; set; } = new();
        public List<PaymentBreakdownItem> PaymentMethodBreakdown { get; set; } = new();
        public List<Customer> TopCustomers { get; set; } = new();
    }

    public class ServicePopularityItem
    {
        public string ServiceName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Revenue { get; set; }
    }

    public class PaymentBreakdownItem
    {
        public string Method { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Total { get; set; }
    }
}