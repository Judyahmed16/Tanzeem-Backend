using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.CustomExceptions;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.DeliveryIssues;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Orders;
using Tanzeem.Domain.Entities.Products;
using Tanzeem.Domain.Entities.Suppliers;
using Tanzeem.Domain.Enums;
using Tanzeem.Domain.Exceptions;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Services.Abstractions.DeliveryIssues;
using Tanzeem.Services.Abstractions.Notifications;
using Tanzeem.Services.Abstractions.Orders;
using Tanzeem.Services.Abstractions.Transactions;
using Tanzeem.Shared.Dtos;
using Tanzeem.Shared.Dtos.Orders;
using Tanzeem.Shared.Dtos.Products;

namespace Tanzeem.Services.Orders
{

    public class OrderService(IUnitOfWork _unitOfWork, INotificationService _notificationService
        ,ITransactionService _transactionService, IDeliveryIssuesService _deliveryIssue,
        ICurrentService _currentService) : IOrderService
    {
        public async Task<int> CreateOrderAsync(OrderRequestDto orderDto)
        {
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 
            //int branchId = 2;

            #region validations dto
            if (orderDto == null || orderDto.Items == null || !orderDto.Items.Any())
                throw new ValidationException("Order data is missing or order has no items.");

            if (orderDto.Items.Any(i => i.Price < 0 || i.Quantity <= 0))
                throw new ValidationException("Price and Quantity must be greater than zero.");
            
            if (orderDto.ExpectedDeliveryDate < orderDto.OrderDate)
                throw new BusinessRuleException("Expected delivery date cannot be before the order date.");

            var duplicateProducts = orderDto.Items
            .GroupBy(i => i.ProductId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

            if (duplicateProducts.Any())
            {
                throw new ValidationException($"You cannot send the same product multiple times. Please merge quantities for Product IDs: {string.Join(", ", duplicateProducts)}");
            }

            #endregion 
            var supplier = await _unitOfWork.GetRepository<Supplier>().GetByIdAsync(orderDto.SupplierId);
            if (supplier == null || supplier.BranchId != branchId)
                throw new KeyNotFoundException("This supplier id not found");
            
            var productIds = orderDto.Items.Select(i => i.ProductId).Distinct().ToList();

            var existingProducts = await _unitOfWork.GetRepository<Product>()
                    .GetAllAsIQueryable()
                    .Where(product => productIds.Contains(product.Id))
                    .ToListAsync();

            if (existingProducts.Count != productIds.Count)
                throw new BusinessRuleException("One or more products may be deleted from system!");

            var lastOrder = await _unitOfWork.GetRepository<Order>().GetAllAsIQueryable()
                .Where(o => o.BranchId == branchId)
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (lastOrder != null && !string.IsNullOrWhiteSpace(lastOrder.OrderNumber))
            {
                string[] numberParts = lastOrder.OrderNumber.Split('-');
                if (numberParts.Length > 0 && int.TryParse(numberParts.Last(), out int lastSeq))
                {
                    nextNumber = lastSeq + 1;
                }
            }
            string generatedOrderNumber = $"ORD-{nextNumber:D4}";
            #region mapping
            var OrderItems = orderDto.Items.Select(item => new OrderItem
            {
                ProductId = item.ProductId,
                Price = item.Price,
                Quantity = item.Quantity,
                Total = item.Price * item.Quantity,
            }).ToList();           

            Order order = new Order
            {
                OrderDate = orderDto.OrderDate,
                Total = OrderServiceHelper.calculateTotalOfOrder(OrderItems, orderDto.Taxes, orderDto.ShippingCost),
                SupplierId = orderDto.SupplierId,
                Taxes = orderDto.Taxes,
                ShippingCost = orderDto.ShippingCost,
                ExpectedDeliveryDate = orderDto.ExpectedDeliveryDate,
                Notes = orderDto.Notes,
                SupplierName = supplier.FullName,
                Items = OrderItems,
                Status = OrderStatus.Pending,
                BranchId = branchId,
                OrderNumber = generatedOrderNumber,
            };
            #endregion

            await _unitOfWork.GetRepository<Order>().AddAsync(order);

            int affectedRows = await _unitOfWork.SaveChangesAsync();

            if(affectedRows <= 0)
            {
                throw new DbUpdateFailedException("There is a problem happend when creating order at our database, please try again later");
            }

            await _notificationService.CreateNewOrderNotification(order);

            return order.Id;
        }

        //public async Task<bool> DeleteOrderAsync(int id)
        //{
        //    //int branchId = 2;
        //    int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 

        //    var orderToDelete = await _unitOfWork.GetRepository<Order>().GetByIdAsync(id);
        //    var deliveryissue = await _unitOfWork.GetRepository<DeliveryIssue>().GetAllAsIQueryable()
        //        .Where(x => x.OrderId == id).FirstOrDefaultAsync();

        //    if (orderToDelete is null || orderToDelete.BranchId != branchId)
        //        throw new KeyNotFoundException("No order with this id");

        //    if (orderToDelete.Status == OrderStatus.Deliverd)
        //    {
        //        throw new BusinessRuleException("You cannot delete an order that is already processed or completed. Only pending or cancelled orders can be deleted.");
        //    }

        //    _unitOfWork.GetRepository<Order>().DeleteAsync(orderToDelete);

        //    int affectedRows = await _unitOfWork.SaveChangesAsync();

        //    if (affectedRows <= 0)
        //        throw new DbUpdateFailedException("No rows affected");
        //    return true;
        //}
        public async Task<bool> DeleteOrderAsync(int id)
        {
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned");

            var orderToDelete = await _unitOfWork.GetRepository<Order>().GetByIdAsync(id);

            if (orderToDelete is null || orderToDelete.BranchId != branchId)
                throw new KeyNotFoundException("No order found with this id in your branch");

            if (orderToDelete.Status == OrderStatus.Deliverd)
            {
                throw new BusinessRuleException("You cannot delete an order that is already processed or completed. Only pending or cancelled orders can be deleted.");
            }

            var deliveryIssue = await _unitOfWork.GetRepository<DeliveryIssue>().GetAllAsIQueryable()
                .Where(x => x.OrderId == id).FirstOrDefaultAsync();

            if (deliveryIssue != null)
            {
                var deliveryIssueItems = await _unitOfWork.GetRepository<DeliveryIssueItem>().GetAllAsIQueryable()
                    .Where(x => x.DeliveryIssueId == deliveryIssue.Id).ToListAsync();

                foreach (var issueItem in deliveryIssueItems)
                {
                    _unitOfWork.GetRepository<DeliveryIssueItem>().DeleteAsync(issueItem);
                }
                _unitOfWork.GetRepository<DeliveryIssue>().DeleteAsync(deliveryIssue);
            }

            var orderItems = await _unitOfWork.GetRepository<OrderItem>().GetAllAsIQueryable()
                .Where(x => x.OrderId == id).ToListAsync();

            foreach (var item in orderItems)
            {
                _unitOfWork.GetRepository<OrderItem>().DeleteAsync(item);
            }
            _unitOfWork.GetRepository<Order>().DeleteAsync(orderToDelete);

            int affectedRows = await _unitOfWork.SaveChangesAsync();

            if (affectedRows <= 0)
                throw new DbUpdateFailedException("Failed to delete the order and its related data");

            return true;
        }

        public async Task<OrderResponseDto> GetOrderByIdAsync(int id)
        {
            //int branchId = 2;
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 
            
            var query = _unitOfWork.GetRepository<Order>().GetByIdAsQueryable(id);
            
            var orderQuery = query.Include(c => c.Supplier).Include(o => o.Items).ThenInclude(p => p.Product);

            var order = await orderQuery.FirstOrDefaultAsync();

            if (order is null || order.BranchId != branchId)
            {
                throw new KeyNotFoundException($"This order #ORD-{id:D4} not found");
                
            }

            var orderItems = order.Items.Select(item => new OrderItemResponseDto
            {
                ProductName = item.Product?.Name ?? "N/A",
                ProductId = item.Product?.Id ?? 0,
                SKU = item.Product?.SKU ?? "",
                Price = item.Price,
                Quantity = item.Quantity,
                Total = item.Total,
            }).ToList();

            #region mapping
            OrderResponseDto orderDto = new OrderResponseDto
            {
                Id = order.Id,
                StringId = order.OrderNumber,
                OrderDate = order.OrderDate,
                ExpectedDeliveryDate = order.ExpectedDeliveryDate,
                RecievedDeliveryDate = order.RecievedDeliveryDate ?? null,
                SupplierId = order.SupplierId ?? 0,
                StringSupplierId = order.Supplier!.SupplierNumber ?? "Deleted Supplier",
                SupplierName = order.SupplierName,
                Total = order.Total,
                Taxes = order.Taxes,
                ShippingCost = order.ShippingCost,
                Status = order.Status.ToString(),
                Items = orderItems,
                SubTotal = orderItems.Sum(i => i.Total),
                Notes = order.Notes,
            };
            #endregion
            return orderDto;
        }

        public async Task<int> UpdateOrderAsync(int id, OrderRequestDto orderDto)
        {
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 
            //int branchId = 2;

            #region validations dto
            if (orderDto == null || orderDto.Items == null || !orderDto.Items.Any())
                throw new ValidationException("Order data is missing or order has no items.");

            if (orderDto.Items.Any(i => i.Price < 0 || i.Quantity <= 0))
                throw new ValidationException("Price and Quantity must be greater than zero.");

            if (orderDto.ExpectedDeliveryDate < orderDto.OrderDate)
                throw new BusinessRuleException("Expected delivery date cannot be before the order date.");

            var productIds = orderDto.Items.Select(i => i.ProductId).Distinct().ToList();
            var existingProductsCount = await _unitOfWork.GetRepository<Product>()
                .GetAllAsIQueryable()
                .CountAsync(p => productIds.Contains(p.Id));

            if (existingProductsCount != productIds.Count)
                throw new BusinessRuleException("One or more products in the update request do not exist in the system.");

            var duplicateProducts = orderDto.Items
            .GroupBy(i => i.ProductId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

            if (duplicateProducts.Any())
            {
                throw new ValidationException($"You cannot send the same product multiple times. Please merge quantities for Product IDs: {string.Join(", ", duplicateProducts)}");
            }

            #endregion 

            var order = await _unitOfWork.GetRepository<Order>().GetByIdAsQueryable(id)
                .Include(i => i.Items).FirstOrDefaultAsync();

            #region validation order
            if (order == null || order.BranchId != branchId)
            {
                throw new KeyNotFoundException($"This order ORD-{id:D4} not found");
            }

            if (order.Status != OrderStatus.Pending)
            {
                throw new BusinessRuleException("you cannot update deliverd or cancelled orders");
            }
            #endregion

            #region mapping
            order.OrderDate = orderDto.OrderDate;
            order.ExpectedDeliveryDate = orderDto.ExpectedDeliveryDate;
            order.RecievedDeliveryDate = orderDto.RecievedDeliveryDate;
            order.ShippingCost = orderDto.ShippingCost;
            order.Taxes = orderDto.Taxes;
            //order.Status = orderDto.Status;
            order.Notes = orderDto.Notes;


            #region items mapping
            order.Items.Clear();

            var newItems = orderDto.Items.Select(item => new OrderItem
            {
                ProductId = item.ProductId,
                Price = item.Price,
                Quantity = item.Quantity,
                Total = item.Price * item.Quantity,
            }).ToList();

            foreach (var item in newItems)
            {
                order.Items.Add(item);
            }
            #endregion
            order.Total = OrderServiceHelper.calculateTotalOfOrder(newItems, orderDto.Taxes, orderDto.ShippingCost);
            #endregion

            _unitOfWork.GetRepository<Order>().UpdateAsync(order);

            int rowsAffected = await _unitOfWork.SaveChangesAsync();

            return order.Id;
        }

        public async Task<PaginationResponseDto<OrderSummaryResponseDto>> GetOrdersWithPaginationAsync(
            int page, int pageSize, OrderFilter? orderFilter = null,OrderSort? orderSort=null, string? searchTerm = null)
        {
            //int branchId = 2;
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 
            
            if (page <= 0) page = 1;

            const int maxPageSize = 20;
            
            if (pageSize > maxPageSize) pageSize = maxPageSize;

            var query = _unitOfWork.GetRepository<Order>().GetAllAsIQueryable().Where(order => order.BranchId == branchId);

            if (orderFilter.HasValue)
            {
                switch (orderFilter.Value)
                {
                    case OrderFilter.PendingOrders:
                        query = query.Where(order => order.Status == OrderStatus.Pending);
                        break;
                    case OrderFilter.DeliveredOrders:
                        query = query.Where(order => order.Status == OrderStatus.Deliverd);
                        break;
                }
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var cleanSearch = searchTerm.Trim().ToLower();

                var searchNumberText = cleanSearch.Replace("ord-", "");

                bool isNumber = int.TryParse(searchNumberText, out int searchNumber);
                bool isDate = DateTime.TryParse(searchTerm, out DateTime searchDate);

                query = query.Where(order =>

                    order.SupplierName.ToLower().Contains(cleanSearch) ||
                    order.OrderNumber.Contains(cleanSearch) ||
                    (order.Notes != null && order.Notes.ToLower().Contains(cleanSearch)) ||

                    (isNumber && (order.Id == searchNumber || order.Total == searchNumber)) ||

                    (isDate && (order.OrderDate.Date == searchDate.Date ||
                                (order.RecievedDeliveryDate != null && order.RecievedDeliveryDate.Value.Date == searchDate.Date) ||
                                order.ExpectedDeliveryDate.Date == searchDate.Date))
                );
            }

            var totalCount = await query.CountAsync();

            if (orderSort.HasValue)
            {
                switch(orderSort.Value)
                {
                    case OrderSort.AZSupplierName:
                        query = query.OrderBy(query => query.SupplierName);
                        break;
                    case OrderSort.ZASupplierName:
                        query = query.OrderByDescending(query => query.SupplierName);
                        break;
                    case OrderSort.NearRecieveDate:
                        query = query.OrderByDescending(query => query.RecievedDeliveryDate);
                        break;
                    case OrderSort.FarRecieveDate:
                        query = query.OrderBy(query => query.RecievedDeliveryDate);
                        break;
                    case OrderSort.NearOrderDate:
                        query = query.OrderByDescending(query => query.OrderDate);
                        break;
                    case OrderSort.FarOrderDate:
                        query = query.OrderBy(query => query.OrderDate);
                        break;
                    case OrderSort.HighTotal:
                        query = query.OrderByDescending(query => query.Total);
                        break;
                    case OrderSort.LowTotal:
                        query = query.OrderBy(query => query.Total);
                        break;

                }
            }
            else
            {
                query = query.OrderByDescending(o => o.OrderDate);
            }

      
            var orders = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize).ToListAsync();

            var mappedData = orders.Select(order => new OrderSummaryResponseDto
            {
                Id = order.Id,
                StringId = order.OrderNumber,
                OrderDate = order.OrderDate,
                SupplierId = order.SupplierId ?? 0,
                SupplierName = order.SupplierName,
                Total = order.Total,
                Status = order.Status.ToString(),
            }).ToList();

            return new PaginationResponseDto<OrderSummaryResponseDto>
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Data = mappedData
            };
        }


        //public async Task<string> ChangeOrderToDeliverd(OrderConfirmDto confirmDto)
        //{
        //    // int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("User is not assigned to any branch.");
        //    int branchId = 1;

        //    #region data validation
        //    if (confirmDto == null)
        //        throw new ValidationException("empty confirmation fields");

        //    var errors = new List<string>();
        //    if (confirmDto.OrderId <= 0)
        //        errors.Add("Order Id is required and must be greater than zero");

        //    if (confirmDto.ItemsConfirmDtos == null || !confirmDto.ItemsConfirmDtos.Any())
        //        errors.Add("You must confirm the items at the order.");

        //    if (confirmDto.RecievedDate == null)
        //        errors.Add("Recieved date is required");

        //    if (errors.Any())
        //        throw new ValidationException(errors);
        //    errors.Clear();
        //    #endregion

        //    int orderId = confirmDto.OrderId;
        //    var order = await _unitOfWork.GetRepository<Order>().GetByIdAsQueryable(orderId)
        //        .Include(o => o.Items)
        //        .FirstOrDefaultAsync();


        //    #region data validation 2
        //    if (order == null || order.BranchId != branchId)
        //        throw new KeyNotFoundException("No order with this id");

        //    if (order.Status == OrderStatus.Deliverd)
        //    {
        //        throw new BusinessRuleException("This order has been already delivered");
        //    }
        //    else if (order.Status == OrderStatus.Cancelled)
        //    {
        //        throw new BusinessRuleException("this order has been cancelled");
        //    }


        //    if (order.Items == null || !order.Items.Any())
        //    {
        //        errors.Add("you cannot receive empty order");
        //        foreach(var orderItemConfirm in confirmDto.ItemsConfirmDtos!)
        //        {
        //            if (orderItemConfirm != null)
        //            {
        //                if(orderItemConfirm.ProductId == 0)
        //                {
        //                    errors.Add("Product id is required");
        //                }
        //            }
        //        }
        //    }

        //    var orderProductIds = order.Items.Select(i => i.ProductId).ToList();
        //    var incomingProductIds = confirmDto.ItemsConfirmDtos.Select(i => i.ProductId).ToList();
        //    var invalidItems = incomingProductIds.Except(orderProductIds).ToList();

        //    if (invalidItems.Any())
        //    {
        //        errors.Add("Some items in the request do not belong to this order!");
        //    }
        //    if (confirmDto.RecievedDate == null)
        //    {
        //        errors.Add("Recived date is required, please write it");
        //    }

        //    if (errors.Any())
        //    {
        //        throw new ValidationException(errors);
        //    }
        //    errors.Clear();
        //    #endregion

        //    var productIds = order.Items.Select(i => i.ProductId);

        //    var inventories = await _unitOfWork.GetRepository<Inventory>().GetAllAsIQueryable().AsTracking()
        //        .Where(inv => productIds.Contains(inv.ProductId) && inv.BranchId == branchId).ToListAsync();

        //    #region changes after deliver
        //    order.Status = Domain.Enums.OrderStatus.Deliverd;
        //    order.RecievedDeliveryDate = confirmDto.RecievedDate ?? DateTime.Now;

        //    #region price
        //    //var products = await _unitOfWork.GetRepository<Product>().GetAllAsIQueryable().AsTracking()
        //    //    .Where(product => productIds.Contains(product.Id)).ToListAsync();

        //    //if (!products.Any())
        //    //{
        //    //    throw new Exception("No products");
        //    //}
        //    ///TODO exception handling

        //    //foreach (var product in products)
        //    //{
        //    //    var itemsConfirm = confirmDto!.ItemsConfirmDtos.FirstOrDefault(confirmDto => confirmDto.ProductId == product.Id);

        //    //    if (itemsConfirm != null)
        //    //    {
        //    //        product.CostPrice = itemsConfirm.CostPrice;
        //    //        product.SellingPrice = itemsConfirm.SellPrice;
        //    //    }
        //    //}
        //    #endregion

        //    foreach (var orderItem in order.Items)
        //    {
        //        var itemsConfirm = confirmDto.ItemsConfirmDtos.FirstOrDefault(c => c.ProductId == orderItem.ProductId);

        //        if (itemsConfirm != null)
        //        {
        //            int totalIssues = itemsConfirm.ItemsIssueDtos?.Sum(issue => issue.Quantity) ?? 0;
        //            int netQuantityToAdd = orderItem.Quantity - totalIssues;

        //            var existingInventory = inventories.FirstOrDefault(inv => inv.ProductId == orderItem.ProductId);

        //            if (existingInventory != null)
        //            {
        //                existingInventory.Quantity += netQuantityToAdd;
        //            }
        //            else
        //            {
        //                var newInventory = new Inventory
        //                {
        //                    ProductId = orderItem.ProductId,
        //                    BranchId = branchId,
        //                    Quantity = netQuantityToAdd
        //                };

        //                await _unitOfWork.GetRepository<Inventory>().AddAsync(newInventory);
        //            }
        //        }
        //    }
        //    #endregion

        //    int rowsAffected = await _unitOfWork.SaveChangesAsync();

        //    if (rowsAffected <= 0)
        //        throw new DbUpdateFailedException("Status not changed");

        //    await _transactionService.CreateConfirmOrderTransactionAsync(order);
        //    await _notificationService.CreateOrderDeliveredNotification(order.Id);
        //    await _deliveryIssue.CreateDeliveryIssue(confirmDto);

        //    return order.Status.ToString();
        //}

        public async Task<string> ChangeOrderToDeliverd(OrderConfirmDto confirmDto)
        {
            //int branchId = 2;
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 

            #region Data Validation
            if (confirmDto == null)
                throw new ValidationException("Empty confirmation fields");

            var errors = new List<string>();
            if (confirmDto.OrderId <= 0)
                errors.Add("Order Id is required and must be greater than zero");

            if (confirmDto.ItemsConfirmDtos == null || !confirmDto.ItemsConfirmDtos.Any())
                errors.Add("You must confirm the items at the order.");
            else if (confirmDto.ItemsConfirmDtos.Any(i => i.ProductId <= 0))
                errors.Add("Product id is required for all items.");

            if (confirmDto.RecievedDate == null)
                errors.Add("Recieved date is required");

            if (errors.Any())
                throw new ValidationException(errors);
            #endregion

            int orderId = confirmDto.OrderId;
            var order = await _unitOfWork.GetRepository<Order>().GetByIdAsQueryable(orderId)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync();


            #region Business Validation
            if (order == null || order.BranchId != branchId)
                throw new KeyNotFoundException("No order with this id");

            if (order.Status == OrderStatus.Deliverd)
                throw new BusinessRuleException("This order has been already delivered");

            if (order.Status == OrderStatus.Cancelled)
                throw new BusinessRuleException("This order has been cancelled");

            if (order.Items == null || !order.Items.Any())
                throw new BusinessRuleException("You cannot receive an empty order");

            var orderProductIds = order.Items.Select(i => i.ProductId).ToList();
            var incomingProductIds = confirmDto.ItemsConfirmDtos!.Select(i => i.ProductId).ToList();
            var invalidItems = incomingProductIds.Except(orderProductIds).ToList();

            if (invalidItems.Any())
                throw new ValidationException("Some items in the request do not belong to this order!");
            #endregion

            using var transaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                var productIds = order.Items.Select(i => i.ProductId);
                var inventories = await _unitOfWork.GetRepository<Inventory>().GetAllAsIQueryable().AsTracking()
                    .Where(inv => productIds.Contains(inv.ProductId) && inv.BranchId == branchId).ToListAsync();

                #region Changes after deliver
                order.Status = Domain.Enums.OrderStatus.Deliverd;
                order.RecievedDeliveryDate = confirmDto.RecievedDate ?? DateTime.UtcNow;

                foreach (var orderItem in order.Items)
                {
                    var itemsConfirm = confirmDto.ItemsConfirmDtos!.FirstOrDefault(c => c.ProductId == orderItem.ProductId);

                    if (itemsConfirm != null)
                    {
                        int totalIssues = itemsConfirm.ItemsIssueDtos?.Sum(issue => issue.Quantity) ?? 0;
                        int netQuantityToAdd = orderItem.Quantity - totalIssues;

                        var existingInventory = inventories.FirstOrDefault(inv => inv.ProductId == orderItem.ProductId);

                        if (existingInventory != null)
                        {
                            existingInventory.Quantity += netQuantityToAdd;
                        }
                        else
                        {
                            var newInventory = new Inventory
                            {
                                ProductId = orderItem.ProductId,
                                BranchId = branchId,
                                Quantity = netQuantityToAdd
                            };
                            await _unitOfWork.GetRepository<Inventory>().AddAsync(newInventory);
                        }

                        if (netQuantityToAdd > 0)
                        {
                            await _unitOfWork.GetRepository<InventoryBatch>().AddAsync(new InventoryBatch
                            {
                                ProductId = orderItem.ProductId,
                                BranchId = branchId,
                                Quantity = netQuantityToAdd,
                                BatchNumber = string.IsNullOrWhiteSpace(itemsConfirm.BatchNumber)
                                    ? $"{order.OrderNumber}-{orderItem.ProductId}"
                                    : itemsConfirm.BatchNumber.Trim(),
                                ExpiryDate = itemsConfirm.ExpiryDate ?? orderItem.Product?.ExpiryDate,
                                CostPrice = itemsConfirm.CostPrice ?? orderItem.Price,
                                ReceivedAt = order.RecievedDeliveryDate ?? DateTime.UtcNow
                            });
                        }
                    }
                }
                #endregion

                int rowsAffected = await _unitOfWork.SaveChangesAsync();
                if (rowsAffected <= 0)
                    throw new DbUpdateFailedException("Status not changed");

                await _transactionService.CreateConfirmOrderTransactionAsync(order);
                await _notificationService.CreateOrderDeliveredNotification(order);
                await _deliveryIssue.CreateDeliveryIssue(confirmDto);

                await transaction.CommitAsync();

                return order.Status.ToString();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw; 
            }
        }
        public async Task<OrderCountsDto> Counts()
        {
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("User is not assigned to any branch.");
            //int branchId = 2;

            var pendingCount = await _unitOfWork.GetRepository<Order>().GetAllAsIQueryable()
                .CountAsync(o => o.BranchId == branchId && o.Status == OrderStatus.Pending);

            var deliveredCount = await _unitOfWork.GetRepository<Order>().GetAllAsIQueryable()
                .CountAsync(o => o.BranchId == branchId && o.Status == OrderStatus.Deliverd);

            var deliveredTotals = await _unitOfWork.GetRepository<Order>().GetAllAsIQueryable()
                .Where(o => o.BranchId == branchId && o.Status == OrderStatus.Deliverd)
                .Select(o => o.Total)
                .ToListAsync();

            var totalRevenue = deliveredTotals.Sum();

            var roundedRevenue = Math.Round(totalRevenue);
            
            return new OrderCountsDto
            {
                PendingOrdersCount = pendingCount,
                DeliveredOrdersCount = deliveredCount,
                TotalOrdersRevenue = roundedRevenue
            };
        }

        public async Task<OrderConfirmResponseDto> ViewConfirm(int id)
        {
            //int branchId = 2;
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 

            var order = await _unitOfWork.GetRepository<Order>().GetByIdAsQueryable(id)
                .Include(x => x.Supplier)
                .Include(o => o.Items)
                .ThenInclude(p => p.Product)
                .FirstOrDefaultAsync();

            if (order == null || order.BranchId != branchId)
            {
                throw new KeyNotFoundException("no order found with this id");
                
            }
            else if (order.Status == OrderStatus.Deliverd)
            {
                throw new BusinessRuleException("This order has been delivered already");
            }
            else if (order.Status == OrderStatus.Cancelled)
            {
                throw new BusinessRuleException("This order has been cancelled");
            }
            var itemsDtos = order.Items.Select(item => new OrderItemConfirmResponseDto
            {
                ProductId = item.ProductId,
                SKU = item.Product?.SKU ?? "N/A",
                Price = item.Price,
                OrderedQuantity = item.Quantity,
            }).ToList();

            return new OrderConfirmResponseDto
            {
                OrderId = id,
                OrderStringId = order.OrderNumber,
                SupplierId = order.SupplierId ?? 0,
                SupplierStringId = order.Supplier!.SupplierNumber ?? "Deleted Supplier",
                SupplierName = order.SupplierName,
                ItemsConfirmResponseDtos = itemsDtos
            };
        }

       

    }
}
