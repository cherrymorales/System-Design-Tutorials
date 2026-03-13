using MassTransit;
using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.Microservices.BuildingBlocks;
using SystemDesignTutorials.Microservices.Contracts;

namespace SystemDesignTutorials.Microservices.Orders;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(order => order.Id);
            entity.HasIndex(order => order.OrderNumber).IsUnique();
            entity.Property(order => order.OrderNumber).HasMaxLength(80);
            entity.Property(order => order.CustomerReference).HasMaxLength(120);
            entity.Property(order => order.Currency).HasMaxLength(10);
            entity.Property(order => order.CreatedByEmail).HasMaxLength(200);
            entity.Property(order => order.FailureReason).HasMaxLength(500);
        });

        modelBuilder.Entity<OrderLine>(entity =>
        {
            entity.HasKey(line => line.Id);
            entity.Property(line => line.Sku).HasMaxLength(80);
            entity.Property(line => line.ProductName).HasMaxLength(200);
            entity.HasOne<Order>().WithMany(order => order.Lines).HasForeignKey(line => line.OrderId);
        });
    }
}

public sealed class Order
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerReference { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public ReservationStatus ReservationStatus { get; set; } = ReservationStatus.Pending;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public ShipmentStatus? ShipmentStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string CreatedByEmail { get; set; } = string.Empty;
    public Guid? ShipmentId { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<OrderLine> Lines { get; set; } = [];
}

public sealed class OrderLine
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public sealed class OrdersSeeder : IDatabaseSeeder<OrdersDbContext>
{
    public Task SeedAsync(OrdersDbContext dbContext, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class OrdersService(OrdersDbContext dbContext)
{
    public async Task<IReadOnlyCollection<OrderSummaryDto>> GetOrdersAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Orders
            .AsNoTracking()
            .OrderByDescending(order => order.UpdatedAt)
            .Select(order => new OrderSummaryDto(
                order.Id,
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
        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(candidate => candidate.Lines)
            .SingleOrDefaultAsync(candidate => candidate.Id == orderId, cancellationToken);

        return order is null ? null : Map(order);
    }

    public async Task<OrderDetailDto> CreateDraftAsync(InternalCreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerReference))
        {
            throw new InvalidOperationException("Customer reference is required.");
        }

        if (request.Lines.Count == 0 || request.Lines.Any(line => line.Quantity <= 0))
        {
            throw new InvalidOperationException("A draft order requires at least one valid line.");
        }

        var nextSequence = await dbContext.Orders.CountAsync(cancellationToken) + 1;
        var now = DateTimeOffset.UtcNow;
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"ORD-{now:yyyyMMdd}-{nextSequence:0000}",
            CustomerReference = request.CustomerReference,
            Currency = request.Currency,
            CreatedByUserId = request.CreatedByUserId,
            CreatedByEmail = request.CreatedByEmail,
            CreatedAt = now,
            UpdatedAt = now,
            TotalAmount = request.Lines.Sum(line => line.UnitPrice * line.Quantity),
            Lines = request.Lines.Select(line => new OrderLine
            {
                Id = Guid.NewGuid(),
                Sku = line.Sku,
                ProductName = line.ProductName,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
            }).ToList(),
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(order);
    }

    public async Task<OrderDetailDto?> SubmitAsync(Guid orderId, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.Include(candidate => candidate.Lines)
            .SingleOrDefaultAsync(candidate => candidate.Id == orderId, cancellationToken);

        if (order is null)
        {
            return null;
        }

        if (order.Status != OrderStatus.Draft)
        {
            throw new InvalidOperationException("Only draft orders can be submitted.");
        }

        order.Status = OrderStatus.AwaitingDependencies;
        order.ReservationStatus = ReservationStatus.Pending;
        order.PaymentStatus = PaymentStatus.Pending;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await publishEndpoint.Publish(
            new OrderSubmittedIntegrationEvent(
                order.Id,
                order.OrderNumber,
                order.CustomerReference,
                order.Currency,
                order.TotalAmount,
                order.CreatedByEmail,
                order.Lines.Select(line => new SubmittedOrderLine(line.Sku, line.ProductName, line.Quantity, line.UnitPrice)).ToArray(),
                order.UpdatedAt),
            cancellationToken);

        return Map(order);
    }

    public async Task<OrderDetailDto?> CancelAsync(Guid orderId, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.Include(candidate => candidate.Lines)
            .SingleOrDefaultAsync(candidate => candidate.Id == orderId, cancellationToken);

        if (order is null)
        {
            return null;
        }

        if (order.Status is OrderStatus.Completed or OrderStatus.Cancelled)
        {
            return Map(order);
        }

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (order.ReservationStatus == ReservationStatus.Reserved)
        {
            await publishEndpoint.Publish(
                new ReservationReleaseRequestedIntegrationEvent(order.Id, "Order cancelled by operator.", DateTimeOffset.UtcNow),
                cancellationToken);
        }

        if (order.PaymentStatus == PaymentStatus.Authorized)
        {
            await publishEndpoint.Publish(
                new PaymentVoidRequestedIntegrationEvent(order.Id, "Order cancelled by operator.", DateTimeOffset.UtcNow),
                cancellationToken);
        }

        await publishEndpoint.Publish(
            new NotificationRequestedIntegrationEvent(order.Id, "OrderCancelled", $"Order {order.OrderNumber} was cancelled.", DateTimeOffset.UtcNow),
            cancellationToken);

        return Map(order);
    }

    public async Task HandleInventoryReservedAsync(InventoryReservedIntegrationEvent message, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(candidate => candidate.Id == message.OrderId, cancellationToken);
        if (order is null || order.ReservationStatus == ReservationStatus.Reserved)
        {
            return;
        }

        order.ReservationStatus = ReservationStatus.Reserved;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        if (order.Status == OrderStatus.Failed || order.Status == OrderStatus.Cancelled)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await publishEndpoint.Publish(
                new ReservationReleaseRequestedIntegrationEvent(order.Id, "Late reservation received for terminal order.", DateTimeOffset.UtcNow),
                cancellationToken);
            return;
        }

        var isReady = TryMarkReadyForFulfillment(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (isReady)
        {
            await PublishReadyForFulfillmentAsync(order, publishEndpoint, cancellationToken);
        }
    }

    public async Task HandleInventoryRejectedAsync(InventoryReservationRejectedIntegrationEvent message, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(candidate => candidate.Id == message.OrderId, cancellationToken);
        if (order is null || order.ReservationStatus == ReservationStatus.Rejected)
        {
            return;
        }

        order.ReservationStatus = ReservationStatus.Rejected;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        var publishCompensation = false;
        if (order.Status is not OrderStatus.Failed and not OrderStatus.Cancelled and not OrderStatus.Completed)
        {
            order.Status = OrderStatus.Failed;
            order.FailureReason = message.Reason;
            publishCompensation = order.PaymentStatus == PaymentStatus.Authorized;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (publishCompensation)
        {
            await publishEndpoint.Publish(
                new PaymentVoidRequestedIntegrationEvent(order.Id, "Inventory reservation failed.", DateTimeOffset.UtcNow),
                cancellationToken);
        }

        await PublishSubmissionFailedAsync(order, message.Reason, publishEndpoint, cancellationToken);
    }

    public async Task HandlePaymentAuthorizedAsync(PaymentAuthorizedIntegrationEvent message, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(candidate => candidate.Id == message.OrderId, cancellationToken);
        if (order is null || order.PaymentStatus == PaymentStatus.Authorized)
        {
            return;
        }

        order.PaymentStatus = PaymentStatus.Authorized;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        if (order.Status == OrderStatus.Failed || order.Status == OrderStatus.Cancelled)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await publishEndpoint.Publish(
                new PaymentVoidRequestedIntegrationEvent(order.Id, "Late payment authorization received for terminal order.", DateTimeOffset.UtcNow),
                cancellationToken);
            return;
        }

        var isReady = TryMarkReadyForFulfillment(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (isReady)
        {
            await PublishReadyForFulfillmentAsync(order, publishEndpoint, cancellationToken);
        }
    }

    public async Task HandlePaymentFailedAsync(PaymentAuthorizationFailedIntegrationEvent message, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(candidate => candidate.Id == message.OrderId, cancellationToken);
        if (order is null || order.PaymentStatus == PaymentStatus.Failed)
        {
            return;
        }

        order.PaymentStatus = PaymentStatus.Failed;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        var publishCompensation = false;
        if (order.Status is not OrderStatus.Failed and not OrderStatus.Cancelled and not OrderStatus.Completed)
        {
            order.Status = OrderStatus.Failed;
            order.FailureReason = message.Reason;
            publishCompensation = order.ReservationStatus == ReservationStatus.Reserved;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (publishCompensation)
        {
            await publishEndpoint.Publish(
                new ReservationReleaseRequestedIntegrationEvent(order.Id, "Payment authorization failed.", DateTimeOffset.UtcNow),
                cancellationToken);
        }

        await PublishSubmissionFailedAsync(order, message.Reason, publishEndpoint, cancellationToken);
    }

    public async Task HandleShipmentCreatedAsync(ShipmentCreatedIntegrationEvent message, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(candidate => candidate.Id == message.OrderId, cancellationToken);
        if (order is null)
        {
            return;
        }

        order.ShipmentId ??= message.ShipmentId;
        order.ShipmentStatus = ShipmentStatus.Pending;
        if (order.Status == OrderStatus.ReadyForFulfillment)
        {
            order.Status = OrderStatus.FulfillmentInProgress;
        }

        order.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleShipmentDeliveredAsync(ShipmentDeliveredIntegrationEvent message, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(candidate => candidate.Id == message.OrderId, cancellationToken);
        if (order is null)
        {
            return;
        }

        order.ShipmentStatus = ShipmentStatus.Delivered;
        order.Status = OrderStatus.Completed;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static OrderDetailDto Map(Order order)
    {
        return new OrderDetailDto(
            order.Id,
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
            order.Lines
                .OrderBy(line => line.ProductName)
                .Select(line => new OrderLineDto(
                    line.Sku,
                    line.ProductName,
                    line.Quantity,
                    line.UnitPrice,
                    line.Quantity * line.UnitPrice))
                .ToList());
    }

    private static bool TryMarkReadyForFulfillment(Order order)
    {
        if (order.ReservationStatus == ReservationStatus.Reserved
            && order.PaymentStatus == PaymentStatus.Authorized
            && order.Status == OrderStatus.AwaitingDependencies)
        {
            order.Status = OrderStatus.ReadyForFulfillment;
            order.FailureReason = null;
            return true;
        }

        return false;
    }

    private static async Task PublishReadyForFulfillmentAsync(Order order, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken)
    {
        await publishEndpoint.Publish(
            new OrderReadyForFulfillmentIntegrationEvent(order.Id, order.OrderNumber, DateTimeOffset.UtcNow),
            cancellationToken);

        await publishEndpoint.Publish(
            new NotificationRequestedIntegrationEvent(order.Id, "OrderReady", $"Order {order.OrderNumber} is ready for fulfillment.", DateTimeOffset.UtcNow),
            cancellationToken);
    }

    private static async Task PublishSubmissionFailedAsync(Order order, string reason, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken)
    {
        await publishEndpoint.Publish(
            new OrderSubmissionFailedIntegrationEvent(order.Id, reason, DateTimeOffset.UtcNow),
            cancellationToken);

        await publishEndpoint.Publish(
            new NotificationRequestedIntegrationEvent(order.Id, "OrderFailed", $"Order {order.OrderNumber} failed: {reason}", DateTimeOffset.UtcNow),
            cancellationToken);
    }
}
