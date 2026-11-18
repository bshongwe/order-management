using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Orders;

namespace OrderManagement.Infrastructure.Persistence;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(order => order.Id);
            entity.Property(order => order.CustomerReference).HasMaxLength(64).IsRequired();
            entity.Property(order => order.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(order => order.CreatedAtUtc).IsRequired();
            entity.Property<byte[]>("RowVersion").IsRowVersion();
            entity.HasIndex(order => order.CustomerReference);
        });
    }
}
