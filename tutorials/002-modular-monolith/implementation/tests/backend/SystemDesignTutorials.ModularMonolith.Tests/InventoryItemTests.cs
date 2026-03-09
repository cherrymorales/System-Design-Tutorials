using FluentAssertions;
using SystemDesignTutorials.ModularMonolith.Domain.Modules.Inventory;
using SystemDesignTutorials.ModularMonolith.Domain.Modules.Orders;
using SystemDesignTutorials.ModularMonolith.Domain.Modules.Billing;
using SystemDesignTutorials.ModularMonolith.Domain.Modules.Shared;

namespace SystemDesignTutorials.ModularMonolith.Tests;

public class InventoryItemTests
{
    [Fact]
    public void Stock_item_tracks_reserve_release_and_commit()
    {
        var stockItem = new StockItem(Guid.NewGuid(), Guid.NewGuid(), 20, 5);

        stockItem.Reserve(6);
        stockItem.QuantityReserved.Should().Be(6);
        stockItem.AvailableQuantity.Should().Be(14);

        stockItem.Release(2);
        stockItem.QuantityReserved.Should().Be(4);
        stockItem.AvailableQuantity.Should().Be(16);

        stockItem.Commit(4);
        stockItem.QuantityOnHand.Should().Be(16);
        stockItem.QuantityReserved.Should().Be(0);
    }

    [Fact]
    public void Order_cannot_complete_before_it_is_invoiced()
    {
        var order = new Order(Guid.NewGuid(), [new OrderLine(Guid.NewGuid(), 2, 99m)], "sales@modularmonolith.local");

        var action = () => order.Complete();

        action.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Invoice_cannot_be_paid_before_it_is_issued()
    {
        var invoice = new Invoice(Guid.NewGuid(), Guid.NewGuid(), "INV-001", 100m, "finance@modularmonolith.local");

        var action = () => invoice.MarkPaid("finance@modularmonolith.local");

        action.Should().Throw<BusinessRuleException>();
    }
}
