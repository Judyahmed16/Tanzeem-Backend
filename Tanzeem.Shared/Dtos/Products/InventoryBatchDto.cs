namespace Tanzeem.Shared.Dtos.Products;

public class InventoryBatchDto
{
    public int Id { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal CostPrice { get; set; }
    public DateTime ReceivedAt { get; set; }
}
