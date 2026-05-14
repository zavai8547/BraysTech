using System.ComponentModel.DataAnnotations;

namespace SplashCityCarwash.Models
{
    public class ServicePackage
    {
        [Key]
        public int ServiceID { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int EstimatedDuration { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<TransactionService> TransactionServices { get; set; } = new List<TransactionService>();
    }
}