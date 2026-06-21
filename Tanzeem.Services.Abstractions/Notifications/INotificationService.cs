using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Orders;
using Tanzeem.Domain.Entities.Transactions;
using Tanzeem.Domain.Enums;
using Tanzeem.Shared.Dtos;
using Tanzeem.Shared.Dtos.Notifications;

namespace Tanzeem.Services.Abstractions.Notifications
{
    public interface INotificationService
    {
        public Task<PaginationResponseDto<NotificationDto>> GetAllNotifications(int page, int pageSize);
        public Task<IEnumerable<int>> CreateLowStockNotification(List<TransactionItem> transactionItems,List<Inventory> inventories);
        public Task<bool> MarkAsReadAsync(int notificationId);
        public Task CreateOrderDeliveredNotification(Order order);
        public Task MarkAllAsReadAsync();
        public Task CreateNotification();
        public Task CreateNewOrderNotification(Order order);

    }
}
