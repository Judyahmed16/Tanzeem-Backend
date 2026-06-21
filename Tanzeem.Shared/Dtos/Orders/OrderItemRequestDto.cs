namespace Tanzeem.Shared.Dtos.Orders
{
    public class OrderItemRequestDto
    {
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public int ProductId { get; set; }
    }
}
