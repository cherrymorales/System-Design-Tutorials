using MassTransit;
using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.Microservices.BuildingBlocks;
using SystemDesignTutorials.Microservices.Contracts;

namespace SystemDesignTutorials.Microservices.Fulfillment;

public sealed class FulfillmentDbContext(DbContextOptions<FulfillmentDbContext> options) : DbContext(options)
{
    public DbSet<Shipment> Shipments => Set<Shipment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.HasKey(shipment => shipment.Id);
            entity.HasIndex(shipment => shipment.OrderId).IsUnique();
            entity.Property(shipment => shipment.OrderNumber).HasMaxLength(80);
            entity.Property(shipment => shipment.WarehouseCode).HasMaxLength(80);
            entity.Property(shipment => shipment.TrackingReference).HasMaxLength(120);
        });
    }
}

public sealed class Shipment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Pending;
    public string WarehouseCode { get; set; } = string.Empty;
    public string TrackingReference { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class FulfillmentSeeder : IDatabaseSeeder<FulfillmentDbContext>
{
    public Task SeedAsync(FulfillmentDbContext dbContext, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class FulfillmentService(FulfillmentDbContext dbContext)
{
    public async Task<IReadOnlyCollection<ShipmentDto>> GetShipmentsAsync(ShipmentStatus? status, CancellationToken cancellationToken)
    {
        var query = dbContext.Shipments.AsNoTracking().AsQueryable();
        if (status is not null)
        {
            query = query.Where(shipment => shipment.Status == status);
        }

        var shipments = await query
            .Select(shipment => new ShipmentDto(
                shipment.Id,
                shipment.OrderId,
                shipment.OrderNumber,
                shipment.Status,
                shipment.WarehouseCode,
                shipment.TrackingReference,
                shipment.UpdatedAt))
            .ToListAsync(cancellationToken);

        return shipments.OrderByDescending(shipment => shipment.UpdatedAt).ToList();
    }

    public async Task<ShipmentDto?> GetShipmentAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        return await dbContext.Shipments.AsNoTracking()
            .Where(shipment => shipment.Id == shipmentId)
            .Select(shipment => new ShipmentDto(
                shipment.Id,
                shipment.OrderId,
                shipment.OrderNumber,
                shipment.Status,
                shipment.WarehouseCode,
                shipment.TrackingReference,
                shipment.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task HandleReadyForFulfillmentAsync(OrderReadyForFulfillmentIntegrationEvent message, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken)
    {
        var existingShipment = await dbContext.Shipments.SingleOrDefaultAsync(shipment => shipment.OrderId == message.OrderId, cancellationToken);
        if (existingShipment is not null)
        {
            return;
        }

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = message.OrderId,
            OrderNumber = message.OrderNumber,
            WarehouseCode = "MEL-DC",
            TrackingReference = $"TRK-{message.OrderNumber}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.Shipments.Add(shipment);
        await dbContext.SaveChangesAsync(cancellationToken);

        await publishEndpoint.Publish(
            new ShipmentCreatedIntegrationEvent(
                shipment.OrderId,
                shipment.Id,
                shipment.OrderNumber,
                shipment.WarehouseCode,
                shipment.TrackingReference,
                shipment.CreatedAt),
            cancellationToken);
    }

    public async Task<ShipmentDto?> ProgressAsync(Guid shipmentId, ShipmentStatus targetStatus, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken)
    {
        var shipment = await dbContext.Shipments.SingleOrDefaultAsync(candidate => candidate.Id == shipmentId, cancellationToken);
        if (shipment is null)
        {
            return null;
        }

        var validTransition = (shipment.Status, targetStatus) switch
        {
            (ShipmentStatus.Pending, ShipmentStatus.Picking) => true,
            (ShipmentStatus.Picking, ShipmentStatus.Packed) => true,
            (ShipmentStatus.Packed, ShipmentStatus.Shipped) => true,
            (ShipmentStatus.Shipped, ShipmentStatus.Delivered) => true,
            _ => false,
        };

        if (!validTransition)
        {
            throw new InvalidOperationException($"Cannot move shipment from {shipment.Status} to {targetStatus}.");
        }

        shipment.Status = targetStatus;
        shipment.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await publishEndpoint.Publish(
            new ShipmentStatusChangedIntegrationEvent(shipment.OrderId, shipment.Id, shipment.Status, shipment.UpdatedAt),
            cancellationToken);

        if (shipment.Status == ShipmentStatus.Delivered)
        {
            await publishEndpoint.Publish(
                new ShipmentDeliveredIntegrationEvent(shipment.OrderId, shipment.Id, shipment.UpdatedAt),
                cancellationToken);
            await publishEndpoint.Publish(
                new NotificationRequestedIntegrationEvent(shipment.OrderId, "ShipmentDelivered", $"Shipment {shipment.TrackingReference} was delivered.", shipment.UpdatedAt),
                cancellationToken);
        }

        return new ShipmentDto(
            shipment.Id,
            shipment.OrderId,
            shipment.OrderNumber,
            shipment.Status,
            shipment.WarehouseCode,
            shipment.TrackingReference,
            shipment.UpdatedAt);
    }
}
