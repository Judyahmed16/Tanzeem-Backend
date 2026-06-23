using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Products;
using Tanzeem.Services.Abstractions.Current;

namespace Tanzeem.Services.Products {
    public class ProductHelperService(IUnitOfWork _unitOfWork,
        ICurrentService currentService) {

        public async Task<IEnumerable<Product>> GetAllProducts(int? sortId, int? filterId, string? searchQuery) {

            var branchId = currentService.BranchId
                ?? throw new UnauthorizedAccessException("BranchId not found");

            IQueryable<Product> query = _unitOfWork.GetRepository<Product>()
                .GetAllAsIQueryable()
                .Where(p => p.CompanyId == currentService.CompanyId
                    && p.Inventories.Any(i => i.BranchId == branchId));

            if (filterId.HasValue)
                query = query.Where(p => p.CategoryId == filterId);

            // Search across Name, SKU, and Barcode
            if (!string.IsNullOrWhiteSpace(searchQuery)) {
                var term = searchQuery.Trim().ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(term) ||
                    p.SKU.ToLower().Contains(term) ||
                    p.Barcode.ToLower().Contains(term) ||
                    p.Inventories.Any(i => i.BranchId == branchId
                        && i.ProductNumber != null
                        && i.ProductNumber.ToLower().Contains(term))
                );
            }

            query = sortId switch {
                1 => query.OrderBy(p => p.Name),
                2 => query.OrderBy(p => p.SellingPrice),
                3 => query.OrderBy(p => p.InventoryBatches
                             .Where(i => i.BranchId == branchId)
                             .Sum(i => i.Quantity)),
                null => query.OrderBy(p => p.Id),
                _ => throw new ArgumentException($"Invalid sort option: {sortId}")
            };

            return await query
                .Include(p => p.Category)
                .Include(p => p.Inventories)
                .Include(p => p.InventoryBatches)
                .AsSplitQuery()
                .ToListAsync();
        }

    }
}
