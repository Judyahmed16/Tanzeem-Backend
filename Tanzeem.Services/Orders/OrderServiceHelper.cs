using Tanzeem.Domain.Entities.Orders;

namespace Tanzeem.Services.Orders
{
    public class OrderServiceHelper
    {
        public static decimal calculateTotalOfOrder(IEnumerable<OrderItem> items, decimal Taxes, decimal Shipping)
        {
            var totalOfEveryOrderItem = items.Select(item => item.Total);
            var itemsTotal = items?.Sum(item => item.Total) ?? 0;
            return itemsTotal + Taxes + Shipping;
        }
    }
}
