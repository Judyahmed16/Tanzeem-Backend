using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tanzeem.Shared.Dtos.DemandForecast
{
    public class AIDemandForecastResponseDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string SKU { get; set; }
        [JsonPropertyName("demand_occurs")] public bool DemandOccurs { get; set; }
        [JsonPropertyName("predicted_units")] public double PredictedUnits { get; set; }
        [JsonPropertyName("segment")] public string Segment { get; set; } = string.Empty;
        [JsonPropertyName("confidence")] public double Confidence { get; set; }

    }
}
