namespace SystemDesignTutorials.LayeredMonolith.Web.Contracts;

public sealed record ApproveInventoryAdjustmentRequest(string ApprovedBy, string? Notes);
