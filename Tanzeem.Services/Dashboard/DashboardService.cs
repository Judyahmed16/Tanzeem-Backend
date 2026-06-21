using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Products;
using Tanzeem.Domain.Entities.Transactions;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.Alerts;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Services.Abstractions.Dashboard;
using Tanzeem.Shared.Dtos.Dashboard;

namespace Tanzeem.Services.Dashboard
{
    public class DashboardService(IUnitOfWork _unitOfWork, IAlertService _alertService, ICurrentService _currentService) : IDashboardService
    {
        private async Task<decimal> CalculateTotalStockValue()
        {
            //int branchId = 1;
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 

            var batchValues = await _unitOfWork.GetRepository<InventoryBatch>()
                .GetAllAsIQueryable()
                .Where(batch => batch.Quantity > 0 && batch.BranchId == branchId)
                .Select(batch => new { batch.Quantity, batch.CostPrice })
                .ToListAsync();

            var totalValue = batchValues.Sum(batch => batch.Quantity * batch.CostPrice);

            return totalValue;
        }
        public async Task<DashboardBoxesDto> GetDashboardSummary()
        {
            var deadAlerts = await _alertService.ShowDeadStockAlerts();
            int deadCount = deadAlerts.Count();

            var lowStockAlerts = await _alertService.ShowLowStockAlerts();
            int lowCount = lowStockAlerts.Count();

            var expiryAlerts = await _alertService.ShowExpiryAlerts();
            int expiryCount = expiryAlerts.Count();
            return new DashboardBoxesDto()
            {
                LowStockCount = lowCount,
                DeadStockCount = deadCount,
                NearExpiryCount = expiryCount,
                TotalStockValue = await CalculateTotalStockValue()
            };
        }

        public async Task<List<TopMovingItemsDto>> GetTopMovingItemsAsync()
        {
            //int branchId = 1;
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 
            
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var sixtyDaysAgo = DateTime.UtcNow.AddDays(-60);

            var transactionItems = await _unitOfWork.GetRepository<TransactionItem>()
                .GetAllAsIQueryable()
                .Where(ti => ti.Transaction.BranchId == branchId
                          && ti.Transaction.Type == TransactionType.Out
                          && ti.Transaction.SourceReason == TransactionSource.Selling 
                          && ti.Transaction.CreatedAt >= sixtyDaysAgo)
                .Select(ti => new
                {
                    ti.ProductId,
                    ti.Product.Name,
                    ti.Transaction.CreatedAt,
                    ti.QuantityOfTransactedItem,
                    ti.UnitPrice
                })
                .ToListAsync();

            var rawData = transactionItems
                .GroupBy(ti => new { ti.ProductId, ti.Name })
                .Select(g => new
                {
                    ItemName = g.Key.Name,

                    CurrentUnits = g.Sum(ti => ti.CreatedAt >= thirtyDaysAgo ? ti.QuantityOfTransactedItem : 0),
                    CurrentRevenue = g.Sum(ti => ti.CreatedAt >= thirtyDaysAgo ? ti.QuantityOfTransactedItem * ti.UnitPrice : 0),

                    PreviousUnits = g.Sum(ti => ti.CreatedAt < thirtyDaysAgo ? ti.QuantityOfTransactedItem : 0)
                })
                .Where(x => x.CurrentUnits > 0) 
                .OrderByDescending(x => x.CurrentUnits)
                .Take(10)
                .ToList();

            
            var result = rawData.Select(x => new TopMovingItemsDto
            {
                ItemName = x.ItemName,
                UnitsSold = x.CurrentUnits,
                Revenue = Math.Round(x.CurrentRevenue, 2),
                Trend = x.CurrentUnits >= x.PreviousUnits ? "Rising" : "Falling"
            }).ToList();

            return result;
        }

        public async Task<List<CategoryDistributionDto>> GetCategoryDistribution()
        {
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned");

            var rawDistribution = await _unitOfWork.GetRepository<Inventory>()
                .GetAllAsIQueryable()
                .Where(inv => inv.BranchId == branchId)
                .GroupBy(inv => new { inv.Product.Category.Id, inv.Product.Category.Name })
                .Select(g => new
                {
                    CategoryName = g.Key.Name,
                    TypesCount = g.Select(inv => inv.ProductId).Distinct().Count()
                })
                .OrderByDescending(x => x.TypesCount)
                .ToListAsync();

            var result = new List<CategoryDistributionDto>();
            var top4Categories = rawDistribution.Take(4).ToList();

            foreach (var item in top4Categories)
            {
                result.Add(new CategoryDistributionDto
                {
                    CategoryName = item.CategoryName,
                    Count = item.TypesCount 
                });
            }

            if (rawDistribution.Count > 4)
            {
                var othersCount = rawDistribution.Skip(4).Sum(x => x.TypesCount);
                result.Add(new CategoryDistributionDto
                {
                    CategoryName = "Others",
                    Count = othersCount
                });
            }

            return result;
        }

        public async Task<List<MonthlyMovementDto>> GetMonthlyStockMovementAsync()
        {
            //var branchId = 1;
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 

            var now = DateTime.UtcNow;
            var startOfCurrentMonth = new DateTime(now.Year, now.Month, 1);

            var last12Months = Enumerable.Range(0, 12)
                .Select(i => startOfCurrentMonth.AddMonths(-11 + i))
                .ToList();

            var startDate = last12Months.First();

            var rawData = await _unitOfWork.GetRepository<Transaction>()
                .GetAllAsIQueryable()
                .Where(t => t.BranchId == branchId && t.CreatedAt >= startDate)
                .GroupBy(t => new {
                    Year = t.CreatedAt.Year,
                    Month = t.CreatedAt.Month
                })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    InCount = g.Sum(t => t.Type == TransactionType.In ? t.TotalTransactedItems : 0),
                    OutCount = g.Sum(t => t.Type == TransactionType.Out ? t.TotalTransactedItems : 0)
                })
                .ToListAsync();

            var result = last12Months.Select(m =>
            {
                var dbRecord = rawData.FirstOrDefault(r => r.Year == m.Year && r.Month == m.Month);

                return new MonthlyMovementDto
                {
                    MonthName = m.ToString("MMM"),
                    StockIn = dbRecord != null ? dbRecord.InCount : 0,
                    StockOut = dbRecord != null ? dbRecord.OutCount : 0
                };
            }).ToList();

            return result;
        }

        public async Task<List<StockValueDto>> GetStockValueTrendAsync()
        {
            //var branchId = 1;
            int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 

            var now = DateTime.UtcNow;
            var startOfCurrentMonth = new DateTime(now.Year, now.Month, 1);
            var startDate12MonthsAgo = startOfCurrentMonth.AddMonths(-11);

            var previousItems = await _unitOfWork.GetRepository<TransactionItem>()
                .GetAllAsIQueryable()
                .Where(ti => ti.Transaction.BranchId == branchId && ti.Transaction.CreatedAt < startDate12MonthsAgo)
                .Select(ti => new
                {
                    ti.QuantityOfTransactedItem,
                    ti.UnitPrice,
                    ti.UnitCost,
                    ti.Transaction.Type
                })
                .ToListAsync();

            var initialStockValue = previousItems.Sum(ti =>
                    ti.Type == TransactionType.In
                        ? ti.QuantityOfTransactedItem * ti.UnitPrice
                        : -ti.QuantityOfTransactedItem * ti.UnitCost);

            var currentItems = await _unitOfWork.GetRepository<TransactionItem>()
                .GetAllAsIQueryable()
                .Where(ti => ti.Transaction.BranchId == branchId && ti.Transaction.CreatedAt >= startDate12MonthsAgo)
                .Select(ti => new
                {
                    ti.QuantityOfTransactedItem,
                    ti.UnitPrice,
                    ti.UnitCost,
                    ti.Transaction.Type,
                    ti.Transaction.CreatedAt
                })
                .ToListAsync();

            var monthlyNetChanges = currentItems
                .GroupBy(ti => new { ti.CreatedAt.Year, ti.CreatedAt.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    NetChange = g.Sum(ti =>
                        ti.Type == TransactionType.In
                            ? ti.QuantityOfTransactedItem * ti.UnitPrice
                            : -ti.QuantityOfTransactedItem * ti.UnitCost)
                })
                .ToList();

            var result = new List<StockValueDto>();
            var runningTotalValue = initialStockValue;

            var last12Months = Enumerable.Range(0, 12).Select(i => startDate12MonthsAgo.AddMonths(i)).ToList();

            foreach (var monthDate in last12Months)
            {
                var monthChange = monthlyNetChanges
                    .FirstOrDefault(m => m.Year == monthDate.Year && m.Month == monthDate.Month)?.NetChange ?? 0;

                runningTotalValue += monthChange;

                result.Add(new StockValueDto
                {
                    Month = monthDate.ToString("MMM"),
                    TotalValue = Math.Max(0, runningTotalValue)
                });
            }

            return result;
        }
    }
}
