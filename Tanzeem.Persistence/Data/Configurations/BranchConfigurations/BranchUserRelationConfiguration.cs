using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Branches;

namespace Tanzeem.Persistence.Data.Configurations.BranchConfigurations {
    public class BranchUserRelationConfiguration : IEntityTypeConfiguration<BranchUserRelationship> {
        public void Configure(EntityTypeBuilder<BranchUserRelationship> builder) {

            builder.HasIndex(x => new {
                x.UserId,
                x.BranchId,
            }).IsUnique();

            builder.HasIndex(x => x.UserId)
                .IsUnique()
                .HasFilter("[IsPrimary] = 1");

            builder.Property(x => x.IsPrimary);

            builder.HasOne(x => x.Branch)
                .WithMany(x => x.BURelations)
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany(x => x.BURelations)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
