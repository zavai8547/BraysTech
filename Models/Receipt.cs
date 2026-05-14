using System.ComponentModel.DataAnnotations;

namespace SplashCityCarwash.Models
{
    public class Receipt
    {
        [Key]
        public int ReceiptID { get; set; }
        public int TransactionID { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public string? FilePath { get; set; }

        public Transaction Transaction { get; set; } = null!;
    }
}