using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.AIDemand;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Orders;
using Tanzeem.Domain.Entities.Settings;
using Tanzeem.Domain.Entities.Transactions;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.AI;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Shared.Dtos;
using Tanzeem.Shared.Dtos.DemandForecast;

public class DemandForecastingService(IUnitOfWork _unitOfWork, IHttpClientFactory _httpClientFactory,
    ICurrentService _currentService, IConfiguration _configuration, ILogger<DemandForecastingService> _logger) : IDemandForecastingService
{
    public async Task<PaginationResponseDto<AIDemandForecastResponseDto>> GetAllPredictionsAsync(int page, int pageSize)
    {
        int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 
        
        //int branchId = 1;

        if (page <= 0) page = 1;

        const int maxPageSize = 20;

        if (pageSize > maxPageSize) pageSize = maxPageSize;

        var predictions = _unitOfWork.GetRepository<DemandForecast>().GetAllAsIQueryable()
            .Where(x => x.BranchId == branchId);

        var totalCount = await predictions.CountAsync();

        var predictionRows = await predictions
            .Include(x => x.Product)
            .ToListAsync();

        var mappedData = predictionRows
            .OrderByDescending(x => x.PredictedUnits)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new AIDemandForecastResponseDto
            {
                ProductId = p.ProductId,
                ProductName = p.Product.Name,
                SKU = p.Product.SKU,
                DemandOccurs = p.DemandOccurs,
                PredictedUnits = (int)p.PredictedUnits,
                Segment = p.Segment,
                Confidence = (double)p.Confidence,
            }).ToList();

        return new PaginationResponseDto<AIDemandForecastResponseDto>
        {
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Data = mappedData
        };
    }

    public async Task<IEnumerable<TopCategoriesByForecastDto>> GetTopCategoriesByForecast()
    {
        //int branchId = 1;
        int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 

        var forecasts = await _unitOfWork.GetRepository<DemandForecast>().GetAllAsIQueryable()
            .Where(x => x.BranchId == branchId)
            .Include(x => x.Product)
            .ThenInclude(x => x.Category)
            .ToListAsync();

        var topCategories = forecasts
            .GroupBy(x => new { CategoryId = x.Product.CategoryId, CategoryName = x.Product.Category.Name })
            .Select(g => new TopCategoriesByForecastDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.CategoryName ?? "Uncategorized",
                CategoryCount = (int)g.Sum(x => x.PredictedUnits)
            })
            .OrderByDescending(c => c.CategoryCount)
            .Take(10)
            .ToList();

        return topCategories;
    }

    public async Task<DemandDashboardDto> GetCounts()
    {
        int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 
        
        //int branchId = 1;
        
        var demandItems = _unitOfWork.GetRepository<DemandForecast>().GetAllAsIQueryable()
            .Where(x => x.BranchId == branchId);

        var TotalProductForecasted = await demandItems.CountAsync();

        var HighDemandItems = await demandItems.Where( x => x.Segment.ToLower() == "high").CountAsync();

        var averageConfidence = await demandItems.AverageAsync(f => (double?)f.Confidence) ?? 0;
        var confidencePercentage = Math.Round(averageConfidence * 100);

        var branchBatchTotals = _unitOfWork.GetRepository<InventoryBatch>().GetAllAsIQueryable()
            .Where(batch => batch.BranchId == branchId)
            .GroupBy(batch => batch.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Quantity = g.Sum(batch => batch.Quantity)
            });

        var itemsNeedRestock = await demandItems
            .GroupJoin(
                branchBatchTotals,
                forecast => forecast.ProductId,
                batchTotal => batchTotal.ProductId,
                (forecast, batchTotals) => new { forecast, batchQuantity = batchTotals.Select(x => x.Quantity).FirstOrDefault() })
            .Where(x => x.batchQuantity <= x.forecast.PredictedUnits)
            .CountAsync();
        return new DemandDashboardDto
        {
            TotalProductForecasted = TotalProductForecasted,
            HighDemandItems = HighDemandItems,
            AverageForecastConfidence = confidencePercentage,
            ItemsNeedRestock = itemsNeedRestock
        };
    }


    private class ProductSaleData
    {
        public int ProductId { get; set; }
        public DateTime Date { get; set; }
        public int Quantity { get; set; }
    }

    public async Task UpdateAllForecastsAsync()
    {
        var settings = await _unitOfWork.GetRepository<AIConfigurations>().GetAllAsIQueryable()
        .ToDictionaryAsync(s => s.BranchId, s => s.DemandForecasting);
        
        var allBranchIds = await _unitOfWork.GetRepository<Branch>()
        .GetAllAsIQueryable()
        .Select(b => b.Id)
        .ToListAsync();

        _logger.LogInformation(
            "Starting demand forecast refresh for {BranchCount} branches with {SettingsCount} AI settings.",
            allBranchIds.Count,
            settings.Count);

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30).Date;
        var todayDate = DateTime.UtcNow.Date;

        foreach (var branchId in allBranchIds)
        {
            bool isForecastingEnabled = settings.TryGetValue(branchId, out bool isEnabled) && isEnabled;

            if (!isForecastingEnabled)
            {
                continue;
            }

            var rawSales = await _unitOfWork.GetRepository<TransactionItem>()
                .GetAllAsIQueryable()
                .Where(ti => ti.Transaction.BranchId == branchId
                          && ti.Transaction.Type == TransactionType.Out
                          && ti.Transaction.CreatedAt >= thirtyDaysAgo)
                .Select(ti => new ProductSaleData
                {
                    ProductId = ti.ProductId,
                    Date = ti.Transaction.CreatedAt.Date,
                    Quantity = ti.QuantityOfTransactedItem
                })
                .ToListAsync();

            var todayOrdersRaw = await _unitOfWork.GetRepository<OrderItem>()
                .GetAllAsIQueryable()
                .Where(oi => oi.Order.BranchId == branchId && oi.Order.OrderDate.Date == todayDate)
                .Select(oi => new
                {
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity
                })
                .ToListAsync();

            Dictionary<int, int> ordersTodayByProduct = todayOrdersRaw
                .GroupBy(x => x.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            var inventories = await _unitOfWork.GetRepository<Inventory>()
                .GetAllAsIQueryable()
                .IgnoreQueryFilters()
                .Include(i => i.Product)
                .Where(i => i.BranchId == branchId)
                .ToListAsync();
            var branchBatchTotals = await GetBranchBatchTotalsAsync(branchId);

            double overallStoreAvg = rawSales.Any() ? rawSales.Average(x => x.Quantity) : 0;

            var requestBatch = BuildFlaskRequests(inventories, rawSales, ordersTodayByProduct, branchId, overallStoreAvg, thirtyDaysAgo, branchBatchTotals);

            foreach (var requestItem in requestBatch)
            {
                var aiPrediction = await CallFlaskApiAsync(requestItem);

                if (aiPrediction != null)
                {
                    int actualProductId = int.Parse(requestItem.ProductId.Replace("P_", ""));
                    // FIX: SaveChangesAsync is called inside ApplyUpsertToDatabase now
                    await ApplyUpsertToDatabase(actualProductId, branchId, aiPrediction);
                }
            }
            // FIX: No SaveChangesAsync here anymore — it's handled per-record inside ApplyUpsertToDatabase
        }
    }

    public async Task UpdateForecastForBranchAsync(int branchId)
    {
        var aiConfig = await _unitOfWork.GetRepository<AIConfigurations>()
            .GetAllAsIQueryable()
            .FirstOrDefaultAsync(c => c.BranchId == branchId);

        if (aiConfig == null || !aiConfig.DemandForecasting)
        {
            return;
        }

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30).Date;
        var todayDate = DateTime.UtcNow.Date;

        var rawSales = await _unitOfWork.GetRepository<TransactionItem>()
            .GetAllAsIQueryable()
            .Where(ti => ti.Transaction.BranchId == branchId
                      && ti.Transaction.Type == TransactionType.Out
                      && ti.Transaction.CreatedAt >= thirtyDaysAgo)
            .Select(ti => new ProductSaleData
            {
                ProductId = ti.ProductId,
                Date = ti.Transaction.CreatedAt.Date,
                Quantity = ti.QuantityOfTransactedItem
            })
            .ToListAsync();

        var todayOrdersRaw = await _unitOfWork.GetRepository<OrderItem>()
            .GetAllAsIQueryable()
            .Where(oi => oi.Order.BranchId == branchId && oi.Order.OrderDate.Date == todayDate)
            .Select(oi => new
            {
                ProductId = oi.ProductId,
                Quantity = oi.Quantity
            })
            .ToListAsync();

        Dictionary<int, int> ordersTodayByProduct = todayOrdersRaw
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        var inventories = await _unitOfWork.GetRepository<Inventory>()
            .GetAllAsIQueryable()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(i => i.Product)
            .Where(i => i.BranchId == branchId)
            .ToListAsync();
        var branchBatchTotals = await GetBranchBatchTotalsAsync(branchId);

        double overallStoreAvg = rawSales.Any() ? rawSales.Average(x => x.Quantity) : 0;

        var requestBatch = BuildFlaskRequests(inventories, rawSales, ordersTodayByProduct, branchId, overallStoreAvg, thirtyDaysAgo, branchBatchTotals);

        foreach (var requestItem in requestBatch)
        {
            var aiPrediction = await CallFlaskApiAsync(requestItem);

            if (aiPrediction != null)
            {
                int actualProductId = int.Parse(requestItem.ProductId.Replace("P_", ""));
                // FIX: SaveChangesAsync is called inside ApplyUpsertToDatabase now
                await ApplyUpsertToDatabase(actualProductId, branchId, aiPrediction);
            }
        }
        // FIX: No SaveChangesAsync here anymore — it's handled per-record inside ApplyUpsertToDatabase
    }

    private async Task<AIDemandForecastResponseDto?> CallFlaskApiAsync(AIDemandForecastRequestDto requestItem)
    {
        try
        {
            string apiUrl = _configuration["AIModels:ForecastApiUrl"] ?? throw new InvalidOperationException("API URL is missing in appsettings.json!");

            _logger.LogInformation(
                "Requesting demand forecast for {BranchId}/{ProductId}.",
                requestItem.BranchId,
                requestItem.ProductId);

            var _httpClient = _httpClientFactory.CreateClient(nameof(DemandForecastingService));
            var response = await _httpClient.PostAsJsonAsync(apiUrl, requestItem);

            if (response.IsSuccessStatusCode)
            {
                string rawJson = await response.Content.ReadAsStringAsync();

                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return System.Text.Json.JsonSerializer.Deserialize<AIDemandForecastResponseDto>(rawJson, options);
                }
                catch (System.Text.Json.JsonException ex)
                {
                    _logger.LogWarning(ex, "Demand forecast API returned invalid JSON for {BranchId}/{ProductId}.", requestItem.BranchId, requestItem.ProductId);
                    return null;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "Demand forecast API failed for {BranchId}/{ProductId} with status {StatusCode}: {Error}",
                    requestItem.BranchId,
                    requestItem.ProductId,
                    response.StatusCode,
                    errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Demand forecast API call failed for {BranchId}/{ProductId}.", requestItem.BranchId, requestItem.ProductId);
            return null;
        }
    }

    // FIX: Added AsNoTracking() to the query + SaveChangesAsync() called here per record
    // This prevents EF from accumulating multiple tracked DemandForecast entities
    // that share a ProductId index, which caused the circular dependency exception.
    private async Task ApplyUpsertToDatabase(int productId, int branchId, AIDemandForecastResponseDto prediction)
    {
        var targetForecastDate = DateTime.UtcNow.AddDays(1).Date;
        int predictedUnits = (int)Math.Round((decimal)prediction.PredictedUnits, MidpointRounding.AwayFromZero);

        // FIX: AsNoTracking() — we don't want EF to track this entity long-term.
        // We manually attach it via UpdateAsync right after, so tracking here causes the conflict.
        var existingForecast = await _unitOfWork.GetRepository<DemandForecast>()
            .GetAllAsIQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.BranchId == branchId
                                   && f.ProductId == productId);
                                   //&& f.ForecastDate == targetForecastDate);

        if (existingForecast != null)
        {
            existingForecast.PredictedUnits = predictedUnits;
            existingForecast.DemandOccurs = prediction.DemandOccurs;
            existingForecast.Segment = prediction.Segment;
            existingForecast.Confidence = (decimal)prediction.Confidence;
            existingForecast.ForecastDate = targetForecastDate;
            existingForecast.LastUpdated = DateTime.UtcNow;

            _unitOfWork.GetRepository<DemandForecast>().UpdateAsync(existingForecast);
        }
        else
        {
            var newForecast = new DemandForecast
            {
                ProductId = productId,
                BranchId = branchId,
                PredictedUnits = predictedUnits,
                DemandOccurs = prediction.DemandOccurs,
                Segment = prediction.Segment,
                Confidence = (decimal)prediction.Confidence,
                ForecastDate = targetForecastDate,
                LastUpdated = DateTime.UtcNow
            };
            await _unitOfWork.GetRepository<DemandForecast>().AddAsync(newForecast);
        }

        // FIX: Save immediately after each record so the ChangeTracker stays clean.
        // Previously, all records were saved in one batch, which caused EF to detect
        // a circular dependency between Modified entities sharing the ProductId index.
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<Dictionary<int, int>> GetBranchBatchTotalsAsync(int branchId)
        => await _unitOfWork.GetRepository<InventoryBatch>()
            .GetAllAsIQueryable()
            .IgnoreQueryFilters()
            .Where(batch => batch.BranchId == branchId)
            .GroupBy(batch => batch.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Quantity = g.Sum(batch => batch.Quantity)
            })
            .ToDictionaryAsync(x => x.ProductId, x => x.Quantity);

    private List<AIDemandForecastRequestDto> BuildFlaskRequests(List<Inventory> inventories, List<ProductSaleData> rawSales, Dictionary<int, int> ordersTodayByProduct, int branchId, double overallStoreAvg, DateTime thirtyDaysAgo, Dictionary<int, int> branchBatchTotals)
    {
        var batch = new List<AIDemandForecastRequestDto>();

        var salesByProduct = rawSales.ToLookup(x => x.ProductId);
        var targetForecastDate = DateTime.UtcNow.AddDays(1);
        var dayOfWeek = targetForecastDate.DayOfWeek;
        int isHoliday = (dayOfWeek == DayOfWeek.Thursday || dayOfWeek == DayOfWeek.Friday || dayOfWeek == DayOfWeek.Saturday) ? 1 : 0;

        int historyDays = (DateTime.UtcNow.Date - thirtyDaysAgo).Days;

        foreach (var inv in inventories)
        {
            var productSales = salesByProduct[inv.ProductId].ToList();
            List<DailyHistoryDto> history = new();
            List<int> dailyUnits = new();

            for (int i = historyDays; i >= 1; i--)
            {
                var historyDate = DateTime.UtcNow.AddDays(-i).Date;
                var unitsSold = productSales.Where(x => x.Date == historyDate).Sum(x => x.Quantity);

                history.Add(new DailyHistoryDto { Date = historyDate.ToString("yyyy-MM-dd"), UnitsSold = unitsSold });
                dailyUnits.Add(unitsSold);
            }

            var todayUnitsOrdered = ordersTodayByProduct.TryGetValue(inv.ProductId, out int quantity) ? quantity : 0;
            var inventoryLevel = branchBatchTotals.GetValueOrDefault(inv.ProductId, 0);

            batch.Add(new AIDemandForecastRequestDto
            {
                BranchId = $"STORE_{branchId:D3}",
                ProductId = $"P_{inv.ProductId:D3}",
                Date = targetForecastDate.ToString("yyyy-MM-dd"),
                Price = inv.Product.SellingPrice,
                Discount = 0,
                HolidayPromotion = isHoliday,
                InventoryLevel = inventoryLevel,
                UnitsOrdered = todayUnitsOrdered,
                History = history,
                ProductStats = new ProductStatsDto
                {
                    Mean = Math.Round(dailyUnits.Average(), 2),
                    Max = dailyUnits.Max(),
                    Min = dailyUnits.Min(),
                    Std = Math.Round(CalculateStdDev(dailyUnits), 2),
                    Median = CalculateMedian(dailyUnits)
                },
                StoreAvg = Math.Round(overallStoreAvg, 2)
            });
        }
        return batch;
    }

    #region Helpers
    private double CalculateStdDev(IEnumerable<int> values)
    {
        int count = values.Count();
        if (count <= 1) return 0;

        double avg = values.Average();
        double sum = values.Sum(d => Math.Pow(d - avg, 2));
        return Math.Sqrt(sum / (count - 1));
    }

    private double CalculateMedian(IEnumerable<int> values)
    {
        var sorted = values.OrderBy(n => n).ToArray();
        if (sorted.Length == 0) return 0;
        int mid = sorted.Length / 2;
        return (sorted.Length % 2 != 0) ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2.0;
    }
    #endregion
}
