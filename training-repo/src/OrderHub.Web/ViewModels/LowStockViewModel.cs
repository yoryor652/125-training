using System.ComponentModel.DataAnnotations;

namespace OrderHub.Web.ViewModels;

public class LowStockViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "庫存門檻必須大於 0")]
    [Display(Name = "庫存門檻")]
    public int Threshold { get; set; } = 10;

    public int Page { get; set; } = 1;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public IReadOnlyList<LowStockRowViewModel> Products { get; set; } = Array.Empty<LowStockRowViewModel>();
}

public class LowStockRowViewModel
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int UnitsSoldLast30Days { get; set; }
}
