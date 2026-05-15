namespace BraysTech.Models
{
    public class DashboardViewModel
    {
        public int TotalBranches { get; set; }
        public int TotalStaff { get; set; }
        public int TotalPhonesInStock { get; set; }
        public decimal RevenueTodayAll { get; set; }
        public decimal RevenueThisMonthAll { get; set; }
        public decimal ProfitThisMonth { get; set; }
        public int SalesToday { get; set; }
        public int SalesThisMonth { get; set; }
        public List<BranchSummary> BranchSummaries { get; set; }
            = new List<BranchSummary>();
    }

    public class BranchSummary
    {
        public string BranchName { get; set; } = string.Empty;
        public decimal RevenueToday { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public int PhonesInStock { get; set; }
        public int StaffCount { get; set; }
    }
}