using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Products;

namespace Tanzeem.Domain.Entities.AIDemand
{
    public class DemandForecast
    {
        public int Id { get; set; }
        public bool DemandOccurs { get; set; }

        [Precision(18, 2)]
        public decimal PredictedUnits { get; set; }
        public string Segment {  get; set; }

        [Precision(18, 4)]
        public decimal Confidence { get; set; }
        public DateTime ForecastDate { get; set; }
        public DateTime LastUpdated { get; set; }
        public int ProductId { get; set; }
        public int BranchId { get; set; }
        public Product Product { get; set; }
        public Branch Branch { get; set; }
    }
}
