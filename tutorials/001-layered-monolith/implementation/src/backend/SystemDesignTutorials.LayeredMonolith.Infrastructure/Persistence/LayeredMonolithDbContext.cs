using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.LayeredMonolith.Domain.Entities;
using SystemDesignTutorials.LayeredMonolith.Infrastructure.Identity;

namespace SystemDesignTutorials.LayeredMonolith.Infrastructure.Persistence;

public sealed class LayeredMonolithDbContext(DbContextOptions<LayeredMonolithDbContext> options)
    : IdentityDbContext<AppIdentityUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryReceipt> InventoryReceipts => Set<InventoryReceipt>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<InventoryAdjustment> InventoryAdjustments => Set<InventoryAdjustment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Product>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Sku).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(128).IsRequired();
            entity.Property(x => x.SupplierCode).HasMaxLength(128).IsRequired();
            entity.Property(x => x.UnitCost).HasPrecision(12, 2);
            entity.HasIndex(x => x.Sku).IsUnique();
        });

        builder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
            entity.Property(x => x.City).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
        });

        builder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ProductId, x.WarehouseId }).IsUnique();
        });

        builder.Entity<InventoryReceipt>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SupplierReference).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ReceivedBy).HasMaxLength(256).IsRequired();
        });

        builder.Entity<StockTransfer>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RequestedBy).HasMaxLength(256).IsRequired();
            entity.Property(x => x.ApprovedBy).HasMaxLength(256);
            entity.Property(x => x.DispatchedBy).HasMaxLength(256);
            entity.Property(x => x.ReceivedBy).HasMaxLength(256);
            entity.Property(x => x.CancelledBy).HasMaxLength(256);
            entity.Property(x => x.Reason).HasMaxLength(256).IsRequired();
            entity.Property(x => x.CancellationReason).HasMaxLength(256);
        });

        builder.Entity<InventoryAdjustment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ReasonCode).HasMaxLength(128).IsRequired();
            entity.Property(x => x.SubmittedBy).HasMaxLength(256).IsRequired();
            entity.Property(x => x.ApprovedBy).HasMaxLength(256);
            entity.Property(x => x.RejectedBy).HasMaxLength(256);
            entity.Property(x => x.Notes).HasMaxLength(512);
        });
    }
}
