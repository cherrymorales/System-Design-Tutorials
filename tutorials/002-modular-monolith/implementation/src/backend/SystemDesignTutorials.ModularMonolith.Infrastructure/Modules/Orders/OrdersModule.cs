using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.ModularMonolith.Application.Modules;
using SystemDesignTutorials.ModularMonolith.Domain.Enums;
using SystemDesignTutorials.ModularMonolith.Domain.Modules.Orders;
using SystemDesignTutorials.ModularMonolith.Domain.Modules.Shared;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Persistence;

namespace SystemDesignTutorials.ModularMonolith.Infrastructure.Modules.Orders;

public sealed class OrdersModule(
    ModularMonolithDbContext dbContext,
    ICustomersModule customersModule,
    ICatalogModule catalogModule,
    IInventoryModule inventoryModule,
    IBillingModule billingModule) : IOrdersModule
{
    public async Task<IReadOnlyList<OrderSummaryDto>> GetOrdersAsync(CancellationToken cancellationToken)
    {
        var orders = await dbContext.Orders.Include(x => x.Lines).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return await ProjectOrdersAsync(orders, cancellationToken);
    }

    public async Task<OrderSummaryDto?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        return (await ProjectOrdersAsync([order], cancellationToken)).Single();
    }

    public async Task<OrderSummaryDto> CreateOrderAsync(CreateOrderCommand command, string actor, CancellationToken cancellationToken)
    {
        if (command.Lines.Count == 0)
        {
            throw new BusinessRuleException("Order must include at least one line.");
        }

        var customer = await customersModule.GetCustomerValidationAsync(command.CustomerId, cancellationToken);
        var productMap = await catalogModule.GetProductValidationMapAsync(command.Lines.Select(x => x.ProductId), cancellationToken);

        var lines = command.Lines.Select(line =>
        {
            if (!productMap.TryGetValue(line.ProductId, out var product))
            {
                throw new KeyNotFoundException("One or more products were not found.");
            }

            if (!product.IsActive)
            {
                throw new BusinessRuleException($"Product {product.Sku} is archived and cannot be used on an order.");
            }

            return new OrderLine(product.Id, line.Quantity, product.UnitPrice);
        }).ToArray();

        _ = customer;
        var order = new Order(command.CustomerId, lines, actor);
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetOrderAsync(order.Id, cancellationToken) ?? throw new InvalidOperationException("Order was not persisted.");
    }

    public async Task<OrderSummaryDto> SubmitOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new KeyNotFoundException("Order not found.");

        order.Submit();
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetOrderAsync(orderId, cancellationToken) ?? throw new InvalidOperationException("Order submit failed.");
    }

    public async Task<OrderSummaryDto> ReserveOrderAsync(Guid orderId, string actor, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new KeyNotFoundException("Order not found.");

        if (order.Status != OrderStatus.Submitted)
        {
            throw new BusinessRuleException("Only submitted orders can reserve inventory.");
        }

        var customer = await customersModule.GetCustomerValidationAsync(order.CustomerId, cancellationToken);
        if (!customer.IsActive)
        {
            throw new BusinessRuleException("Inactive customers cannot reserve inventory.");
        }

        var productMap = await catalogModule.GetProductValidationMapAsync(order.Lines.Select(x => x.ProductId), cancellationToken);
        foreach (var line in order.Lines)
        {
            if (!productMap.TryGetValue(line.ProductId, out var product) || !product.IsActive)
            {
                throw new BusinessRuleException("Archived or missing products cannot be reserved.");
            }
        }

        var reservation = await inventoryModule.ReserveOrderAsync(order.Id, order.Lines.Select(x => new ReservationItemCommand(x.ProductId, x.Quantity)).ToArray(), actor, cancellationToken);
        order.AttachReservation(reservation.ReservationId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetOrderAsync(orderId, cancellationToken) ?? throw new InvalidOperationException("Order reservation failed.");
    }

    public async Task<OrderSummaryDto> MarkReadyForInvoicingAsync(Guid orderId, string actor, CancellationToken cancellationToken)
    {
        _ = actor;
        var order = await dbContext.Orders.SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new KeyNotFoundException("Order not found.");

        order.MarkReadyForInvoicing();
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetOrderAsync(orderId, cancellationToken) ?? throw new InvalidOperationException("Order readiness update failed.");
    }

    public async Task<OrderBillingSnapshotDto> GetOrderForInvoiceAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new KeyNotFoundException("Order not found.");

        if (order.Status != OrderStatus.ReadyForInvoicing)
        {
            throw new BusinessRuleException("Only invoice-ready orders can create draft invoices.");
        }

        var customer = await customersModule.GetCustomerValidationAsync(order.CustomerId, cancellationToken);
        return new OrderBillingSnapshotDto(order.Id, order.CustomerId, customer.Name, order.TotalAmount, order.InvoiceId, order.Status.ToString());
    }

    public async Task LinkInvoiceAsync(Guid orderId, Guid invoiceId, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new KeyNotFoundException("Order not found.");

        order.LinkInvoice(invoiceId);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkOrderInvoicedAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new KeyNotFoundException("Order not found.");

        order.MarkInvoiced();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<OrderSummaryDto> CompleteOrderAsync(Guid orderId, string actor, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new KeyNotFoundException("Order not found.");

        if (order.InvoiceId is null)
        {
            throw new BusinessRuleException("An invoice must exist before the order can be completed.");
        }

        var invoiceStatus = await billingModule.GetInvoiceStatusAsync(order.InvoiceId.Value, cancellationToken)
            ?? throw new BusinessRuleException("Invoice status could not be found for this order.");

        if (!invoiceStatus.IsPaid)
        {
            throw new BusinessRuleException("Only paid invoices can complete an order.");
        }

        if (order.ReservationId is Guid reservationId)
        {
            await inventoryModule.CommitReservationAsync(reservationId, actor, cancellationToken);
        }

        order.Complete();
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetOrderAsync(orderId, cancellationToken) ?? throw new InvalidOperationException("Order completion failed.");
    }

    public async Task<OrderSummaryDto> CancelOrderAsync(Guid orderId, string actor, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new KeyNotFoundException("Order not found.");

        var reservationId = order.ReservationId;
        order.Cancel();
        await dbContext.SaveChangesAsync(cancellationToken);

        if (reservationId is Guid existingReservationId)
        {
            await inventoryModule.ReleaseReservationAsync(existingReservationId, actor, cancellationToken);
        }

        return await GetOrderAsync(orderId, cancellationToken) ?? throw new InvalidOperationException("Order cancellation failed.");
    }

    private async Task<IReadOnlyList<OrderSummaryDto>> ProjectOrdersAsync(IReadOnlyList<Order> orders, CancellationToken cancellationToken)
    {
        var customerIds = orders.Select(order => order.CustomerId).Distinct().ToList();
        var productIds = orders.SelectMany(order => order.Lines.Select(line => line.ProductId)).Distinct().ToList();

        var customerMap = await dbContext.Customers
            .Where(x => customerIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var productMap = await dbContext.Products
            .Where(x => productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return orders.Select(order =>
        {
            var customer = customerMap[order.CustomerId];
            var lines = order.Lines.Select(line =>
            {
                var product = productMap[line.ProductId];
                return new OrderLineDto(line.ProductId, product.Sku, product.Name, line.Quantity, line.UnitPrice, line.LineTotal);
            }).ToArray();

            return new OrderSummaryDto(
                order.Id,
                order.CustomerId,
                customer.Name,
                customer.AccountCode,
                order.Status.ToString(),
                order.ReservationId,
                order.InvoiceId,
                order.TotalAmount,
                order.CreatedBy,
                order.CreatedAt,
                order.SubmittedAt,
                order.ReadyForInvoicingAt,
                order.CompletedAt,
                order.CancelledAt,
                lines);
        }).ToArray();
    }
}
