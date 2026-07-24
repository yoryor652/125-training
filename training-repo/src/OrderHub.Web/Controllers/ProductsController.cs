using Microsoft.AspNetCore.Mvc;
using OrderHub.Core.Services;
using OrderHub.Web.ViewModels;

namespace OrderHub.Web.Controllers;

public class ProductsController : Controller
{
    private const int PageSize = 20;

    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _productService.GetAllAsync();

        var vm = new ProductListViewModel
        {
            Products = products.Select(p => new ProductRowViewModel
            {
                Sku = p.Sku,
                Name = p.Name,
                UnitPrice = p.UnitPrice,
                StockQuantity = p.StockQuantity,
                IsActive = p.IsActive
            }).ToList()
        };

        return View(vm);
    }

    public async Task<IActionResult> LowStock(LowStockViewModel filter, int page = 1)
    {
        if (!ModelState.IsValid)
        {
            filter.Products = Array.Empty<LowStockRowViewModel>();
            return View(filter);
        }

        var result = await _productService.GetLowStockAlertsAsync(page, PageSize, filter.Threshold);

        filter.Page = result.Page;
        filter.TotalCount = result.TotalCount;
        filter.TotalPages = result.TotalPages;
        filter.Products = result.Items.Select(r => new LowStockRowViewModel
        {
            Sku = r.Product.Sku,
            Name = r.Product.Name,
            StockQuantity = r.Product.StockQuantity,
            UnitsSoldLast30Days = r.UnitsSoldLast30Days
        }).ToList();

        return View(filter);
    }
}

