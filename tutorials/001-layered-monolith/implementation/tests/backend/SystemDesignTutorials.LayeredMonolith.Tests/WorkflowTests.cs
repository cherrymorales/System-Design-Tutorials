using FluentAssertions;
using SystemDesignTutorials.LayeredMonolith.Domain.Entities;
using SystemDesignTutorials.LayeredMonolith.Domain.Enums;

namespace SystemDesignTutorials.LayeredMonolith.Tests;

public sealed class WorkflowTests
{
    [Fact]
    public void Transfer_approve_dispatch_receive_advances_state_machine()
    {
        var transfer = new StockTransfer(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 5, "planner@layeredmonolith.local", "Replenish stock");

        transfer.Approve("manager@layeredmonolith.local");
        transfer.Dispatch("operator.brisbane@layeredmonolith.local");
        transfer.Receive("operator.brisbane@layeredmonolith.local");

        transfer.Status.Should().Be(TransferStatus.Received);
        transfer.ApprovedBy.Should().Be("manager@layeredmonolith.local");
        transfer.DispatchedBy.Should().Be("operator.brisbane@layeredmonolith.local");
        transfer.ReceivedBy.Should().Be("operator.brisbane@layeredmonolith.local");
    }

    [Fact]
    public void Transfer_cancel_before_dispatch_sets_cancelled_state()
    {
        var transfer = new StockTransfer(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, "planner@layeredmonolith.local", "Rebalance stock");

        transfer.Cancel("manager@layeredmonolith.local", "No longer needed");

        transfer.Status.Should().Be(TransferStatus.Cancelled);
        transfer.CancellationReason.Should().Be("No longer needed");
    }

    [Fact]
    public void Adjustment_submit_with_small_value_auto_approves()
    {
        var adjustment = new InventoryAdjustment(Guid.NewGuid(), Guid.NewGuid(), -2, "Damaged", "operator.brisbane@layeredmonolith.local", "Cycle count variance");

        adjustment.Submit(100m);

        adjustment.Status.Should().Be(AdjustmentStatus.Approved);
        adjustment.RequiresApproval.Should().BeFalse();
        adjustment.ApprovedBy.Should().Be("operator.brisbane@layeredmonolith.local");
    }

    [Fact]
    public void Adjustment_submit_with_large_value_enters_pending_approval()
    {
        var adjustment = new InventoryAdjustment(Guid.NewGuid(), Guid.NewGuid(), -20, "Damaged", "operator.brisbane@layeredmonolith.local", "Rack collapse");

        adjustment.Submit(150m);

        adjustment.Status.Should().Be(AdjustmentStatus.PendingApproval);
        adjustment.RequiresApproval.Should().BeTrue();
    }
}
