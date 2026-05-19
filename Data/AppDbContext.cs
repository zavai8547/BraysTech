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