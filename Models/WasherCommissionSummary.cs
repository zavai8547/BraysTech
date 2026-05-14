namespace SplashCityCarwash.Models
{
    public class WasherCommissionSummary
    {
        public string WasherID { get; set; } = string.Empty;
        public string WasherName { get; set; } = string.Empty;
        public int TotalWashes { get; set; }
        public decimal TotalWashValue { get; set; }
        public decimal Commission { get; set; }
    }
}