using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Orders;
using Tanzeem.Domain.Entities.Products;
using Tanzeem.Domain.Entities.Transactions;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.Alerts;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Services.Notifications;
using Tanzeem.Shared;
using Tanzeem.Shared.Dtos;
using Tanzeem.Shared.Dtos.Notifications;

namespace Tanzeem.Services.Alerts
{
    
    public class AlertService(IUnitOfWork _unitOfWork,ICurrentService _currentService) : IAlertService
    {
        public async Task<PaginationResponseDto<AlertDto>> ShowAlerts(
        NotificationType? type, int page, int pageSize, int ExpiryFilterByMonths = 3
            , int DeadStockFilterByMonths = 3)
        {
            if (page <= 0) page = 1;
            if (pageSize > 20) pageSize = 20;

            switch (type)
            {
                case NotificationType.LowStockAlert:
                    var lowData = await ShowLowStockAlerts();
                    return lowData.ToPaginatedResponse(page, pageSize);

                case NotificationType.DeadStockAlert:
                    var deadStockData = await ShowDeadStockAlerts(DeadStockFilterByMonths);
                    return deadStockData.ToPaginatedResponse(page, pageSize);

                case NotificationType.ExpiryAlert:
                    var expiryData = await ShowExpiryAlerts(ExpiryFilterByMonths);
                    return expiryData.ToPaginatedResponse(page, pageSize);

                case NotificationType.OutOfStock:
                    var outData = await ShowOutStockAlerts();
                    return outData.ToPaginatedResponse(page, pageSize);

                case NotificationType.OrderUpdate:
                    var orderData = await ShowOrderUpdates();
                     return orderData.ToPaginatedResponse(page, pageSize);

                default:
                    var lowAlerts = await ShowLowStockAlerts();
                    var deadAlerts = await ShowDeadStockAlerts(DeadStockFilterByMonths);
                    var expiryAlerts = await ShowExpiryAlerts(ExpiryFilterByMonths);
                    var outAlerts = await ShowOutStockAlerts();
                    var orderAlerts = await ShowOrderUpdates();

                    var allAlerts = lowAlerts
                        .Concat(deadAlerts)
                        .Concat(expiryAlerts)
                        .Concat(outAlerts)
                        .Concat(orderAlerts)
                        .OrderBy(x => x.ProductId)
                        .ToList();

                    return allAlerts.ToPaginatedResponse(page, pageSize);
            }
        }


        public async Task<IEnumerable<AlertDto>> ShowDeadStockAlerts(int DeadStockFilterByMonths =3)
        {

            //int branchId = 1;
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 

            var recentlySoldIds = _unitOfWork.GetRepository<TransactionItem>()
                .GetAllAsIQueryable()
                .Where(ti => ti.Transaction.Type == TransactionType.Out
                          && ti.Transaction.CreatedAt > DateTime.UtcNow.AddMonths(- DeadStockFilterByMonths)
                          && ti.Transaction.BranchId == branchId)
                .Select(ti => ti.ProductId)
                .Distinct();

            var branchBatchTotals = _unitOfWork.GetRepository<InventoryBatch>()
                .GetAllAsIQueryable()
                .Where(batch => batch.BranchId == branchId)
                .GroupBy(batch => batch.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Quantity = g.Sum(batch => batch.Quantity)
                });

            var rawQuery = await _unitOfWork.GetRepository<Inventory>()
                .GetAllAsIQueryable()
                .GroupJoin(
                    branchBatchTotals,
                    inventory => inventory.ProductId,
                    batchTotal => batchTotal.ProductId,
                    (inventory, batchTotals) => new { inventory, batchQuantity = batchTotals.Select(x => x.Quantity).FirstOrDefault() })
                .Where(inv => inv.inventory.BranchId == branchId
                           && inv.batchQuantity > 0
                           && !recentlySoldIds.Contains(inv.inventory.ProductId))
                .Select(inv => new
                {
                    inv.inventory.ProductId,
                    inv.inventory.Product.Name,
                    inv.inventory.Product.SKU,
                    LastSaleDate = inv.inventory.Product.TransactionItems
                        .Where(ti => ti.Transaction.Type == TransactionType.Out && ti.Transaction.BranchId == branchId)
                        .Select(ti => (DateTime?)ti.Transaction.CreatedAt)
                        .Max() 
                }).ToListAsync();

            var alerts = rawQuery
                .Select(x => new AlertDto
                {
                    AlertTitle = "Dead Stock Alert",
                    AlertDescription = $"{x.Name} has not moved in " +
                                       (x.LastSaleDate.HasValue
                                           ? NotificationServiceHelper.GenerateSinceDate(x.LastSaleDate.Value)
                                           : "No sales recorded yet"),
                    //AlertDescription = $"{x.Name} has not moved in " + x.LastSaleDate,
                    AlertSubTitle = $"{x.Name} (SKU: {x.SKU})",
                    ProductId = x.ProductId,
                    Type = NotificationType.DeadStockAlert,
                    Priority = AlertPriority.Critical.ToString(),
                }).OrderBy(x => x.ProductId);
            return alerts;
        }
        public async Task<IEnumerable<AlertDto>> ShowLowStockAlerts()
        {
            //int branchId = 1;
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 

            var branchBatchTotals = _unitOfWork.GetRepository<InventoryBatch>()
                .GetAllAsIQueryable()
                .Where(batch => batch.BranchId == branchId)
                .GroupBy(batch => batch.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Quantity = g.Sum(batch => batch.Quantity)
                });

            var alerts = await _unitOfWork.GetRepository<Inventory>().GetAllAsIQueryable()
                .GroupJoin(
                    branchBatchTotals,
                    inventory => inventory.ProductId,
                    batchTotal => batchTotal.ProductId,
                    (inventory, batchTotals) => new { inventory, batchQuantity = batchTotals.Select(x => x.Quantity).FirstOrDefault() })
                .Where(x => x.inventory.BranchId == branchId
                    && x.batchQuantity > 0
                    && x.batchQuantity <= x.inventory.Product.ReorderLevel)
                .OrderBy(x => x.inventory.ProductId)
                .Select(x => new AlertDto
                {
                    AlertTitle = "Low Stock Alert",
                    AlertDescription = $"{x.inventory.Product.Name} stock is below minimum threshold",
                    AlertSubTitle = $"{x.inventory.Product.Name}(SKU: {x.inventory.Product.SKU}), Current Quantity: {x.batchQuantity}",
                    ProductId = x.inventory.ProductId,
                    Type = NotificationType.LowStockAlert,
                    Priority = AlertPriority.Warning.ToString(),
                }).ToListAsync();
            return alerts;         
        }
          
        public async Task<IEnumerable<AlertDto>> ShowExpiryAlerts(int ExpiryFilterByMonths = 3)
        {
            //int companyId = 14;
            int companyId = _currentService.CompanyId ?? throw new UnauthorizedAccessException("No company id assigned"); 

            var batches = await _unitOfWork.GetRepository<InventoryBatch>().GetAllAsIQueryable()
                .Where(batch => batch.Quantity > 0
                    && batch.ExpiryDate.HasValue
                    && batch.ExpiryDate <= DateTime.UtcNow.AddMonths(ExpiryFilterByMonths)
                    && batch.Product.CompanyId == companyId)
                .Select(batch => new
                {
                    ProductId = batch.Product.Id,
                    batch.Product.Name,
                    batch.Product.SKU,
                    batch.BatchNumber,
                    ExpiryDate = batch.ExpiryDate!.Value,
                    batch.Quantity
                })
                .ToListAsync();

            var alerts = batches.Select(batch =>
            {
                bool isExpired = batch.ExpiryDate <= DateTime.UtcNow;

                return new AlertDto
                {
                    AlertTitle = isExpired ? "Expired Product" : "Expiry Warning",
                    AlertDescription = isExpired
                        ? $"{batch.Name} batch {batch.BatchNumber} has already expired!"
                        : $"{batch.Name} batch {batch.BatchNumber} will expire in {NotificationServiceHelper.GenerateSinceDate(batch.ExpiryDate)}",
                    AlertSubTitle = $"{batch.Name} (SKU: {batch.SKU}), Quantity: {batch.Quantity}",
                    ProductId = batch.ProductId,
                    Type = NotificationType.ExpiryAlert,
                    Priority = isExpired ? nameof(AlertPriority.Critical) : nameof(AlertPriority.Warning),
                };
            }).OrderBy(p => p.ProductId);

            return alerts;
        }

        public async Task<IEnumerable<AlertDto>> ShowOutStockAlerts()
        {
            //int branchId = 1;
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 
            var branchBatchTotals = _unitOfWork.GetRepository<InventoryBatch>()
                .GetAllAsIQueryable()
                .Where(batch => batch.BranchId == branchId)
                .GroupBy(batch => batch.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Quantity = g.Sum(batch => batch.Quantity)
                });

            var alerts = await _unitOfWork.GetRepository<Inventory>().GetAllAsIQueryable()
                .GroupJoin(
                    branchBatchTotals,
                    inventory => inventory.ProductId,
                    batchTotal => batchTotal.ProductId,
                    (inventory, batchTotals) => new { inventory, batchQuantity = batchTotals.Select(x => x.Quantity).FirstOrDefault() })
                .Where(x => x.inventory.BranchId == branchId
                    && x.batchQuantity == 0)
                .OrderBy(x => x.inventory.ProductId)
                .Select(x => new AlertDto
                {
                    AlertTitle = "Out Of Stock Alert",
                    AlertDescription = $"{x.inventory.Product.Name} is completely out of stock",
                    AlertSubTitle = $"{x.inventory.Product.Name}(SKU: {x.inventory.Product.SKU})",
                    ProductId = x.inventory.ProductId,
                    Type = NotificationType.OutOfStock,
                    Priority = AlertPriority.Critical.ToString(),
                }).ToListAsync();

            return alerts;
        }

        public async Task<IEnumerable<AlertDto>> ShowOrderUpdates()
        {
            //int branchId = 2;
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 

            var recentDate = DateTime.UtcNow.AddDays(-2);

            var alerts = await _unitOfWork.GetRepository<Order>().GetAllAsIQueryable()
            .Where(x => x.BranchId == branchId && 
                   (x.Status == OrderStatus.Pending || x.Status == OrderStatus.Deliverd && x.RecievedDeliveryDate >= recentDate))
            .OrderByDescending(a => a.OrderDate)
            .Select(order => new AlertDto
            {
            AlertTitle = order.Status == OrderStatus.Pending ? "Order Pending" : "Order Delivered",

            AlertDescription = order.Status == OrderStatus.Pending
                ? $"Order #{order.Id} is waiting for processing"
                : $"Order #{order.Id} has been successfully delivered",

            AlertSubTitle = order.Status == OrderStatus.Pending ? $"order created at: {order.OrderDate}" : $"order recived at: {order.RecievedDeliveryDate}",
            Type = NotificationType.OrderUpdate,
            Priority = AlertPriority.Info.ToString(),
            }).ToListAsync();
            return alerts;
        }

        //public async Task<object> Counts()
        //{
        //    var deadTask = ShowDeadStockAlerts().CountAsync();
        //    var outStockTask = ShowOutStockAlerts().CountAsync();
        //    var expiryTask = ShowExpiryAlerts().CountAsync();
        //    var lowStockTask = ShowLowStockAlerts().CountAsync();
        //    var infoTask = ShowOrderUpdates().CountAsync();

        //    await Task.WhenAll(deadTask, outStockTask, expiryTask, lowStockTask, infoTask);

        //    int deadCount = deadTask.Result;
        //    int outStockCount = outStockTask.Result;
        //    int expiryCount = expiryTask.Result;
        //    int lowStockCount = lowStockTask.Result;
        //    int infoCount = infoTask.Result;

        //    int criticalTotal = deadCount + outStockCount;
        //    int warningTotal = expiryCount + lowStockCount;

        //    return new
        //    {
        //        deadCount = deadCount,
        //        criticalCount = criticalTotal,
        //        warningCount = warningTotal,
        //        infoCount = infoCount
        //    };

        //}
        public async Task<AlertCountsDto> Counts()
        {
            var deadAlerts = await ShowDeadStockAlerts();
            int deadCount = deadAlerts.Count();

            var outOfStockAlerts = await ShowOutStockAlerts();
            int outCount = outOfStockAlerts.Count();

            var expiryAlerts = await ShowExpiryAlerts();
            int expiryCount = expiryAlerts.Count();

            var lowStockAlerts = await ShowLowStockAlerts();
            int lowCount = lowStockAlerts.Count();

            var infoAlerts = await ShowOrderUpdates();
            int infoCount = infoAlerts.Count();

            return new AlertCountsDto
            {
                DeadCount = deadCount,
                CriticalCount = deadCount + outCount,
                WarningCount = expiryCount + lowCount,
                InfoCount = infoCount
            };
        }
    }
}
