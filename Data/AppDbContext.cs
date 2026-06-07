using BraysTech.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BraysTech.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<IMEIStock> IMEIStock { get; set; }
        public DbSet<PhoneSale> PhoneSales { get; set; }
        public DbSet<PhoneSaleItem> PhoneSaleItems { get; set; }
        public DbSet<Customer> Customers { get; set; }

        public DbSet<AuditLog> AuditLogs { get; set; }

        // Accessories
        public DbSet<Accessory> Accessories { get; set; }
        public DbSet<AccessorySale> AccessorySales { get; set; }
        public DbSet<AccessorySaleItem> AccessorySaleItems { get; set; }

        // Services
        public DbSet<ServiceRecord> ServiceRecords { get; set; }

        // M-Pesa Float
        public DbSet<MpesaFloat> MpesaFloats { get; set; }
        public DbSet<FloatTransaction> FloatTransactions { get; set; }

        // Cash Up
        public DbSet<CashUp> CashUps { get; set; }

        // SIM Cards
        public DbSet<SimCard> SimCards { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<IMEIStock>()
                .HasIndex(i => i.IMEI)
                .IsUnique();

            builder.Entity<Setting>()
                .HasIndex(s => s.SettingKey)
                .IsUnique();

            // Unique index for Customer phone number
            builder.Entity<Customer>()
                .HasIndex(c => c.Phone)
                .IsUnique();
        }
    }
}