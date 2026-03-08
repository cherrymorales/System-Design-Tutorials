using SystemDesignTutorials.LayeredMonolith.Domain.Enums;

namespace SystemDesignTutorials.LayeredMonolith.Domain.Entities;

public sealed class Warehouse
{
    private Warehouse()
    {
    }

    public Warehouse(string code, string name, string city)
    {
        Id = Guid.NewGuid();
        Code = code;
        Name = name;
        City = city;
        Status = WarehouseStatus.Active;
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public WarehouseStatus Status { get; private set; }

    public void Deactivate()
    {
        Status = WarehouseStatus.Inactive;
    }
}
