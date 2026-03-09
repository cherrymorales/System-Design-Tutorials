using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.ModularMonolith.Application.Modules;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Persistence;

namespace SystemDesignTutorials.ModularMonolith.Infrastructure.Modules.Reporting;

public sealed class ReportingModule(ModularMonolithDbContext dbContext) : IReportingModule
{
    public async Task<ReportSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var totalCustomers = await dbContext.Customers.CountAsync(cancellationToken);
        var activeProducts = await dbContext.Products.CountAsync(x => x.Status == Domain.Enums.ProductStatus.Active, cancellationToken);
        var draftOrders = await dbContext.Orders.CountAsync(x => x.Status == Domain.Enums.OrderStatus.Draft, cancellationToken);
        var reservedOrders = await dbContext.Orders.CountAsync(x => x.Status == Domain.Enums.OrderStatus.Reserved, cancellationToken);
        var readyOrders = await dbContext.Orders.CountAsync(x => x.Status == Domain.Enums.OrderStatus.ReadyForInvoicing, cancellationToken);
        var issuedInvoices = await dbContext.Invoices.CountAsync(x => x.Status == Domain.Enums.InvoiceStatus.Issued, cancellationToken);
        var paidInvoices = await dbContext.Invoices.CountAsync(x => x.Status == Domain.Enums.InvoiceStatus.Paid, cancellationToken);
        var totalPaidValue = await dbContext.Invoices.Where(x => x.Status == Domain.Enums.InvoiceStatus.Paid).SumAsync(x => (decimal?)x.TotalAmount, cancellationToken) ?? 0m;

        var stockItems = await (
                from stock in dbContext.StockItems
                join product in dbContext.Products on stock.ProductId equals product.Id
                join warehouse in dbContext.Warehouses on stock.WarehouseId equals warehouse.Id
                select new LowStockDto(stock.Id, stock.ProductId, product.Sku, product.Name, stock.WarehouseId, warehouse.Code, warehouse.Name, stock.QuantityOnHand, stock.QuantityReserved, stock.AvailableQuantity, stock.ReorderThreshold))
            .ToListAsync(cancellationToken);

        var totalReservedValue = await (
                from reservation in dbContext.InventoryReservationLines
                join product in dbContext.Products on reservation.ProductId equals product.Id
                select (decimal?)(reservation.Quantity * product.UnitPrice))
            .SumAsync(cancellationToken) ?? 0m;

        var moduleHealth = new[]
        {
            new ModuleHealthDto("Customers", $"{totalCustomers} customer records", "Healthy"),
            new ModuleHealthDto("Catalog", $"{activeProducts} active products", "Healthy"),
            new ModuleHealthDto("Orders", $"{draftOrders + reservedOrders + readyOrders} open order workflows", "Healthy"),
            new ModuleHealthDto("Inventory", $"{stockItems.Count} tracked stock positions", "Healthy"),
            new ModuleHealthDto("Billing", $"{issuedInvoices + paidInvoices} invoice records", "Healthy"),
            new ModuleHealthDto("Reporting", "Read models queried in-process", "Healthy"),
        };

        return new ReportSummaryDto(
            totalCustomers,
            activeProducts,
            draftOrders,
            reservedOrders,
            readyOrders,
            issuedInvoices,
            paidInvoices,
            totalReservedValue,
            totalPaidValue,
            stockItems.Where(x => x.AvailableQuantity <= x.ReorderThreshold).OrderBy(x => x.ProductName).ToArray(),
            moduleHealth);
    }
}
