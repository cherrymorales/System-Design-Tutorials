using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.Microservices.BuildingBlocks;
using SystemDesignTutorials.Microservices.Contracts;

namespace SystemDesignTutorials.Microservices.OperationsQuery;

public sealed class OperationsQueryDbContext(DbContextOptions<OperationsQueryDbContext> options) : DbContext(options)
{
    public DbSet<OrderProjection> Orders => Set<OrderProjection>();
    public DbSet<OrderProjectionLine> OrderLines => Set<OrderProjectionLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderProjection>(entity =>
        {
            entity.HasKey(order => order.OrderId);
            entity.Property(order => order.OrderNumber).HasMaxLength(80);
            entity.Property(order => order.CustomerReference).HasMaxLength(120);
            entity.Property(order => order.Currency).HasMaxLength(10);
            entity.Property(order => order.CreatedByEmail).HasMaxLength(200);
            entity.Property(order => order.FailureReason).HasMaxLength(500);
        });

        modelBuilder.Entity<OrderProjectionLine>(entity =>
        {
            entity.HasKey(line => line.Id);
            entity.Property(line => line.Sku).HasMaxLength(80);
            entity.Property(line => line.ProductName).HasMaxLength(200);
            entity.HasOne<OrderProjection>().WithMany(order => order.Lines).HasForeignKey(line => line.OrderId);
        });
    }
}

public sealed class OrderProjection
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerReference { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.AwaitingDependencies;
    public ReservationStatus ReservationStatus { get; set; } = ReservationStatus.Pending;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public ShipmentStatus? ShipmentStatus { get; set; }
    public Guid? ShipmentId { get; set; }
    public decimal TotalAmount { get; set; }
    public string CreatedByEmail { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<OrderProjectionLine> Lines { get; set; } = [];
}

public sealed class OrderProjectionLine
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public sealed class OperationsQuerySeeder : IDatabaseSeeder<OperationsQueryDbContext>
{
    public Task SeedAsync(OperationsQueryDbContext dbContext, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class OperationsQueryService(OperationsQueryDbContext dbContext)
{
    public async Task<DashboardSummaryDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var orders = await dbContext.Orders.AsNoTracking().OrderByDescending(order => order.UpdatedAt).ToListAsync(cancellationToken);
        var recent = orders.Take(6).Select(MapSummary).ToList();
        return new DashboardSummaryDto(
            orders.Count,
            orders.Count(order => order.Status == OrderStatus.AwaitingDependencies),
            orders.Count(order => order.Status == OrderStatus.ReadyForFulfillment),
            orders.Count(order => order.Status == OrderStatus.FulfillmentInProgress),
            orders.Count(order => order.Status == OrderStatus.Failed),
            orders.Count(order => order.Status == OrderStatus.Completed),
            recent);
    }

    public async Task<IReadOnlyCollection<OrderSummaryDto>> GetOrdersAsync(OrderStatus? status, CancellationToken cancellationToken)
    {
        var query = dbContext.Orders.AsNoTracking().AsQueryable();
        if (status is not null)
        {
            query = query.Where(order => order.Status == status.Value);
        }

        return await query.OrderByDescending(order => order.UpdatedAt)
            .Select(order => new OrderSummaryDto(
                order.OrderId,
                order.OrderNumber,
                order.CustomerReference,
                order.Status,
                order.ReservationStatus,
                order.PaymentStatus,
                order.ShipmentStatus,
                order.TotalAmount,
                order.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderDetailDto?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.AsNoTracking()
            .Include(candidate => candidate.Lines)
            .SingleOrDefaultAsync(candidate => candidate.OrderId == orderId, cancellationToken);

        return order is null ? null : MapDetail(order);
    }

    public async Task ApplyOrderSubmittedAsync(OrderSubmittedIntegrationEvent message, CancellationToken cancellationToken)
    {
        if (await dbContext.Orders.AnyAsync(order => order.OrderId == message.OrderId, cancellationToken))
        {
            return;
        }

        dbContext.Orders.Add(new OrderProjection
        {
            OrderId = message.OrderId,
            OrderNumber = message.OrderNumber,
            CustomerReference = message.CustomerReference,
            Currency = message.Currency,
            TotalAmount = message.TotalAmount,
            CreatedByEmail = message.RequestedByEmail,
            CreatedAt = message.SubmittedAt,
            UpdatedAt = message.SubmittedAt,
            Lines = message.Lines.Select(line => new OrderProjectionLine
            {
                Id = Guid.NewGuid(),
                Sku = line.Sku,
                ProductName = line.ProductName,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
            }).ToList(),
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApplyInventoryReservedAsync(InventoryReservedIntegrationEvent message, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(candidate => candidate.OrderId == message.OrderId, cancellationToken);
        if (order is null)
        {
            return;
        }

        order.ReservationStatus = ReservationStatus.Reserved;
        order.UpdatedAt = message.ReservedAt;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApplyInventoryRejectedAsync(InventoryReservationRejectedIntegrationEvent message, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(candidate => candidate.OrderId == message.OrderId, cancellationToken);
        if (order is null)
        {
            return;
        }

        order.ReservationStatus = ReservationStatus.Rejected;
        order.Status = OrderStatus.Failed;
        order.FailureReason = message.Reason;
        order.UpdatedAt = message.RejectedAt;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApplyPaymentAuthorizedAsync(PaymentAuthorizedIntegrationEvent message, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(candidate => candidate.OrderId == message.OrderId, cancellationToken);
        if (order is null)
        {
            return;
        }

        order.PaymentStatus = PaymentStatus.Authorized;
        order.UpdatedAt = message.AuthorizedAt;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApplyPaymentFailedAsync(PaymentAuthorizationFailedIntegrationEvent message, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(candidate => candidate.OrderId == message.OrderId, cancellationToken);
        if (order is null)
        {
            return;
        }

        order.PaymentStatus = PaymentStatus.Failed;
        order.Status = OrderStatus.Failed;
        order.FailureReason = message.Reason;
        order.UpdatedAt = message.FailedAt;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApplyReadyAsync(OrderReadyForFulfillmentIntegrationEvent message, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(candidate => candidate.OrderId == message.OrderId, cancellationToken);
        if (order is null)
        {
            return;
        }

        order.Status = OrderStatus.ReadyForFulfillment;
        order.FailureReason = null;
        order.UpdatedAt = message.ReadyAt;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApplyFailedAsync(OrderSubmissionFailedIntegrationEvent message, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(candidate => candidate.OrderId == message.OrderId, cancellationToken);
        if (order is null)
        {
            return;
        }

        order.Status = OrderStatus.Failed;
        order.FailureReason = message.Reason;
        order.UpdatedAt = message.FailedAt;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApplyShipmentCreatedAsync(ShipmentCreatedIntegrationEvent message, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(candidate => candidate.OrderId == message.OrderId, cancellationToken);
        if (order is null)
        {
            return;
        }

        order.ShipmentId = message.ShipmentId;
        order.ShipmentStatus = ShipmentStatus.Pending;
        order.Status = OrderStatus.FulfillmentInProgress;
        order.UpdatedAt = message.CreatedAt;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApplyShipmentStatusChangedAsync(ShipmentStatusChangedIntegrationEvent message, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(candidate => candidate.OrderId == message.OrderId, cancellationToken);
        if (order is null)
        {
            return;
        }

        order.ShipmentId = message.ShipmentId;
        order.ShipmentStatus = message.Status;
        if (message.Status == ShipmentStatus.Delivered)
        {
            order.Status = OrderStatus.Completed;
        }
        else
        {
            order.Status = OrderStatus.FulfillmentInProgress;
        }

        order.UpdatedAt = message.ChangedAt;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static OrderSummaryDto MapSummary(OrderProjection order)
        => new(
            order.OrderId,
            order.OrderNumber,
            order.CustomerReference,
            order.Status,
            order.ReservationStatus,
            order.PaymentStatus,
            order.ShipmentStatus,
            order.TotalAmount,
            order.UpdatedAt);

    private static OrderDetailDto MapDetail(OrderProjection order)
        => new(
            order.OrderId,
            order.OrderNumber,
            order.CustomerReference,
            order.Currency,
            order.Status,
            order.ReservationStatus,
            order.PaymentStatus,
            order.ShipmentStatus,
            order.TotalAmount,
            order.CreatedByEmail,
            order.FailureReason,
            order.ShipmentId,
            order.CreatedAt,
            order.UpdatedAt,
            order.Lines.OrderBy(line => line.ProductName)
                .Select(line => new OrderLineDto(line.Sku, line.ProductName, line.Quantity, line.UnitPrice, line.Quantity * line.UnitPrice))
                .ToList());
}
