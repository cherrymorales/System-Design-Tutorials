using FluentAssertions;
using SystemDesignTutorials.LayeredMonolith.Domain.Entities;

namespace SystemDesignTutorials.LayeredMonolith.Tests;

public sealed class InventoryItemTests
{
    [Fact]
    public void Reserve_ShouldIncreaseReservedQuantity_WithoutChangingOnHand()
    {
        var inventoryItem = new InventoryItem(Guid.NewGuid(), Guid.NewGuid(), reorderThreshold: 10);
        inventoryItem.Receive(25);

        inventoryItem.Reserve(5);

        inventoryItem.QuantityOnHand.Should().Be(25);
        inventoryItem.QuantityReserved.Should().Be(5);
        inventoryItem.AvailableQuantity.Should().Be(20);
    }

    [Fact]
    public void DispatchReserved_ShouldDecreaseOnHandAndReservation()
    {
        var inventoryItem = new InventoryItem(Guid.NewGuid(), Guid.NewGuid(), reorderThreshold: 10);
        inventoryItem.Receive(25);
        inventoryItem.Reserve(5);

        inventoryItem.DispatchReserved(5);

        inventoryItem.QuantityOnHand.Should().Be(20);
        inventoryItem.QuantityReserved.Should().Be(0);
        inventoryItem.AvailableQuantity.Should().Be(20);
    }
}
