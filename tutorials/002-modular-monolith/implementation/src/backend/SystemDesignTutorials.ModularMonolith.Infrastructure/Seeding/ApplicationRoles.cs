namespace SystemDesignTutorials.ModularMonolith.Infrastructure.Seeding;

public static class ApplicationRoles
{
    public const string SalesCoordinator = "SalesCoordinator";
    public const string WarehouseOperator = "WarehouseOperator";
    public const string FinanceOfficer = "FinanceOfficer";
    public const string OperationsManager = "OperationsManager";

    public static readonly string[] All =
    [
        SalesCoordinator,
        WarehouseOperator,
        FinanceOfficer,
        OperationsManager,
    ];
}
