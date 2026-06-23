using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Transactions;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.Current;

namespace Tanzeem.Services.Transactions {
    public class TransactionHelperService(
        IUnitOfWork _unitOfWork,
        ICurrentService currentService) {

        public async Task<IEnumerable<Transaction>> GetAllTransactions(int? sortId, int? filterId, string? searchQuery) {

            var branchId = currentService.BranchId
                ?? throw new UnauthorizedAccessException("BranchId not found");

            IQueryable<Transaction> query = _unitOfWork.GetRepository<Transaction>()
                .GetAllAsIQueryable()
                .Where(t => t.BranchId == branchId);

            if (filterId.HasValue)
                query = ApplyFilter(query, filterId.Value);

            if (!string.IsNullOrWhiteSpace(searchQuery)) {
                var term = searchQuery.Trim().ToLower();
                query = query.Where(t =>
                    t.TransactionId.ToLower().Contains(term) ||
                    (t.TransactionNumber != null && t.TransactionNumber.ToLower().Contains(term)) ||
                    (t.ReferenceNumber != null && t.ReferenceNumber.ToLower().Contains(term)) ||
                    (t.Notes != null && t.Notes.ToLower().Contains(term))
                );
            }

            query = ApplySort(query, sortId);

            return await query
                .Include(t => t.TransactionItems)
                    .ThenInclude(ti => ti.Product)
                        .ThenInclude(p => p.Category)
                .Include(t => t.TransactionItems)
                    .ThenInclude(ti => ti.Product)
                        .ThenInclude(p => p.Inventories)
                .Include(t => t.TransactionItems)
                    .ThenInclude(ti => ti.Product)
                        .ThenInclude(p => p.InventoryBatches)
                .Include(t => t.PreformedByUser)
                .AsSplitQuery()
                .ToListAsync();
        }

        private static IQueryable<Transaction> ApplyFilter(IQueryable<Transaction> query, int filterId) {
            return filterId switch {
                >= 1 and <= 3 => query.Where(t => t.Type == (TransactionType)filterId),
                >= 4 and <= 6 => query.Where(t => t.Status == (TransactionStatus)filterId),
                >= 7 and <= 12 => query.Where(t => t.SourceReason == (TransactionSource)filterId),
                _ => throw new ArgumentOutOfRangeException(nameof(filterId), "Invalid filter option")
            };
        }

        private static IQueryable<Transaction> ApplySort(IQueryable<Transaction> query, int? sortId) {
            return sortId switch {
                1 => query.OrderBy(t => t.CreatedAt),
                2 => query.OrderBy(t => t.Value),
                3 => query.OrderBy(t => t.TotalTransactedItems),
                null => query.OrderBy(t => t.Id),
                _ => throw new ArgumentException($"Invalid sort option: {sortId}")
            };
        }
    }
}
