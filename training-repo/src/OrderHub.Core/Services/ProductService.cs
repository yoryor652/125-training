using OrderHub.Core.Common;
using OrderHub.Core.Domain;
using OrderHub.Core.Interfaces;

namespace OrderHub.Core.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;

    public ProductService(IProductRepository productRepository, IOrderRepository orderRepository)
    {
        _productRepository = productRepository;
        _orderRepository = orderRepository;
    }

    public Task<IReadOnlyList<Product>> GetAllAsync() => _productRepository.GetAllAsync();

    public Task<IReadOnlyList<Product>> GetActiveAsync() => _productRepository.GetActiveAsync();

    public async Task<PagedResult<LowStockProductResult>> GetLowStockAlertsAsync(int page, int pageSize, int threshold)
    {
        var paged = await _productRepository.GetLowStockAsync(page, pageSize, threshold);
        var since = DateTime.UtcNow.AddDays(-30);
        var soldMap = await _orderRepository.GetUnitsSoldSinceAsync(since, paged.Items.Select(p => p.Id));

        var results = paged.Items
            .Select(p => new LowStockProductResult
            {
                Product = p,
                UnitsSoldLast30Days = soldMap.GetValueOrDefault(p.Id, 0)
            })
            .ToList();

        return new PagedResult<LowStockProductResult>
        {
            Items = results,
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize
        };
    }
}
