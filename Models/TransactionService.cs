using System.ComponentModel.DataAnnotations;

namespace SplashCityCarwash.Models
{
    public class TransactionService
    {
        [Key]
        public int ID { get; set; }
        public int TransactionID { get; set; }
        public int ServiceID { get; set; }
        public decimal PriceAtTime { get; set; }

        public Transaction Transaction { get; set; } = null!;
        public ServicePackage Service { get; set; } = null!;
    }
}