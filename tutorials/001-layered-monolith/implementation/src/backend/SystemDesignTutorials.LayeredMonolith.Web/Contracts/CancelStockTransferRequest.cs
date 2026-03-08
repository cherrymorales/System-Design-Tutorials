namespace SystemDesignTutorials.LayeredMonolith.Web.Contracts;

public sealed record CancelStockTransferRequest(string CancelledBy, string? CancellationReason);
