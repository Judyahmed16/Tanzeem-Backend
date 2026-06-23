using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Mail;
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
using Tanzeem.Domain.Entities.Users;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Services.Abstractions.Notifications;
using Tanzeem.Shared.Dtos;
using Tanzeem.Shared.Dtos.Notifications;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Tanzeem.Services.Notifications
{
    public class NotificationService(IUnitOfWork _unitOfWork, ICurrentService _currentService, IConfiguration configuration) : INotificationService
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

                var alert = await GetAlertConfigurationAsync(branchId);
                await SendAlertEmailsAsync(notifications, alert);
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

            await SendAlertEmailAsync(notification, deadAlert);

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

            await SendAlertEmailAsync(notification, expiryAlert);
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

            await SendAlertEmailAsync(notification, Alert);
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

            await SendAlertEmailAsync(notification, Alert);
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

            await SendAlertEmailAsync(notification, Alert);
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

            await SendAlertEmailAsync(notification, Alert);
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

        private async Task<AlertConfigurations?> GetAlertConfigurationAsync(int branchId)
        {
            return await _unitOfWork.GetRepository<AlertConfigurations>().GetAllAsIQueryable()
                .FirstOrDefaultAsync(x => x.BranchId == branchId);
        }

        private async Task SendAlertEmailsAsync(IEnumerable<Notification> notifications, AlertConfigurations? alert)
        {
            foreach (var notification in notifications)
            {
                await SendAlertEmailAsync(notification, alert);
            }
        }

        private async Task SendAlertEmailAsync(Notification notification, AlertConfigurations? alert)
        {
            if (!ShouldSendEmail(notification, alert))
            {
                return;
            }

            var smtpHost = configuration["SmtpSettings:Host"];
            var smtpPortValue = configuration["SmtpSettings:Port"] ?? "587";
            var smtpEmail = configuration["SmtpSettings:Email"];
            var smtpPassword = configuration["SmtpSettings:Password"];
            var displayName = configuration["SmtpSettings:DisplayName"] ?? "Tanzeem";

            if (string.IsNullOrWhiteSpace(smtpHost)
                || string.IsNullOrWhiteSpace(smtpEmail)
                || string.IsNullOrWhiteSpace(smtpPassword)
                || !int.TryParse(smtpPortValue, out var smtpPort))
            {
                return;
            }

            var recipients = await GetAlertEmailRecipientsAsync(notification.BranchId);
            if (recipients.Count == 0)
            {
                return;
            }

            try
            {
                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(smtpEmail, smtpPassword)
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpEmail, displayName),
                    Subject = $"Tanzeem alert: {notification.Title}",
                    Body = BuildAlertEmailBody(notification),
                    IsBodyHtml = true
                };

                foreach (var recipient in recipients)
                {
                    mailMessage.Bcc.Add(recipient);
                }

                await client.SendMailAsync(mailMessage);
            }
            catch
            {
                // Alert emails are a secondary channel; keep the inventory/order action successful.
            }
        }

        private static bool ShouldSendEmail(Notification notification, AlertConfigurations? alert)
        {
            if (alert is null || !alert.IsActive_EmailNotifiation)
            {
                return false;
            }

            return notification.Type switch
            {
                NotificationType.LowStockAlert => alert.IsActive_LowAlert,
                NotificationType.OutOfStock => alert.IsActive_OutAlert,
                NotificationType.ExpiryAlert => alert.IsActive_ExpiryAlert,
                NotificationType.DeadStockAlert => alert.IsActive_DeadAlert,
                NotificationType.OrderUpdate => alert.IsActive_NewOrderAlert || alert.IsActive_OrderUpdateAlert,
                _ => true
            };
        }

        private async Task<IReadOnlyCollection<string>> GetAlertEmailRecipientsAsync(int branchId)
        {
            var recipients = await _unitOfWork.GetRepository<BranchUserRelationship>().GetAllAsIQueryable()
                .IgnoreQueryFilters()
                .Where(relation => relation.BranchId == branchId
                    && relation.User.Status == UserStatus.Active
                    && !string.IsNullOrWhiteSpace(relation.User.Email))
                .Select(relation => relation.User.Email)
                .Distinct()
                .ToListAsync();

            if (recipients.Count > 0)
            {
                return recipients;
            }

            var branchEmail = await _unitOfWork.GetRepository<Branch>().GetAllAsIQueryable()
                .IgnoreQueryFilters()
                .Where(branch => branch.Id == branchId && !string.IsNullOrWhiteSpace(branch.Email))
                .Select(branch => branch.Email)
                .FirstOrDefaultAsync();

            return string.IsNullOrWhiteSpace(branchEmail)
                ? Array.Empty<string>()
                : new[] { branchEmail };
        }

        private static string BuildAlertEmailBody(Notification notification)
        {
            var title = WebUtility.HtmlEncode(notification.Title);
            var message = WebUtility.HtmlEncode(notification.Message);
            var createdAt = notification.CreatedAt.ToString("yyyy-MM-dd HH:mm 'UTC'");

            return $"""
                <!doctype html>
                <html>
                <body style="margin:0;background:#f6f8f7;font-family:Arial,Helvetica,sans-serif;color:#17211d;">
                    <div style="max-width:640px;margin:0 auto;padding:32px 20px;">
                        <div style="background:#ffffff;border:1px solid #dfe9e4;border-radius:16px;overflow:hidden;">
                            <div style="background:#0f8f5f;padding:24px 28px;color:#ffffff;">
                                <div style="font-size:13px;font-weight:700;letter-spacing:.08em;text-transform:uppercase;">Tanzeem alert</div>
                                <h1 style="font-size:24px;line-height:1.25;margin:10px 0 0;">{title}</h1>
                            </div>
                            <div style="padding:28px;">
                                <p style="font-size:17px;line-height:1.6;margin:0 0 18px;">{message}</p>
                                <p style="font-size:13px;color:#68766f;margin:0;">Created {createdAt}</p>
                            </div>
                        </div>
                    </div>
                </body>
                </html>
                """;
        }

    }
}
