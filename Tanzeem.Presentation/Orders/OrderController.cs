using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tanzeem.Domain.Constants;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.Orders;
using Tanzeem.Shared.Dtos.Orders;

namespace Tanzeem.Presentation.Orders
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager)]
    public class OrderController(IOrderService _orderService) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateOrder(OrderRequestDto orderDto)
        {
            var result = await _orderService.CreateOrderAsync(orderDto);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        //[Authorize(Roles = "")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var result = await _orderService.DeleteOrderAsync(id);
            return Ok(result);
        }

        [HttpPut("{id}")]
        //[Authorize(Roles = "")]
        public async Task<IActionResult> UpdateOrderDetails(int id, OrderRequestDto orderRequestDto)
        {
            var result = await _orderService.UpdateOrderAsync(id, orderRequestDto);
            if (result == -1)
                return BadRequest("cant edit this order..you can just edit pending orders");
            else
                return Ok(result);
        }

        [HttpGet("{id}")]
        //[Authorize(Roles = "")]
        public async Task<IActionResult> ViewOrderDetails(int id)
        {
            var result = await _orderService.GetOrderByIdAsync(id);
            return Ok(result);
        }

        [HttpGet]
        //[Authorize(Roles = "")]
        public async Task<IActionResult> ViewOrdersWithPagination([FromQuery(Name = "page_size")] int pageSize=10, [FromQuery(Name = "page")] int page = 1,[FromQuery(Name ="filterId")] OrderFilter? orderFilter = null, [FromQuery(Name = "sortId")] OrderSort? orderSort = null, [FromQuery(Name = "searchTerm")] string? searchTerm = null)
        {
            var result = await _orderService.GetOrdersWithPaginationAsync(page,pageSize,orderFilter,orderSort,searchTerm);
            return Ok(result);
        }

        [HttpPut("ConfirmDelivery")]
        //[Authorize(Roles = "")]
        public async Task<IActionResult> ConfirmDelivery(OrderConfirmDto confirmDto)
        {
            var result = await _orderService.ChangeOrderToDeliverd(confirmDto);
            return Ok(result);
        }

        //[HttpGet("Lookup_Products")]
        //[Authorize(Roles = "")]
        //public async Task<IActionResult> GetProductsLookup(string term)
        //{
        //    var result = await _orderService.GetProductsLookupAsync(term);
        //    return Ok(result);
        //}

        //[HttpGet("display_order_statuses")]
        //[Authorize(Roles = "")]
        //public IActionResult DisplayOrderStatuses()
        //{
        //    var result = _orderService.DisplayOrderStatuses();
        //    return Ok(result);
        //}

        //[HttpGet("Pending_Order_Count")]
        ////[Authorize(Roles = "")]
        //public IActionResult CountPendingOrders()
        //{
        //    var result = _orderService.CountPendingOrders();
        //    return Ok(result);
        //}
        
        //[HttpGet("Delivered_Order_Count")]
        ////[Authorize(Roles = "")]
        //public IActionResult CountDeliverdOrders()
        //{
        //    var result = _orderService.CountDeliverdOrders();
        //    return Ok(result);
        //}

        [HttpGet("mini_order_dashboard")]
        public async Task<IActionResult> CountsDashboard()
        {
            var result = await _orderService.Counts();
            return Ok(result);
        }
        [HttpGet("View_Order_Confirm/{id}")]
        public async Task<IActionResult> ViewConfirmOrder(int id)
        {
            var result = await _orderService.ViewConfirm(id);
            return Ok(result);
        }
        
    }
}
