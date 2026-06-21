using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tanzeem.Shared.Dtos.DemandForecast
{
    public class AIDemandForecastRequestDto
    {
        [JsonPropertyName("store_id")] public string BranchId { get; set; } = string.Empty;
        [JsonPropertyName("product_id")] public string ProductId { get; set; } = string.Empty;
        [JsonPropertyName("date")] public string Date { get; set; } = string.Empty;
        [JsonPropertyName("price")] public decimal Price { get; set; }
        [JsonPropertyName("discount")] public decimal Discount { get; set; }
        [JsonPropertyName("holiday_promotion")] public int HolidayPromotion { get; set; }
        [JsonPropertyName("inventory_level")] public int InventoryLevel { get; set; }
        [JsonPropertyName("units_ordered")] public int UnitsOrdered { get; set; }
        [JsonPropertyName("history")] public List<DailyHistoryDto> History { get; set; } = new();
        [JsonPropertyName("product_stats")] public ProductStatsDto ProductStats { get; set; } = new();
        [JsonPropertyName("store_avg")] public double StoreAvg { get; set; }

    }
    public class DailyHistoryDto

    {

        [JsonPropertyName("date")] public string Date { get; set; } = string.Empty;

        [JsonPropertyName("units_sold")] public int UnitsSold { get; set; }

    }



    public class ProductStatsDto

    {

        [JsonPropertyName("mean")] public double Mean { get; set; }

        [JsonPropertyName("max")] public double Max { get; set; }

        [JsonPropertyName("min")] public double Min { get; set; }

        [JsonPropertyName("std")] public double Std { get; set; }

        [JsonPropertyName("median")] public double Median { get; set; }

    }
 
}
