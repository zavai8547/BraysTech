using System.ComponentModel.DataAnnotations;

namespace SplashCityCarwash.Models
{
    public class TransactionWasher
    {
        [Key]
        public int ID { get; set; }
        public int TransactionID { get; set; }
        public string WasherID { get; set; } = string.Empty;

        public Transaction Transaction { get; set; } = null!;
        public AppUser Washer { get; set; } = null!;
    }
}