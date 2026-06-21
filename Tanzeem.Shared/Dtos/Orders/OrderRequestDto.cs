using Tanzeem.Domain.Enums;

namespace Tanzeem.Shared.Dtos.Orders
{
    public class OrderRequestDto
    {
        public int SupplierId { get; set; }
        public DateTime OrderDate { get; set; }
        //public OrderStatus Status { get; set; } //enum
        public DateTime ExpectedDeliveryDate { get; set; }
        public DateTime? RecievedDeliveryDate { get; set; } // can be null until recieving order
        public string? Notes { get; set; }
        public decimal ShippingCost { get; set; } = 0;
        public decimal Taxes { get; set; } = 0;
        public List<OrderItemRequestDto> Items { get; set; } = new List<OrderItemRequestDto>();
    }
}
