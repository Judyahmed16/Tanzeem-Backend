using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.AIDemand;
using Tanzeem.Domain.Entities.DeliveryIssues;


namespace Tanzeem.Persistence.Data.Configurations.AIDemandConfigurations
{
    public class DemandForecastingConfiguration : IEntityTypeConfiguration<DemandForecast>
    {
        public void Configure(EntityTypeBuilder<DemandForecast> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Branch)
               .WithMany(x => x.DemandForecasts)
               .HasForeignKey(d => d.BranchId)
               .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Product)
               .WithMany(x => x.Forecasts)
               .HasForeignKey(d => d.ProductId)
               .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
