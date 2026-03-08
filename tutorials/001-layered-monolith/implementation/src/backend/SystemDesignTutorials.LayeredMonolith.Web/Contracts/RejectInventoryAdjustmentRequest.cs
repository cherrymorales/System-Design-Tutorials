namespace SystemDesignTutorials.LayeredMonolith.Web.Contracts;

public sealed record RejectInventoryAdjustmentRequest(string RejectedBy, string? Notes);
