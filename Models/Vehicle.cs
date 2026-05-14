using Microsoft.EntityFrameworkCore.Migrations;
using System.ComponentModel.DataAnnotations;

namespace SplashCityCarwash.Models
{
    public enum VehicleType { Sedan, SUV, Truck, Van, Motorcycle, Other }

    public class Vehicle
    {
        [Key]
        public int VehicleID { get; set; }
        public int CustomerID { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public VehicleType VehicleType { get; set; }
        public string? Make { get; set; }
        public string? CarModel { get; set; }
        public string? Color { get; set; }

        public Customer Customer { get; set; } = null!;
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
