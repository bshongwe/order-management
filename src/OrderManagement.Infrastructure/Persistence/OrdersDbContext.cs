using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Orders;
using OrderManagement.Infrastructure.Messaging;
namespace OrderManagement.Infrastructure.Persistence;
public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options):DbContext(options)
{
    public DbSet<Customer> Customers=>Set<Customer>();
    public DbSet<Order> Orders=>Set<Order>();
    public DbSet<OrderLineItem> OrderLineItems=>Set<OrderLineItem>();
    public DbSet<IdempotencyRecord> IdempotencyRecords=>Set<IdempotencyRecord>();
    public DbSet<OutboxMessage> OutboxMessages=>Set<OutboxMessage>();
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken=default)
    {
        AddOrderProcessingOutboxMessages();
        return base.SaveChangesAsync(cancellationToken);
    }
    private void AddOrderProcessingOutboxMessages()
    {
        ChangeTracker.DetectChanges();
        foreach(var entry in ChangeTracker.Entries<Order>().Where(x=>x.State==EntityState.Modified&&x.Property(o=>o.Status).IsModified))
        {
            var original=entry.Property(x=>x.Status).OriginalValue;
            if(original==OrderStatus.Processing||entry.Entity.Status!=OrderStatus.Processing)continue;
            var now=DateTimeOffset.UtcNow;
            var message=new OrderProcessingRequested(entry.Entity.Id,now);
            OutboxMessages.Add(new OutboxMessage("order.processing.requested",JsonSerializer.Serialize(message),now));
        }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity=>
        {
            entity.ToTable("Customers"); entity.HasKey(x=>x.Id);
            entity.Property(x=>x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x=>x.Email).HasMaxLength(254).IsRequired();
            entity.Property(x=>x.CountryCode).HasColumnType("char(2)").IsFixedLength().IsUnicode(false).HasMaxLength(2).IsRequired();
            entity.Property(x=>x.CreatedAtUtc).IsRequired();
            entity.HasIndex(x=>x.Email).IsUnique(); entity.HasIndex(x=>x.CountryCode);
        });
        modelBuilder.Entity<Order>(entity=>
        {
            entity.ToTable("Orders"); entity.HasKey(x=>x.Id);
            entity.Property(x=>x.CustomerReference).HasMaxLength(64).IsRequired();
            entity.Property(x=>x.CurrencyCode).HasColumnType("char(3)").IsFixedLength().IsUnicode(false).HasMaxLength(3);
            entity.Property(x=>x.TotalAmount).HasPrecision(18,2);
            entity.Property(x=>x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x=>x.CreatedAtUtc).IsRequired();
            entity.Property<byte[]>("RowVersion").IsRowVersion();
            entity.HasOne<Customer>().WithMany().HasForeignKey(x=>x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x=>x.CustomerReference);
            entity.HasIndex(x=>new { x.CustomerId,x.Status,x.CreatedAtUtc });
            entity.HasMany(x=>x.LineItems).WithOne().HasForeignKey(x=>x.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x=>x.LineItems).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        modelBuilder.Entity<OrderLineItem>(entity=>
        {
            entity.ToTable("OrderLineItems"); entity.HasKey(x=>x.Id);
            entity.Property(x=>x.ProductSku).HasMaxLength(64).IsRequired();
            entity.Property(x=>x.UnitPrice).HasPrecision(18,2).IsRequired();
            entity.Ignore(x=>x.LineTotal); entity.HasIndex(x=>x.OrderId);
        });
        modelBuilder.Entity<IdempotencyRecord>(entity=>
        {
            entity.ToTable("IdempotencyRecords"); entity.HasKey(x=>x.Id);
            entity.Property(x=>x.Key).HasMaxLength(128).IsRequired();
            entity.Property(x=>x.RequestFingerprint).HasMaxLength(256).IsRequired();
            entity.Property(x=>x.ResponseType).HasMaxLength(256).IsRequired();
            entity.Property(x=>x.ResponseJson).IsRequired(); entity.Property(x=>x.CreatedAtUtc).IsRequired();
            entity.HasIndex(x=>x.Key).IsUnique();
        });
        modelBuilder.Entity<OutboxMessage>(entity=>
        {
            entity.ToTable("OutboxMessages"); entity.HasKey(x=>x.Id);
            entity.Property(x=>x.EventType).HasMaxLength(200).IsRequired();
            entity.Property(x=>x.Payload).IsRequired();
            entity.Property(x=>x.LastError).HasMaxLength(2000);
            entity.Property(x=>x.OccurredAtUtc).IsRequired();
            entity.Property(x=>x.NextAttemptAtUtc).IsRequired();
            entity.HasIndex(x=>new{x.PublishedAtUtc,x.DeadLetteredAtUtc,x.NextAttemptAtUtc});
        });
    }
}