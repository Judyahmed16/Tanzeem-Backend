namespace Tanzeem.Shared.Dtos.Orders
{
    public class OrderResponseDto
    {
        public int Id { get; set; }
        public string StringId { get; set; }
        public int SupplierId { get; set; }
        public string StringSupplierId { get; set; }
        public string SupplierName { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal Total { get; set; } // calculated from order items
        public decimal SubTotal { get; set; }
        public string Status { get; set; } //enum
        public DateTime ExpectedDeliveryDate { get; set; }
        public DateTime? RecievedDeliveryDate { get; set; } // can be null until recieving order
        public string? Notes { get; set; }
        public decimal ShippingCost { get; set; } = 0;
        public decimal Taxes { get; set; } = 0;
        public List<OrderItemResponseDto> Items { get; set; } = new List<OrderItemResponseDto>();
    }
}
