using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.CustomExceptions;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Notifications;
using Tanzeem.Domain.Entities.Orders;
using Tanzeem.Domain.Entities.Products;
using Tanzeem.Domain.Entities.Settings;
using Tanzeem.Domain.Entities.Transactions;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Services.Abstractions.Notifications;
using Tanzeem.Shared.Dtos;
using Tanzeem.Shared.Dtos.Notifications;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Tanzeem.Services.Notifications
{
    public class NotificationService(IUnitOfWork _unitOfWork, ICurrentService _currentService) : INotificationService
    {
        //try query filters
        public async Task<PaginationResponseDto<NotificationDto>> GetAllNotifications(int page, int pageSize)
        {
            //int branchId = 1;
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 
            if (page <= 0) page = 1;

            const int maxPageSize = 20;

            if (pageSize > maxPageSize) pageSize = maxPageSize;

            var query = _unitOfWork.GetRepository<Notification>().GetAllAsIQueryable()
                .OrderByDescending(x => x.CreatedAt).Where(x => x.BranchId == branchId);
                

            var rowsCount = query.Count();
            
            var messages = query.Skip((page - 1) * pageSize)
                .Take(pageSize);
            
            var messageDtos = await messages.Select(x => new NotificationDto
            {
                Id = x.Id,
                Title = x.Title,
                IsRead = x.IsRead,
                CreatedAt = x.CreatedAt,
                Message = x.Message,
                Type = x.Type,
            }).ToListAsync();


            return new PaginationResponseDto<NotificationDto>()
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = rowsCount,
                Data = messageDtos
            };
        }

        /// <summary>
        /// it creates low and out stock after every stock out operation if conditions are met.
        /// </summary>
        /// <param name="lowStockItems"></param>
        /// <param name="inventories"></param>
        /// <returns>notifications ids</returns>
        /// <exception cref="Exception"></exception>        
        public async Task<IEnumerable<int>> CreateLowStockNotification(List<TransactionItem> lowStockItems,List<Inventory> inventories)
        {
            //int branchId = 1;
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 
            
            List<Notification> notifications = new List<Notification>();
            var productIds = lowStockItems.Select(item => item.ProductId).Distinct().ToList();
            var branchBatchTotals = await _unitOfWork.GetRepository<InventoryBatch>().GetAllAsIQueryable()
                .Where(batch => batch.BranchId == branchId && productIds.Contains(batch.ProductId))
                .GroupBy(batch => batch.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Quantity = g.Sum(batch => batch.Quantity)
                })
                .ToDictionaryAsync(x => x.ProductId, x => x.Quantity);

            foreach (var item in lowStockItems)
            {
                var inventory = inventories.FirstOrDefault(inv => inv.ProductId == item.ProductId && inv.BranchId == branchId);
                
                if (inventory == null) {
                    throw new KeyNotFoundException($"No inventory found for product ID {item.ProductId}");
                }
                var currentQuantity = branchBatchTotals.GetValueOrDefault(item.ProductId, 0);
                if (currentQuantity == 0)
                {
                    Notification notification = new Notification
                    {
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                        Type = NotificationType.OutOfStock,
                        Message = $"Product: {inventory.Product.Name} is completely out of stock",
                        Title = "Out Of Stock Alert",
                        BranchId = branchId
                    };
                    notifications.Add(notification);
                    await _unitOfWork.GetRepository<Notification>().AddAsync(notification);
                }
                else
                {
                    Notification notification = new Notification
                    {
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                        Type = NotificationType.LowStockAlert,
                        Message = $"Product: {inventory.Product.Name} has reached the reorder level. Current quantity: {currentQuantity}",
                        Title = "Low Stock Alert",
                        BranchId = branchId
                    };

                    notifications.Add(notification);
                    await _unitOfWork.GetRepository<Notification>().AddAsync(notification);
                }
            }
            if (notifications.Any())
            {
                int affected = await _unitOfWork.SaveChangesAsync();
                if (affected <= 0)
                {
                    throw new DbUpdateFailedException("Failed to save notifications to the database");
                }
            }
            return notifications.Select(x => x.Id).ToList();
        }
        
        public async Task CreateDeadStockNotification(int branchId)
        {
            var deadAlert = await _unitOfWork.GetRepository<AlertConfigurations>().GetAllAsIQueryable()
                .FirstOrDefaultAsync(x => x.BranchId == branchId);

            if (deadAlert is null || deadAlert.IsActive_DeadAlert == false)
            {
                return;
            }

            var recentlySoldIds = await _unitOfWork.GetRepository<TransactionItem>().GetAllAsIQueryable()
                .IgnoreQueryFilters()
                .Where(ti => ti.Transaction.Type == TransactionType.Out && ti.Transaction.CreatedAt > DateTime.UtcNow.AddDays(- deadAlert.DaysWithoutMovement)
                && ti.Transaction.BranchId == branchId)
                .Select(ti => ti.ProductId)
                .Distinct()
                .ToListAsync();

            var branchBatchTotals = _unitOfWork.GetRepository<InventoryBatch>().GetAllAsIQueryable()
                .IgnoreQueryFilters()
                .Where(batch => batch.BranchId == branchId)
                .GroupBy(batch => batch.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Quantity = g.Sum(batch => batch.Quantity)
                });

            var inventories = await _unitOfWork.GetRepository<Inventory>().GetAllAsIQueryable()
                .IgnoreQueryFilters()
                .GroupJoin(
                    branchBatchTotals,
                    inventory => inventory.ProductId,
                    batchTotal => batchTotal.ProductId,
                    (inventory, batchTotals) => new { inventory, batchQuantity = batchTotals.Select(x => x.Quantity).FirstOrDefault() })
                .Where(inv => !recentlySoldIds.Contains(inv.inventory.ProductId) && inv.inventory.BranchId == branchId && inv.batchQuantity > 0)
                .ToListAsync();

            if (inventories.Count() == 0)
            {
                return;
            }

            Notification notification = new Notification
            {
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    Type = NotificationType.DeadStockAlert,
                    Message = $"There are {inventories.Count} products have shown no sales activity since {deadAlert.DaysWithoutMovement} days, Check them now.",
                    BranchId = branchId,
                    Title = "Dead Stock Alert"
            };

            await _unitOfWork.GetRepository<Notification>().AddAsync(notification);

            int affected = await _unitOfWork.SaveChangesAsync();
            if (affected <= 0 && inventories.Any())
                throw new DbUpdateFailedException("Failed to save the dead stock notification.");

        }

        public async Task CreateExpiryNotification(int branchId)
        {
            var expiryAlert = await _unitOfWork.GetRepository<AlertConfigurations>().GetAllAsIQueryable()
            .FirstOrDefaultAsync(x => x.BranchId == branchId);

            if (expiryAlert is null || expiryAlert.IsActive_ExpiryAlert == false)
            {
                return;
            }

            var expiringBatchesCount = await _unitOfWork.GetRepository<InventoryBatch>().GetAllAsIQueryable()
                .IgnoreQueryFilters()
                .Where(batch => batch.BranchId == branchId
                    && batch.Quantity > 0
                    && batch.ExpiryDate.HasValue
                    && batch.ExpiryDate <= DateTime.UtcNow.AddDays(expiryAlert.DaysBeforeExpiry))
                .CountAsync();

            if (expiringBatchesCount == 0)
            {
                return;
            }

            Notification notification = new Notification
            {
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                Type = NotificationType.ExpiryAlert,
                Message = $"There are {expiringBatchesCount} inventory batches near expiry, Check them now.",
                BranchId = branchId,
                Title = "Expiry Warning"
            };

            await _unitOfWork.GetRepository<Notification>().AddAsync(notification);
            int affected = await _unitOfWork.SaveChangesAsync();
            if (affected <= 0)
                throw new DbUpdateFailedException("Failed to save the near expiry notification.");
        }

        public async Task createLowStockNotificationWeekly(int branchId)
        {
            var Alert = await _unitOfWork.GetRepository<AlertConfigurations>().GetAllAsIQueryable()
            .FirstOrDefaultAsync(x => x.BranchId == branchId);

            if (Alert is null || Alert.IsActive_LowAlert == false)
            {
                return;
            }

            var branchBatchTotals = _unitOfWork.GetRepository<InventoryBatch>().GetAllAsIQueryable()
                .IgnoreQueryFilters()
                .Where(batch => batch.BranchId == branchId)
                .GroupBy(batch => batch.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Quantity = g.Sum(batch => batch.Quantity)
                });

            var inventories = _unitOfWork.GetRepository<Inventory>().GetAllAsIQueryable()
                .IgnoreQueryFilters()
                .Include(inv => inv.Product)
                .GroupJoin(
                    branchBatchTotals,
                    inventory => inventory.ProductId,
                    batchTotal => batchTotal.ProductId,
                    (inventory, batchTotals) => new { inventory, batchQuantity = batchTotals.Select(x => x.Quantity).FirstOrDefault() })
                .Where(inv => inv.inventory.BranchId == branchId
                && inv.batchQuantity > 0
                && inv.batchQuantity < inv.inventory.Product.ReorderLevel)
                .Count();

            if (inventories ==0 )
            {
                return;
            }

            Notification notification = new Notification()
            {
                Title = "Low Stock Alert",
                IsRead = false,
                BranchId = branchId,
                CreatedAt = DateTime.UtcNow,
                Type = NotificationType.LowStockAlert,
                Message = $"{inventories} products have reached the reorder level, Check them now."
            };
            await _unitOfWork.GetRepository<Notification>().AddAsync(notification);
            int affected = await _unitOfWork.SaveChangesAsync();
            if (affected <= 0)
                throw new DbUpdateFailedException("Failed to save the low stock notification.");
        }
        
        public async Task createOutOfStockNotificationWeekly(int branchId)
        {
            var Alert = await _unitOfWork.GetRepository<AlertConfigurations>().GetAllAsIQueryable()
            .FirstOrDefaultAsync(x => x.BranchId == branchId);

            if (Alert is null || Alert.IsActive_OutAlert == false)
            {
                return;
            }
            var branchBatchTotals = _unitOfWork.GetRepository<InventoryBatch>().GetAllAsIQueryable()
                .IgnoreQueryFilters()
                .Where(batch => batch.BranchId == branchId)
                .GroupBy(batch => batch.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Quantity = g.Sum(batch => batch.Quantity)
                });

            var inventories = _unitOfWork.GetRepository<Inventory>().GetAllAsIQueryable()
                .IgnoreQueryFilters()
                .Include(inv => inv.Product)
                .GroupJoin(
                    branchBatchTotals,
                    inventory => inventory.ProductId,
                    batchTotal => batchTotal.ProductId,
                    (inventory, batchTotals) => new { inventory, batchQuantity = batchTotals.Select(x => x.Quantity).FirstOrDefault() })
                .Where(inv => inv.inventory.BranchId == branchId
                && inv.batchQuantity == 0)
                .Count();
            
            if (inventories == 0)
            {
                return;
            }
            Notification notification = new Notification()
            {
                Title = "Out of stock Alert",
                IsRead = false,
                BranchId = branchId,
                CreatedAt = DateTime.UtcNow,
                Type = NotificationType.OutOfStock,
                Message = $"{inventories} products is completely out of stock, Check them now."
            };
            await _unitOfWork.GetRepository<Notification>().AddAsync(notification);
            int affected = await _unitOfWork.SaveChangesAsync();
            if (affected <= 0)
                throw new DbUpdateFailedException("Failed to save the out of stock notification.");
        }
        
        public async Task CreateOrderDeliveredNotification(Order order)
        {
            var Alert = await _unitOfWork.GetRepository<AlertConfigurations>().GetAllAsIQueryable()
            .FirstOrDefaultAsync(x => x.BranchId == order.BranchId);

            if (Alert is null || Alert.IsActive_OrderUpdateAlert == false)
            {
                return;
            }
            //var order = _unitOfWork.GetRepository<Order>().GetByIdAsQueryable(orderId)
            //    .FirstOrDefault();

            if (order == null)
            {
                return;
            }

            Notification notification = new Notification
            {
                Title = "Order Delivered",
                IsRead = false,
                BranchId = order.BranchId,
                CreatedAt = DateTime.UtcNow,
                Type = NotificationType.OrderUpdate,
                Message = $"Order ID: {order.Id} has been delivered, Check it now."
            };

            await _unitOfWork.GetRepository<Notification>().AddAsync(notification);
            
            int affected = await _unitOfWork.SaveChangesAsync();
            
            if (affected <= 0)
                throw new DbUpdateFailedException("Failed to save the order delivered notification.");
        }

        public async Task CreateNewOrderNotification(Order order)
        {
            var Alert = await _unitOfWork.GetRepository<AlertConfigurations>().GetAllAsIQueryable()
            .FirstOrDefaultAsync(x => x.BranchId == order.BranchId);
            
            if (Alert is null || Alert.IsActive_NewOrderAlert == false)
            {
                return;
            }

            Notification notification = new Notification
            {
                BranchId = order.BranchId,
                IsRead= false,
                Title = "New Order is created",
                Type = NotificationType.OrderUpdate,
                CreatedAt= DateTime.UtcNow,
                Message = $"New order (order Id:{order.Id} )has been created, check it now!"
            };
            await _unitOfWork.GetRepository<Notification>().AddAsync(notification);
            int affected = await _unitOfWork.SaveChangesAsync();

            if (affected <= 0)
                throw new DbUpdateFailedException("Failed to save the new order notification.");
        }
        /// <summary>
        /// hangfire uses this method to check
        /// - low stck
        /// - out stock
        /// - dead stock
        /// - expiry date
        /// </summary>
        /// <returns></returns>
        public async Task CreateNotification()
        {
            List<int> branchIds = _unitOfWork.GetRepository<Branch>().GetAllAsIQueryable()
                .Select(br => br.Id)
                .Distinct()
                .ToList();
            foreach (var branchId in branchIds)
            {
                await CreateDeadStockNotification(branchId);
                await CreateExpiryNotification(branchId);
                await createLowStockNotificationWeekly(branchId);
                await createOutOfStockNotificationWeekly(branchId);
            }
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned");
            
            var notification = await _unitOfWork.GetRepository<Notification>().GetByIdAsync(notificationId);

            
            if (notification == null || notification.BranchId != branchId) throw new KeyNotFoundException("no notification with this id");

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                _unitOfWork.GetRepository<Notification>().UpdateAsync(notification);
                await _unitOfWork.SaveChangesAsync();
            }

            return true;
        }

        public async Task MarkAllAsReadAsync()
        {
           // int branchId = 1;
           int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 
            
            var unreadNotifications = await _unitOfWork.GetRepository<Notification>()
                .GetAllAsIQueryable()
                .Where(n => n.BranchId == branchId && !n.IsRead)
                .ToListAsync();

            if (unreadNotifications.Any())
            {
                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    _unitOfWork.GetRepository<Notification>().UpdateAsync(notification);
                }
                await _unitOfWork.SaveChangesAsync();
            }
        }

    }
}
