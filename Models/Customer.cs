using System.ComponentModel.DataAnnotations;

namespace SplashCityCarwash.Models
{
    public class Customer
    {
        [Key]
        public int CustomerID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Notes { get; set; }
        public int TotalVisits { get; set; } = 0;
        public decimal TotalSpent { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}