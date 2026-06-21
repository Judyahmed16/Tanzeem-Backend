using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Orders;
using Tanzeem.Domain.Entities.Products;
using Tanzeem.Domain.Entities.Transactions;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Services.Abstractions.Notifications;
using Tanzeem.Services.Abstractions.Transactions;
using Tanzeem.Shared.Dtos.Transactions;

namespace Tanzeem.Services.Transactions {
    public class TransactionService(
        IUnitOfWork _unitOfWork,
        ICurrentService currentService,
        TransactionHelperService transactionHelper,
        INotificationService _notificationService)
        : ITransactionService {

        public async Task<TransactionDto> GetTransactionByIdAsync(int id) {

            var branchId = currentService.BranchId
                ?? throw new UnauthorizedAccessException("BranchId not found");

            // Single query — loads TransactionItems and their Products and the PerformedByUser
            var transaction = await _unitOfWork.GetRepository<Transaction>()
                .GetAllAsIQueryable()
                .Include(t => t.TransactionItems)
                    .ThenInclude(ti => ti.Product)
                        .ThenInclude(p => p.Category)
                .Include(t => t.TransactionItems)
                    .ThenInclude(ti => ti.Product)
                        .ThenInclude(p => p.InventoryBatches)
                .Include(t => t.PreformedByUser)
                .AsSplitQuery()
                .FirstOrDefaultAsync(t => t.Id == id && t.BranchId == branchId);

            if (transaction is null)
                throw new KeyNotFoundException($"Transaction with ID {id} not found.");

            return MapToTransactionDto(transaction, branchId);
        }

        public async Task<IEnumerable<TransactionDto>> GetAllTransactions(int? filterId, int? sortId, string? searchQuery) {

            var transactions = await transactionHelper.GetAllTransactions(sortId, filterId, searchQuery);

            return transactions.Select(t => MapToTransactionDto(t, currentService.BranchId ?? 0));
        }

        public async Task<int> CreateTransactionAsync(TransactionDto transactionDto) {

            var branchId = currentService.BranchId
                ?? throw new UnauthorizedAccessException("BranchId not found");

            var userId = currentService.UserId
                ?? throw new UnauthorizedAccessException("UserId not found");

            await using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
            try {

                #region Entities Loading

                // Load branch inventories with their products in one query
                var inventories = await _unitOfWork.GetRepository<Inventory>()
                    .GetAllAsIQueryable()
                    .AsTracking()
                    .Include(i => i.Product)
                    .Where(i => i.BranchId == branchId)
                    .ToListAsync();

                var inventoryBatches = await _unitOfWork.GetRepository<InventoryBatch>()
                    .GetAllAsIQueryable()
                    .AsTracking()
                    .Include(i => i.Product)
                    .Where(i => i.BranchId == branchId)
                    .ToListAsync();

                // Load all needed products in one shot by SKU
                var skus = transactionDto.TransactionItemDtos
                    .Select(x => x.Product.SKU)
                    .ToHashSet();

                var productsBySku = await _unitOfWork.GetRepository<Product>()
                    .GetAllAsIQueryable()
                    .AsTracking()          // ensure tracked instances
                    .Where(p => skus.Contains(p.SKU))
                    .ToDictionaryAsync(p => p.SKU);

                #endregion

                #region Mapping

                var transactionItems = transactionDto.TransactionItemDtos.Select(item => {
                    if (!productsBySku.TryGetValue(item.Product.SKU, out var product))
                        throw new KeyNotFoundException($"Product with SKU '{item.Product.SKU}' not found.");

                    return new TransactionItem {
                        QuantityOfTransactedItem = item.QuantityOfTransactedItem,
                        UnitPrice = item.UnitPrice,
                        UnitCost = item.UnitCost,
                        BatchNumber = item.BatchNumber,
                        Product = product,
                    };
                }).ToList();

                var transaction = new Transaction {
                    TransactionId = Guid.NewGuid().ToString(),
                    Type = transactionDto.Type,
                    CreatedAt = transactionDto.CreatedAt,
                    Status = transactionDto.Status,
                    Value = transactionItems.Sum(x => x.UnitPrice * x.QuantityOfTransactedItem),
                    TotalTransactedItems = transactionItems.Sum(x => x.QuantityOfTransactedItem),
                    SourceReason = transactionDto.SourceReason,
                    ReferenceNumber = transactionDto.ReferenceNumber,
                    Notes = transactionDto.Notes,
                    TransactionItems = transactionItems,
                    BranchId = branchId,
                    PerformedByUserId = userId
                };

                #endregion

                #region Update Inventory

                if (transactionDto.Type == TransactionType.In)
                    await InTransactionAsync(transactionDto.TransactionItemDtos, transactionItems, inventories, inventoryBatches);
                else if (transactionDto.Type == TransactionType.Out)
                    OutTransaction(transactionItems, inventories, inventoryBatches);

                #endregion

                await _unitOfWork.GetRepository<Transaction>().AddAsync(transaction);
                await _unitOfWork.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                await LowStockAlertAsync(transaction, transactionItems, inventories);

                return transaction.Id;
            }
            catch {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<int> CreateConfirmOrderTransactionAsync(Order order) {

            if (order is null)
                throw new ArgumentNullException(nameof(order), "Order cannot be null");

            var branchId = currentService.BranchId
                ?? throw new UnauthorizedAccessException("BranchId not found");

            var userId = currentService.UserId
                ?? throw new UnauthorizedAccessException("UserId not found");

            var receivedDate = order.RecievedDeliveryDate
                ?? throw new InvalidOperationException("Order delivery date is required to confirm a transaction");

            var transactionItems = order.Items.Select(orderItem => new TransactionItem {
                Product = orderItem.Product,
                QuantityOfTransactedItem = orderItem.Quantity,
                UnitPrice = orderItem.Price,
                UnitCost = orderItem.Price,
                ProductId = orderItem.ProductId,
                BatchNumber = string.Empty
            }).ToList();

            var transaction = new Transaction {
                BranchId = branchId,
                PerformedByUserId = userId,
                TransactionId = Guid.NewGuid().ToString(),
                CreatedAt = receivedDate,
                SourceReason = TransactionSource.Supplier,
                TransactionItems = transactionItems,
                TotalTransactedItems = transactionItems.Sum(item => item.QuantityOfTransactedItem),
                Type = TransactionType.In,
                Status = TransactionStatus.Completed,
                Value = transactionItems.Sum(item => item.UnitPrice * item.QuantityOfTransactedItem),
                Notes = order.Notes ?? "Order confirmed",
                ReferenceNumber = "--",
            };

            await _unitOfWork.GetRepository<Transaction>().AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            return transaction.Id;
        }

        #region Private Helpers

        private TransactionDto MapToTransactionDto(Transaction transaction, int branchId) {
            var itemDtos = transaction.TransactionItems.Select(ti => {
                var matchingBatch = ti.Product.InventoryBatches
                    .Where(b => b.BranchId == branchId && b.BatchNumber == ti.BatchNumber)
                    .OrderBy(b => b.ExpiryDate ?? DateTime.MaxValue)
                    .FirstOrDefault();

                return new TransactionItemDto {
                QuantityOfTransactedItem = ti.QuantityOfTransactedItem,
                UnitPrice = ti.UnitPrice,
                UnitCost = ti.UnitCost,
                    BatchNumber = ti.BatchNumber ?? string.Empty,
                    ExpiryDate = matchingBatch?.ExpiryDate,
                    Product = new Shared.Dtos.Products.ProductDto {
                    Name = ti.Product.Name,
                    SKU = ti.Product.SKU,
                    Category = ti.Product.Category?.Name ?? "Uncategorized",
                    CostPrice = matchingBatch?.CostPrice ?? ti.Product.CostPrice,
                    SellingPrice = ti.Product.SellingPrice,
                    ExpiryDate = matchingBatch?.ExpiryDate ?? ti.Product.ExpiryDate,
                    Barcode = ti.Product.Barcode,
                    Description = ti.Product.Description,
                    ReorderLevel = ti.Product.ReorderLevel,
                    Status = ti.Product.Status,
                    Stock = ti.Product.InventoryBatches
                        .Where(batch => batch.BranchId == branchId)
                        .Sum(batch => batch.Quantity)
                }
                };
            }).ToList();

            return new TransactionDto {
                Id = transaction.Id,
                TransactionId = transaction.TransactionId,
                Type = transaction.Type,
                CreatedAt = transaction.CreatedAt,
                Status = transaction.Status,
                Value = transaction.Value,
                TotalTransactedItems = transaction.TotalTransactedItems,
                SourceReason = transaction.SourceReason,
                ReferenceNumber = transaction.ReferenceNumber,
                Notes = transaction.Notes,
                PreformedBy = transaction.PreformedByUser?.Name ?? "-",
                TransactionItemDtos = itemDtos
            };
        }

        private async Task InTransactionAsync(
            List<TransactionItemDto> itemDtos,
            List<TransactionItem> transactionItems,
            List<Inventory> inventories,
            List<InventoryBatch> inventoryBatches) {
            foreach (var item in transactionItems) {
                var inventory = inventories.FirstOrDefault(inv => inv.ProductId == item.Product.Id)
                    ?? throw new KeyNotFoundException(
                        $"Inventory record not found for product '{item.Product.Name}' (SKU: {item.Product.SKU}).");

                item.UnitCost = item.UnitPrice;

                var itemDto = itemDtos.First(x => x.Product.SKU == item.Product.SKU);
                var batch = new InventoryBatch
                {
                    ProductId = item.Product.Id,
                    BranchId = inventory.BranchId,
                    BatchNumber = string.IsNullOrWhiteSpace(item.BatchNumber)
                        ? $"IN-{DateTime.UtcNow:yyyyMMddHHmmss}"
                        : item.BatchNumber.Trim(),
                    Quantity = item.QuantityOfTransactedItem,
                    ExpiryDate = itemDto.ExpiryDate ?? item.Product.ExpiryDate,
                    CostPrice = item.UnitPrice,
                    ReceivedAt = DateTime.UtcNow
                };
                await _unitOfWork.GetRepository<InventoryBatch>().AddAsync(batch);
                inventoryBatches.Add(batch);
                inventory.Quantity = inventoryBatches
                    .Where(existingBatch => existingBatch.ProductId == item.Product.Id)
                    .Sum(existingBatch => existingBatch.Quantity);
            }
        }

        private void OutTransaction(
            List<TransactionItem> transactionItems,
            List<Inventory> inventories,
            List<InventoryBatch> inventoryBatches) {
            foreach (var item in transactionItems) {
                var inventory = inventories.FirstOrDefault(inv => inv.ProductId == item.Product.Id)
                    ?? throw new KeyNotFoundException(
                        $"Inventory record not found for product '{item.Product.Name}' (SKU: {item.Product.SKU}).");

                var availableBatchQuantity = GetAvailableBatchQuantity(item, inventoryBatches);
                if (availableBatchQuantity < item.QuantityOfTransactedItem)
                    throw new InvalidOperationException(
                        $"Insufficient stock for '{item.Product.Name}'. Available: {availableBatchQuantity}, Requested: {item.QuantityOfTransactedItem}.");

                item.UnitCost = ConsumeBatches(item, inventoryBatches);
                inventory.Quantity = inventoryBatches
                    .Where(batch => batch.ProductId == item.Product.Id)
                    .Sum(batch => batch.Quantity);
            }
        }

        private static int GetAvailableBatchQuantity(TransactionItem item, List<InventoryBatch> inventoryBatches)
            => inventoryBatches
                .Where(batch => batch.ProductId == item.Product.Id && batch.Quantity > 0)
                .Where(batch => string.IsNullOrWhiteSpace(item.BatchNumber) || batch.BatchNumber == item.BatchNumber)
                .Sum(batch => batch.Quantity);

        private static decimal ConsumeBatches(TransactionItem item, List<InventoryBatch> inventoryBatches)
        {
            var remaining = item.QuantityOfTransactedItem;
            decimal consumedCost = 0;
            var candidateBatches = inventoryBatches
                .Where(batch => batch.ProductId == item.Product.Id && batch.Quantity > 0)
                .Where(batch => string.IsNullOrWhiteSpace(item.BatchNumber) || batch.BatchNumber == item.BatchNumber)
                .OrderBy(batch => batch.ExpiryDate ?? DateTime.MaxValue)
                .ThenBy(batch => batch.ReceivedAt)
                .ToList();

            if (!candidateBatches.Any())
                throw new InvalidOperationException($"No available batch stock found for '{item.Product.Name}'.");

            foreach (var batch in candidateBatches)
            {
                if (remaining <= 0) break;

                var consumed = Math.Min(batch.Quantity, remaining);
                batch.Quantity -= consumed;
                remaining -= consumed;
                consumedCost += consumed * batch.CostPrice;
            }

            if (remaining > 0)
                throw new InvalidOperationException(
                    $"Insufficient batch stock for '{item.Product.Name}'. Remaining shortage: {remaining}.");

            return item.QuantityOfTransactedItem <= 0
                ? 0
                : consumedCost / item.QuantityOfTransactedItem;
        }

        private async Task LowStockAlertAsync(
            Transaction transaction,
            List<TransactionItem> transactionItems,
            List<Inventory> inventories) {
            try {
                if (transaction.Type != TransactionType.Out) return;

                // inventories is already branch-scoped from CreateTransactionAsync — match by ProductId only
                var lowStockItems = transactionItems
                    .Where(item => {
                        var inventory = inventories.FirstOrDefault(x => x.ProductId == item.Product.Id);
                        return inventory != null
                            && (inventory.Quantity ?? 0) <= inventory.Product.ReorderLevel;
                    })
                    .ToList();

                if (lowStockItems.Any())
                    await _notificationService.CreateLowStockNotification(lowStockItems, inventories);
            }
            catch {
                // Never let a failed alert bubble up and roll back the transaction
                // TODO: plug into your logger here → _logger.LogError(ex, "LowStockAlert failed for TransactionId: {id}", transaction.Id)
            }
        }

        #endregion
    
    }
}
