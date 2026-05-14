namespace SplashCityCarwash.Models
{
    public class EditTransactionViewModel
    {
        public int TransactionID { get; set; }
        public decimal TotalAmount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public WashStatus Status { get; set; }
        public string? MpesaCode { get; set; }
        public string? Notes { get; set; }
        public string WashDate { get; set; } = string.Empty;
        public string WashTime { get; set; } = string.Empty;
    }
}