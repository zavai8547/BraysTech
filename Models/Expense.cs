using System.ComponentModel.DataAnnotations;

namespace SplashCityCarwash.Models
{
    public class Expense
    {
        [Key]
        public int ExpenseID { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public string? RecordedByID { get; set; }
        public DateTime ExpenseDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public AppUser? RecordedBy { get; set; }
    }
}