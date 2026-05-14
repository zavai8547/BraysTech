using System.ComponentModel.DataAnnotations;

namespace SplashCityCarwash.Models
{
    public enum MovementType { StockIn, Sale, Adjustment, Return }

    public class StockMovement
    {
        [Key]
        public int MovementID { get; set; }
        public int ProductID { get; set; }
        public MovementType Type { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue { get; set; }
        public string? Notes { get; set; }
        public string? CreatedByID { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Product Product { get; set; } = null!;
        public AppUser? CreatedBy { get; set; }
    }
}