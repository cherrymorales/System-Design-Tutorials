using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.ModularMonolith.Domain.Modules.Billing;
using SystemDesignTutorials.ModularMonolith.Domain.Modules.Catalog;
using SystemDesignTutorials.ModularMonolith.Domain.Modules.Customers;
using SystemDesignTutorials.ModularMonolith.Domain.Modules.Inventory;
using SystemDesignTutorials.ModularMonolith.Domain.Modules.Orders;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Identity;

namespace SystemDesignTutorials.ModularMonolith.Infrastructure.Persistence;

public sealed class ModularMonolithDbContext(DbContextOptions<ModularMonolithDbContext> options)
    : IdentityDbContext<AppIdentityUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();
    public DbSet<InventoryReservationLine> InventoryReservationLines => Set<InventoryReservationLine>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers", "customers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AccountCode).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.BillingContactName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.BillingContactEmail).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ShippingContactName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ShippingContactEmail).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.AccountCode).IsUnique();
        });

        builder.Entity<Product>(entity =>
        {
            entity.ToTable("products", "catalog");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Sku).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(128).IsRequired();
            entity.Property(x => x.UnitPrice).HasPrecision(12, 2);
            entity.HasIndex(x => x.Sku).IsUnique();
        });

        builder.Entity<Warehouse>(entity =>
        {
            entity.ToTable("warehouses", "inventory");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
            entity.Property(x => x.City).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
        });

        builder.Entity<StockItem>(entity =>
        {
            entity.ToTable("stock_items", "inventory");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ProductId, x.WarehouseId }).IsUnique();
        });

        builder.Entity<InventoryReservation>(entity =>
        {
            entity.ToTable("reservations", "inventory");
            entity.HasKey(x => x.Id);
            entity.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.InventoryReservationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<InventoryReservationLine>(entity =>
        {
            entity.ToTable("reservation_lines", "inventory");
            entity.HasKey(x => x.Id);
        });

        builder.Entity<Order>(entity =>
        {
            entity.ToTable("orders", "orders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            entity.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey("OrderId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OrderLine>(entity =>
        {
            entity.ToTable("order_lines", "orders");
            entity.HasKey(x => x.Id);
            entity.Property<Guid>("OrderId");
            entity.Property(x => x.UnitPrice).HasPrecision(12, 2);
        });

        builder.Entity<Invoice>(entity =>
        {
            entity.ToTable("invoices", "billing");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.InvoiceNumber).HasMaxLength(32).IsRequired();
            entity.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            entity.Property(x => x.IssuedBy).HasMaxLength(256);
            entity.Property(x => x.PaidBy).HasMaxLength(256);
            entity.Property(x => x.TotalAmount).HasPrecision(12, 2);
            entity.HasIndex(x => x.InvoiceNumber).IsUnique();
            entity.HasIndex(x => x.OrderId).IsUnique();
        });
    }
}
