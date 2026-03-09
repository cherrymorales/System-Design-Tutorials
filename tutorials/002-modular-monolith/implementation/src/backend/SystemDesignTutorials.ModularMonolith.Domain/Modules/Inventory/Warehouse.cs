using SystemDesignTutorials.ModularMonolith.Domain.Enums;
using SystemDesignTutorials.ModularMonolith.Domain.Modules.Shared;

namespace SystemDesignTutorials.ModularMonolith.Domain.Modules.Inventory;

public sealed class Warehouse
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public WarehouseStatus Status { get; private set; } = WarehouseStatus.Active;

    private Warehouse() { }

    public Warehouse(string code, string name, string city)
    {
        Code = Require(code, nameof(code), 32);
        Name = Require(name, nameof(name), 128);
        City = Require(city, nameof(city), 128);
    }

    private static string Require(string value, string field, int maxLength)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) throw new BusinessRuleException($"{field} is required.");
        if (trimmed.Length > maxLength) throw new BusinessRuleException($"{field} exceeds {maxLength} characters.");
        return trimmed;
    }
}
