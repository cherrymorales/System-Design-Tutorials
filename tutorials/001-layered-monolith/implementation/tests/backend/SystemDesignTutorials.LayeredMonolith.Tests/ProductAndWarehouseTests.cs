using FluentAssertions;
using SystemDesignTutorials.LayeredMonolith.Domain.Entities;
using SystemDesignTutorials.LayeredMonolith.Domain.Enums;

namespace SystemDesignTutorials.LayeredMonolith.Tests;

public sealed class ProductAndWarehouseTests
{
    [Fact]
    public void Product_update_details_replaces_catalog_fields()
    {
        var product = new Product("LAP-14-BLK", "14 Inch Laptop", "Computers", "SUP-TECH", 1299.00m);

        product.UpdateDetails("14 Inch Laptop Pro", "Premium Computers", "SUP-TECH-2", 1499.00m);

        product.Name.Should().Be("14 Inch Laptop Pro");
        product.Category.Should().Be("Premium Computers");
        product.SupplierCode.Should().Be("SUP-TECH-2");
        product.UnitCost.Should().Be(1499.00m);
    }

    [Fact]
    public void Product_archive_marks_product_as_archived()
    {
        var product = new Product("MON-27-4K", "27 Inch 4K Monitor", "Displays", "SUP-DISPLAY", 499.00m);

        product.Archive();

        product.Status.Should().Be(ProductStatus.Archived);
    }

    [Fact]
    public void Warehouse_update_details_replaces_name_and_city()
    {
        var warehouse = new Warehouse("BNE", "Brisbane Warehouse", "Brisbane");

        warehouse.UpdateDetails("Brisbane Central Warehouse", "Brisbane North");

        warehouse.Name.Should().Be("Brisbane Central Warehouse");
        warehouse.City.Should().Be("Brisbane North");
    }

    [Fact]
    public void Warehouse_deactivate_marks_site_as_inactive()
    {
        var warehouse = new Warehouse("SYD", "Sydney Warehouse", "Sydney");

        warehouse.Deactivate();

        warehouse.Status.Should().Be(WarehouseStatus.Inactive);
    }
}

