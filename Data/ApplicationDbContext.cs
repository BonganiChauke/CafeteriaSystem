using CafeteriaSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
namespace CafeteriaSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options){}

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Restaurant> Restaurants { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<DepositHistory> DepositHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Employee>()
               .HasMany(e => e.DepositHistories)
               .WithOne(d => d.Employee)
               .HasForeignKey(d => d.EmployeeId)
               .HasPrincipalKey(e => e.UserId);

            modelBuilder.Entity<Restaurant>()
            .HasMany(r => r.MenuItems)
            .WithOne(m => m.Restaurant)
            .HasForeignKey(m => m.RestaurantId);

            modelBuilder.Entity<Order>()
        .HasMany(o => o.OrderItems)
        .WithOne(oi => oi.Order)
        .HasForeignKey(oi => oi.OrderId);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.MenuItem)
                .WithMany()
                .HasForeignKey(oi => oi.MenuItemId);

        }
    }
}
