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
    public class DeliveryIssuesConfiguration : IEntityTypeConfiguration<DeliveryIssue>
    {
        public void Configure(EntityTypeBuilder<DeliveryIssue> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.RecieveDate).HasColumnName("date");
            builder.Property(o => o.DeliveryIssueNumber).HasDefaultValue("Old-Record");

            builder.HasOne(d => d.Order)
               .WithOne(o => o.DeliveryIssue)
               .HasForeignKey<DeliveryIssue>(d => d.OrderId)
               .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Supplier).
                WithMany(x => x.DeliveryIssues)
                .HasForeignKey(d => d.SupplierId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.Branch)
                .WithMany(x => x.DeliveryIssues)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            
        }
    }
}
