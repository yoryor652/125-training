using OrderHub.Core.Domain;

namespace OrderHub.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task GetAll_ReturnsAllProductsIncludingInactive()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001");
        TestSetup.AddProduct(db, sku: "SKU-A002", isActive: false);

        var products = await service.GetAllAsync();

        Assert.Equal(2, products.Count);
    }

    [Fact]
    public async Task GetActive_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001");
        TestSetup.AddProduct(db, sku: "SKU-A002", isActive: false);

        var products = await service.GetActiveAsync();

        Assert.All(products, p => Assert.True(p.IsActive));
        Assert.Single(products);
    }

    [Fact]
    public async Task GetLowStockAlerts_ReturnsOnlyProductsAtOrBelowThreshold()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var above = TestSetup.AddProduct(db, sku: "SKU-ABOVE", stock: 20);
        var atThreshold = TestSetup.AddProduct(db, sku: "SKU-AT", stock: 10);
        var below = TestSetup.AddProduct(db, sku: "SKU-BELOW", stock: 3);

        var result = await service.GetLowStockAlertsAsync(1, 20, threshold: 10);

        var skus = result.Items.Select(r => r.Product.Sku).ToList();
        Assert.Contains(atThreshold.Sku, skus);
        Assert.Contains(below.Sku, skus);
        Assert.DoesNotContain(above.Sku, skus);
    }

    [Fact]
    public async Task GetLowStockAlerts_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var inactiveLowStock = TestSetup.AddProduct(db, sku: "SKU-INACTIVE", stock: 1, isActive: false);

        var result = await service.GetLowStockAlertsAsync(1, 20, threshold: 10);

        Assert.DoesNotContain(result.Items, r => r.Product.Sku == inactiveLowStock.Sku);
    }

    [Fact]
    public async Task GetLowStockAlerts_UnitsSoldLast30Days_ExcludesCancelledAndOutsideWindow()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, stock: 5);

        TestSetup.AddOrder(db, customer.Id, product.Id, quantity: 3,
            status: OrderStatus.Confirmed, createdAt: DateTime.UtcNow.AddDays(-5));
        TestSetup.AddOrder(db, customer.Id, product.Id, quantity: 100,
            status: OrderStatus.Cancelled, createdAt: DateTime.UtcNow.AddDays(-5));
        TestSetup.AddOrder(db, customer.Id, product.Id, quantity: 50,
            status: OrderStatus.Confirmed, createdAt: DateTime.UtcNow.AddDays(-40));

        var result = await service.GetLowStockAlertsAsync(1, 20, threshold: 10);

        var row = result.Items.Single(r => r.Product.Id == product.Id);
        Assert.Equal(3, row.UnitsSoldLast30Days);
    }

    [Fact]
    public async Task GetLowStockAlerts_ProductWithNoOrders_ReturnsZeroUnitsSold()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var product = TestSetup.AddProduct(db, stock: 2);

        var result = await service.GetLowStockAlertsAsync(1, 20, threshold: 10);

        var row = result.Items.Single(r => r.Product.Id == product.Id);
        Assert.Equal(0, row.UnitsSoldLast30Days);
    }
}
