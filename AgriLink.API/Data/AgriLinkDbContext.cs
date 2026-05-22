using Microsoft.EntityFrameworkCore;
using AgriLink.API.Models.Entities;

namespace AgriLink.API.Data;

public class AgriLinkDbContext : DbContext
{
    public AgriLinkDbContext(DbContextOptions<AgriLinkDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<FarmerProfile> FarmerProfiles => Set<FarmerProfile>();
    public DbSet<BuyerProfile> BuyerProfiles => Set<BuyerProfile>();
    public DbSet<BuyerType> BuyerTypes => Set<BuyerType>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<HarvestLog> HarvestLogs => Set<HarvestLog>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().Property(e => e.Role).HasConversion<string>();
        modelBuilder.Entity<BuyerType>().Property(e => e.Name).HasConversion<string>();

        modelBuilder.Entity<User>().HasOne(u => u.FarmerProfile).WithOne(fp => fp.User)
            .HasForeignKey<FarmerProfile>(fp => fp.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<User>().HasOne(u => u.BuyerProfile).WithOne(bp => bp.User)
            .HasForeignKey<BuyerProfile>(bp => bp.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<BuyerProfile>().HasOne(bp => bp.BuyerType).WithMany().HasForeignKey(bp => bp.BuyerTypeId);
        modelBuilder.Entity<Product>().HasOne(p => p.Farm).WithMany().HasForeignKey(p => p.FarmId);
        modelBuilder.Entity<Product>().HasOne(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId);
        modelBuilder.Entity<Order>().HasOne(o => o.Buyer).WithMany().HasForeignKey(o => o.BuyerId);
        modelBuilder.Entity<Order>().HasOne(o => o.Farmer).WithMany().HasForeignKey(o => o.FarmerId);
        modelBuilder.Entity<HarvestLog>().HasOne(h => h.Farm).WithMany().HasForeignKey(h => h.FarmId);
        modelBuilder.Entity<Conversation>().HasOne(c => c.Farmer).WithMany().HasForeignKey(c => c.FarmerId);
        modelBuilder.Entity<Conversation>().HasOne(c => c.Buyer).WithMany().HasForeignKey(c => c.BuyerId);
        modelBuilder.Entity<Message>().HasOne(m => m.Conversation).WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CartItem>().HasOne(ci => ci.Buyer).WithMany().HasForeignKey(ci => ci.BuyerId);
        modelBuilder.Entity<CartItem>().HasOne(ci => ci.Product).WithMany().HasForeignKey(ci => ci.ProductId);
        modelBuilder.Entity<CartItem>().HasIndex(ci => new { ci.BuyerId, ci.ProductId }).IsUnique();

        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        { property.SetPrecision(10); property.SetScale(2); }
    }
}