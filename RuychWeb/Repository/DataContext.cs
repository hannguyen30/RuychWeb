using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using RuychWeb.Models.Domain;
using RuychWeb.Models.DTO;

namespace RuychWeb.Repository
{
    public class DataContext : IdentityDbContext<Account>
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {

        }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<ProductDetail> ProductDetails { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Color> Colors { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartDetail> CartDetails { get; set; }
        public DbSet<Receipt> Receipts { get; set; }
        public DbSet<ReceiptDetail> ReceiptDetails { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleDetail> SaleDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Account>().ToTable("Account");
            //builder.Entity<Account>().Ignore(e => e.SecurityStamp);
            //builder.Entity<Account>().Ignore(e => e.ConcurrencyStamp);
            //builder.Entity<Account>().Ignore(e => e.PhoneNumberConfirmed);
            //builder.Entity<Account>().Ignore(e => e.PhoneNumber);
            //builder.Entity<Account>().Ignore(e => e.TwoFactorEnabled);
            //builder.Entity<Account>().Ignore(e => e.LockoutEnd);
            //builder.Entity<Account>().Ignore(e => e.LockoutEnabled);
            //builder.Entity<Account>().Ignore(e => e.AccessFailedCount);

            //// Ignore the specified Identity tables
            ////builder.Ignore<IdentityUserClaim<string>>();
            //builder.Ignore<IdentityUserLogin<string>>();
            //builder.Ignore<IdentityUserRole<string>>();
            //builder.Ignore<IdentityUserToken<string>>();
            //builder.Ignore<IdentityRoleClaim<string>>();
            //builder.Ignore<IdentityRole>();

            builder.Entity<Role>().ToTable("Roles");

            builder.Entity<Role>().HasData(
                new Role { Id = "1", Name = "Admin", NormalizedName = "ADMIN" },
                new Role { Id = "2", Name = "Staff", NormalizedName = "STAFF" },
                new Role { Id = "3", Name = "Customer", NormalizedName = "CUSTOMER" }
            );
        }
    }

    public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
    {
        public DataContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
            optionsBuilder.UseSqlServer("Data Source=DESKTOP-BJES0NE;Initial Catalog=RuychData;Integrated Security=True;Encrypt=True;Trust Server Certificate=True;Connection Timeout=30");

            return new DataContext(optionsBuilder.Options);
        }
    }


}