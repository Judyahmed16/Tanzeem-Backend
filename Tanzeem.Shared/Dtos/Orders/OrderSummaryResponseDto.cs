namespace Tanzeem.Shared.Dtos.Orders
{
    public class OrderSummaryResponseDto
    {
        public int Id { get; set; }
        public string StringId { get; set; }
        public DateTime OrderDate { get; set; }
        public int SupplierId { get; set; }
        public string SupplierName { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; }
    }
}
