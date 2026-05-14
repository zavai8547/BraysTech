using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SplashCityCarwash.Models;

namespace SplashCityCarwash.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<ServicePackage> ServicePackages { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionService> TransactionServices { get; set; }
        public DbSet<TransactionWasher> TransactionWashers { get; set; }
        public DbSet<WashQueue> WashQueues { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<Receipt> Receipts { get; set; }

        public DbSet<Product> Products { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<ShopSale> ShopSales { get; set; }
        public DbSet<ShopSaleItem> ShopSaleItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<TransactionService>()
                .HasIndex(ts => new { ts.TransactionID, ts.ServiceID });

            builder.Entity<Customer>()
                .HasIndex(c => c.Phone).IsUnique();

            builder.Entity<Vehicle>()
                .HasIndex(v => v.LicensePlate).IsUnique();

            builder.Entity<Setting>()
                .HasIndex(s => s.SettingKey).IsUnique();
        }
    }
}