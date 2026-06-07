using System.ComponentModel.DataAnnotations;

namespace BraysTech.Models
{
    public class Accessory
    {
        [Key]
        public int AccessoryID { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Category { get; set; }
        // e.g. Charger, Cable, Case, Earphones, Screen Protector

        public string? Brand { get; set; }
        public string? Description { get; set; }
        public decimal BuyingPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public int CurrentStock { get; set; }
        public int LowStockAlert { get; set; } = 5;
        public int BranchID { get; set; }
        public Branch? Branch { get; set; }
        public string? SupplierName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime DateAdded { get; set; } = DateTime.Now;

        public decimal Profit =>
            SellingPrice - BuyingPrice;
        public decimal Margin =>
            SellingPrice > 0
                ? (Profit / SellingPrice * 100) : 0;

        public ICollection<AccessorySaleItem> SaleItems { get; set; }
            = new List<AccessorySaleItem>();
    }
}