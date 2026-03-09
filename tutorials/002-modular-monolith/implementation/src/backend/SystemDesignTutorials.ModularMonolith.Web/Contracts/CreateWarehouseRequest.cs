namespace SystemDesignTutorials.ModularMonolith.Web.Contracts;

public sealed record CreateWarehouseRequest(
    string Code,
    string Name,
    string City);

