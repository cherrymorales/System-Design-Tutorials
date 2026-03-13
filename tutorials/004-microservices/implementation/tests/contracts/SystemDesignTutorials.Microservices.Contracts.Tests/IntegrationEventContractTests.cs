using System.Text.Json;
using FluentAssertions;
using SystemDesignTutorials.Microservices.Contracts;

namespace SystemDesignTutorials.Microservices.Contracts.Tests;

public sealed class IntegrationEventContractTests
{
    [Fact]
    public void Order_submitted_contract_round_trips_with_expected_fields()
    {
        var message = new OrderSubmittedIntegrationEvent(
            Guid.NewGuid(),
            "ORD-20005",
            "CSR-20005",
            "AUD",
            338m,
            "orders@microservices.local",
            [new SubmittedOrderLine("SKU-MOUSE-002", "Wireless Precision Mouse", 2, 89m)],
            DateTimeOffset.UtcNow);

        var json = JsonSerializer.Serialize(message);
        var roundTripped = JsonSerializer.Deserialize<OrderSubmittedIntegrationEvent>(json);

        roundTripped.Should().NotBeNull();
        roundTripped!.OrderNumber.Should().Be("ORD-20005");
        roundTripped.Lines.Should().ContainSingle();
        roundTripped.Lines.Single().Sku.Should().Be("SKU-MOUSE-002");
    }

    [Fact]
    public void Shipment_status_change_contract_round_trips()
    {
        var message = new ShipmentStatusChangedIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), ShipmentStatus.Shipped, DateTimeOffset.UtcNow);

        var json = JsonSerializer.Serialize(message);
        var roundTripped = JsonSerializer.Deserialize<ShipmentStatusChangedIntegrationEvent>(json);

        roundTripped.Should().NotBeNull();
        roundTripped!.Status.Should().Be(ShipmentStatus.Shipped);
    }
}
