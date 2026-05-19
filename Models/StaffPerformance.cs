namespace BraysTech.Models
{
    public class StaffPerformance
    {
        public string StaffID { get; set; } = string.Empty;
        public string StaffName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Role { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public int? BranchID { get; set; }
        public int Rank { get; set; }

        // This Month
        public int SalesCountMonth { get; set; }
        public decimal RevenueMonth { get; set; }
        public decimal ProfitMonth { get; set; }
        public int DevicesSoldMonth { get; set; }

        // Today
        public int SalesToday { get; set; }
        public decimal RevenueToday { get; set; }
    }
}