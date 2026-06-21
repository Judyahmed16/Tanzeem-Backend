using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Subscriptions;

namespace Tanzeem.Persistence.Data.Configurations.SubscriptionConfigurations {
    public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription> {
        public void Configure(EntityTypeBuilder<Subscription> builder) {

            builder.Property(x => x.Plan);

            builder.HasIndex(s => s.StripeSubscriptionId)
                .IsUnique();

            builder.Property(x => x.Status)
                .HasMaxLength(32);

            builder.Property(x => x.StartedAt);

            builder.Property(x => x.ExpiresAt);

            builder.HasOne(x => x.Company)
                .WithOne(x => x.Subscription)
                .HasForeignKey<Subscription>(s => s.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
