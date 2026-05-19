using System.ComponentModel.DataAnnotations;

namespace BraysTech.Models
{
    public class Customer
    {
        [Key]
        public int CustomerID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Notes { get; set; }
        public int TotalPurchases { get; set; } = 0;
        public decimal TotalSpent { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<PhoneSale> Sales { get; set; }
            = new List<PhoneSale>();
    }
}