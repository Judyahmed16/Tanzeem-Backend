using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Shared.Dtos.DemandForecast
{
    public class DemandDashboardDto
    {
        public int TotalProductForecasted { get; set; }
        public int ItemsNeedRestock { get; set; }
        public int HighDemandItems { get; set; }
        public double AverageForecastConfidence { get; set; }

    }
}
