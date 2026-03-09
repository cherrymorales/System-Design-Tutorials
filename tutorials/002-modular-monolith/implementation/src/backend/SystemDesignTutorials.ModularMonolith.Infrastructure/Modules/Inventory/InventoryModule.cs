using Microsoft.EntityFrameworkCore;
using SystemDesignTutorials.ModularMonolith.Application.Modules;
using SystemDesignTutorials.ModularMonolith.Domain.Modules.Inventory;
using SystemDesignTutorials.ModularMonolith.Infrastructure.Persistence;

namespace SystemDesignTutorials.ModularMonolith.Infrastructure.Modules.Inventory;

public sealed class InventoryModule(ModularMonolithDbContext dbContext) : IInventoryModule
{
    public async Task<IReadOnlyList<WarehouseDto>> GetWarehousesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Warehouses
            .OrderBy(x => x.Code)
            .Select(x => new WarehouseDto(x.Id, x.Code, x.Name, x.City, x.Status.ToString()))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockItemDto>> GetStockAsync(CancellationToken cancellationToken)
    {
        return await (
                from stock in dbContext.StockItems
                join product in dbContext.Products on stock.ProductId equals product.Id
                join warehouse in dbContext.Warehouses on stock.WarehouseId equals warehouse.Id
                orderby product.Name
                select new StockItemDto(
                    stock.Id,
                    stock.ProductId,
                    product.Sku,
                    product.Name,
                    stock.WarehouseId,
                    warehouse.Code,
                    warehouse.Name,
                    stock.QuantityOnHand,
                    stock.QuantityReserved,
                    stock.AvailableQuantity,
                    stock.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ReservationResultDto> ReserveOrderAsync(Guid orderId, IReadOnlyList<ReservationItemCommand> items, string actor, CancellationToken cancellationToken)
    {
        _ = actor;
        if (items.Count == 0)
        {
            throw new InvalidOperationException("Cannot create a reservation without any order lines.");
        }

        var warehouse = await dbContext.Warehouses.OrderBy(x => x.Code).FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No warehouse is configured for inventory reservations.");

        var productIds = items.Select(x => x.ProductId).Distinct().ToList();
        var stockItems = await dbContext.StockItems
            .Where(x => productIds.Contains(x.ProductId) && x.WarehouseId == warehouse.Id)
            .ToDictionaryAsync(x => x.ProductId, cancellationToken);

        foreach (var item in items)
        {
            if (!stockItems.TryGetValue(item.ProductId, out var stockItem))
            {
                throw new InvalidOperationException("Stock item is not configured for one of the requested products.");
            }

            stockItem.Reserve(item.Quantity);
        }

        var reservationLines = items.Select(item => new InventoryReservationLine(item.ProductId, warehouse.Id, item.Quantity)).ToArray();
        var reservation = new InventoryReservation(orderId, reservationLines);
        dbContext.InventoryReservations.Add(reservation);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new ReservationResultDto(reservation.Id, reservation.Status.ToString(), reservation.ReservedAt);
    }

    public async Task ReleaseReservationAsync(Guid reservationId, string actor, CancellationToken cancellationToken)
    {
        _ = actor;
        var reservation = await dbContext.InventoryReservations
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.Id == reservationId, cancellationToken)
            ?? throw new KeyNotFoundException("Reservation not found.");

        if (reservation.Status != Domain.Enums.ReservationStatus.Reserved)
        {
            return;
        }

        var lineGroups = reservation.Lines.GroupBy(x => x.ProductId).Select(group => new { ProductId = group.Key, Quantity = group.Sum(item => item.Quantity) }).ToArray();
        var productIds = lineGroups.Select(group => group.ProductId).ToList();
        var stockItems = await dbContext.StockItems.Where(x => productIds.Contains(x.ProductId)).ToDictionaryAsync(x => x.ProductId, cancellationToken);

        foreach (var line in lineGroups)
        {
            stockItems[line.ProductId].Release(line.Quantity);
        }

        reservation.Release();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CommitReservationAsync(Guid reservationId, string actor, CancellationToken cancellationToken)
    {
        _ = actor;
        var reservation = await dbContext.InventoryReservations
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.Id == reservationId, cancellationToken)
            ?? throw new KeyNotFoundException("Reservation not found.");

        var lineGroups = reservation.Lines.GroupBy(x => x.ProductId).Select(group => new { ProductId = group.Key, Quantity = group.Sum(item => item.Quantity) }).ToArray();
        var productIds = lineGroups.Select(group => group.ProductId).ToList();
        var stockItems = await dbContext.StockItems.Where(x => productIds.Contains(x.ProductId)).ToDictionaryAsync(x => x.ProductId, cancellationToken);

        foreach (var line in lineGroups)
        {
            stockItems[line.ProductId].Commit(line.Quantity);
        }

        reservation.Commit();
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
