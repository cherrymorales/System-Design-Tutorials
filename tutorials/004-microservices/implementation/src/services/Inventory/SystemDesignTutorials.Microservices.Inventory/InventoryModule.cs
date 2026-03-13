using MassTransit;
using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.Microservices.BuildingBlocks;
using SystemDesignTutorials.Microservices.Contracts;

namespace SystemDesignTutorials.Microservices.Inventory;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationLine> ReservationLines => Set<ReservationLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StockItem>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.Sku, item.WarehouseCode }).IsUnique();
            entity.Property(item => item.Sku).HasMaxLength(80);
            entity.Property(item => item.WarehouseCode).HasMaxLength(80);
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(reservation => reservation.Id);
            entity.HasIndex(reservation => reservation.OrderId).IsUnique();
            entity.Property(reservation => reservation.WarehouseCode).HasMaxLength(80);
            entity.Property(reservation => reservation.FailureReason).HasMaxLength(500);
        });

        modelBuilder.Entity<ReservationLine>(entity =>
        {
            entity.HasKey(line => line.Id);
            entity.Property(line => line.Sku).HasMaxLength(80);
            entity.HasOne<Reservation>().WithMany(reservation => reservation.Lines).HasForeignKey(line => line.ReservationId);
        });
    }
}

public sealed class StockItem
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public int AvailableQuantity { get; set; }
    public int ReservedQuantity { get; set; }
}

public sealed class Reservation
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public ReservationStatus Status { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }
    public List<ReservationLine> Lines { get; set; } = [];
}

public sealed class ReservationLine
{
    public Guid Id { get; set; }
    public Guid ReservationId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public sealed class InventorySeeder : IDatabaseSeeder<InventoryDbContext>
{
    public async Task SeedAsync(InventoryDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.StockItems.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.StockItems.AddRange(
            Create("SKU-HEADSET-001", "MEL-DC", 18),
            Create("SKU-MOUSE-002", "MEL-DC", 30),
            Create("SKU-DOCK-003", "MEL-DC", 12),
            Create("SKU-HEADSET-001", "SYD-DC", 8),
            Create("SKU-MOUSE-002", "SYD-DC", 14),
            Create("SKU-DOCK-003", "SYD-DC", 6));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static StockItem Create(string sku, string warehouseCode, int availableQuantity)
    {
        return new StockItem
        {
            Id = Guid.NewGuid(),
            Sku = sku,
            WarehouseCode = warehouseCode,
            AvailableQuantity = availableQuantity,
            ReservedQuantity = 0,
        };
    }
}

public sealed class InventoryService(InventoryDbContext dbContext)
{
    public async Task<AvailabilityDto?> GetAvailabilityAsync(string sku, CancellationToken cancellationToken)
    {
        var stock = await dbContext.StockItems
            .AsNoTracking()
            .Where(item => item.Sku == sku)
            .OrderBy(item => item.WarehouseCode)
            .ToListAsync(cancellationToken);

        if (stock.Count == 0)
        {
            return null;
        }

        return new AvailabilityDto(
            sku,
            stock.Sum(item => item.AvailableQuantity),
            stock.Sum(item => item.ReservedQuantity),
            stock.Select(item => new AvailabilityByWarehouseDto(item.WarehouseCode, item.AvailableQuantity, item.ReservedQuantity)).ToList());
    }

    public async Task HandleOrderSubmittedAsync(OrderSubmittedIntegrationEvent message, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken)
    {
        var requestedSkus = message.Lines.Select(line => line.Sku).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var existingReservation = await dbContext.Reservations
            .Include(reservation => reservation.Lines)
            .SingleOrDefaultAsync(reservation => reservation.OrderId == message.OrderId, cancellationToken);

        if (existingReservation is not null)
        {
            return;
        }

        var stockItems = await dbContext.StockItems
            .Where(item => requestedSkus.Contains(item.Sku))
            .OrderBy(item => item.WarehouseCode)
            .ToListAsync(cancellationToken);

        var warehouseCode = "MEL-DC";
        var failureReason = message.CustomerReference.Contains("FAIL-STOCK", StringComparison.OrdinalIgnoreCase)
            ? "Inventory scenario forced to fail by customer reference."
            : GetInsufficientStockReason(message, stockItems, warehouseCode);

        if (failureReason is not null)
        {
            dbContext.Reservations.Add(new Reservation
            {
                Id = Guid.NewGuid(),
                OrderId = message.OrderId,
                Status = ReservationStatus.Rejected,
                WarehouseCode = warehouseCode,
                FailureReason = failureReason,
                CreatedAt = DateTimeOffset.UtcNow,
                Lines = message.Lines.Select(line => new ReservationLine
                {
                    Id = Guid.NewGuid(),
                    Sku = line.Sku,
                    Quantity = line.Quantity,
                }).ToList(),
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await publishEndpoint.Publish(
                new InventoryReservationRejectedIntegrationEvent(message.OrderId, failureReason, DateTimeOffset.UtcNow),
                cancellationToken);
            return;
        }

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            OrderId = message.OrderId,
            Status = ReservationStatus.Reserved,
            WarehouseCode = warehouseCode,
            CreatedAt = DateTimeOffset.UtcNow,
            Lines = message.Lines.Select(line => new ReservationLine
            {
                Id = Guid.NewGuid(),
                Sku = line.Sku,
                Quantity = line.Quantity,
            }).ToList(),
        };

        foreach (var line in message.Lines)
        {
            var stockItem = stockItems.Single(item => item.Sku == line.Sku && item.WarehouseCode == warehouseCode);
            stockItem.AvailableQuantity -= line.Quantity;
            stockItem.ReservedQuantity += line.Quantity;
        }

        dbContext.Reservations.Add(reservation);
        await dbContext.SaveChangesAsync(cancellationToken);

        await publishEndpoint.Publish(
            new InventoryReservedIntegrationEvent(message.OrderId, reservation.Id, warehouseCode, DateTimeOffset.UtcNow),
            cancellationToken);
    }

    public async Task HandleReleaseRequestedAsync(ReservationReleaseRequestedIntegrationEvent message, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken)
    {
        var reservation = await dbContext.Reservations.Include(candidate => candidate.Lines)
            .SingleOrDefaultAsync(candidate => candidate.OrderId == message.OrderId, cancellationToken);

        if (reservation is null || reservation.Status != ReservationStatus.Reserved)
        {
            return;
        }

        var reservedSkus = reservation.Lines.Select(line => line.Sku).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var stockItems = await dbContext.StockItems
            .Where(item => reservedSkus.Contains(item.Sku) && item.WarehouseCode == reservation.WarehouseCode)
            .ToListAsync(cancellationToken);

        foreach (var line in reservation.Lines)
        {
            var stockItem = stockItems.Single(item => item.Sku == line.Sku);
            stockItem.AvailableQuantity += line.Quantity;
            stockItem.ReservedQuantity -= line.Quantity;
        }

        reservation.Status = ReservationStatus.Released;
        reservation.ReleasedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await publishEndpoint.Publish(
            new ReservationReleasedIntegrationEvent(message.OrderId, reservation.Id, reservation.ReleasedAt.Value),
            cancellationToken);
    }

    private static string? GetInsufficientStockReason(
        OrderSubmittedIntegrationEvent message,
        IReadOnlyCollection<StockItem> stockItems,
        string warehouseCode)
    {
        foreach (var line in message.Lines)
        {
            var stockItem = stockItems.SingleOrDefault(item => item.Sku == line.Sku && item.WarehouseCode == warehouseCode);
            if (stockItem is null || stockItem.AvailableQuantity < line.Quantity)
            {
                return $"Insufficient stock for {line.Sku} in {warehouseCode}.";
            }
        }

        return null;
    }
}
