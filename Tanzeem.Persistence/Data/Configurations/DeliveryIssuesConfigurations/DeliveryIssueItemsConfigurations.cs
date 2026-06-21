using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.DeliveryIssues;

namespace Tanzeem.Persistence.Data.Configurations.DeliveryIssuesConfigurations
{
    public class DeliveryIssueItemsConfiguration : IEntityTypeConfiguration<DeliveryIssueItem>
    {
        public void Configure(EntityTypeBuilder<DeliveryIssueItem> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.IssueType).HasConversion<string>();

            builder.HasOne(x => x.DeliveryIssue)
                .WithMany(x => x.DeliveryIssueItem)
                .HasForeignKey(x => x.DeliveryIssueId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.OrderItem)
                .WithMany(x => x.DeliveryIssueItems)
                .HasForeignKey(x => x.OrderItemId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
