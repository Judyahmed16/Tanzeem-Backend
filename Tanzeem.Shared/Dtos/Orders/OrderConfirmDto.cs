
using Tanzeem.Domain.Enums;

namespace Tanzeem.Shared.Dtos.Orders
{
    public class OrderConfirmDto
    {
        public int OrderId { get; set; }
        public DateTime? RecievedDate { get; set; }
        public IEnumerable<OrderItemsConfirmDto> ItemsConfirmDtos { get; set; } = new List<OrderItemsConfirmDto>();
        public string? Notes { get; set; }
    }
    public class OrderItemsConfirmDto
    {
        public int ProductId { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal? CostPrice { get; set; }
        public string? Notes { get; set; }
        public IEnumerable<ConfirmItemIssuesDto> ItemsIssueDtos { get; set; } = new List<ConfirmItemIssuesDto>();
    }

    public class ConfirmItemIssuesDto
    {
        public IssueType IssueType { get; set; }
        public int Quantity { get; set; }
    }



    public class OrderConfirmResponseDto
    {
        public int OrderId { get; set; }
        public string OrderStringId { get; set; }
        public int SupplierId { get; set; }
        public string SupplierStringId { get; set; }
        public string SupplierName { get; set; }
        public IEnumerable<OrderItemConfirmResponseDto> ItemsConfirmResponseDtos { get; set; } = new List<OrderItemConfirmResponseDto>();
    }

    public class OrderItemConfirmResponseDto
    {
    public int ProductId { get; set; }
    public int OrderedQuantity { get; set; }
    public string SKU { get; set; }
    public decimal Price { get; set; }
    }
}
