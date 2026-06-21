using Tanzeem.Domain.AuditLogs;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Products;

namespace Tanzeem.Domain.Entities.Inventories;

public class InventoryBatch : IAuditable
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int BranchId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal CostPrice { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    public Product Product { get; set; } = default!;
    public Branch Branch { get; set; } = default!;
}
