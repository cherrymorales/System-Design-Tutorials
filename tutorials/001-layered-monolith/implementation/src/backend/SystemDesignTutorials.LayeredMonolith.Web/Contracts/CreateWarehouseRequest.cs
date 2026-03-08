namespace SystemDesignTutorials.LayeredMonolith.Web.Contracts;

public sealed record CreateWarehouseRequest(
    string Code,
    string Name,
    string City);
