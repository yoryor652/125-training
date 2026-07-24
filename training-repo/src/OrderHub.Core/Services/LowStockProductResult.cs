using OrderHub.Core.Domain;

namespace OrderHub.Core.Services;

public class LowStockProductResult
{
    public Product Product { get; init; } = null!;
    public int UnitsSoldLast30Days { get; init; }
}
