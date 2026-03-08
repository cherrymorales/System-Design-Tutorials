namespace SystemDesignTutorials.LayeredMonolith.Infrastructure.Seeding;

public static class ApplicationRoles
{
    public const string WarehouseOperator = nameof(WarehouseOperator);
    public const string InventoryPlanner = nameof(InventoryPlanner);
    public const string PurchasingOfficer = nameof(PurchasingOfficer);
    public const string OperationsManager = nameof(OperationsManager);

    public static readonly string[] All =
    [
        WarehouseOperator,
        InventoryPlanner,
        PurchasingOfficer,
        OperationsManager,
    ];
}
