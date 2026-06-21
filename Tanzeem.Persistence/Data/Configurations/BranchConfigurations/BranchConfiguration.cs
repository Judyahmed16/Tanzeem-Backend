using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Branches;

namespace Tanzeem.Persistence.Data.Configurations.BranchConfigurations {
    public class BranchConfiguration : IEntityTypeConfiguration<Branch> {
        public void Configure(EntityTypeBuilder<Branch> builder) {
            
            
            builder.Property(b => b.Name)
                .IsRequired().HasMaxLength(256);

            builder.Property(b => b.Location)
                .HasMaxLength(256);

            builder.Property(b => b.PhoneNumber)
                .HasMaxLength(20);

            builder.Property(b => b.Email)
                .HasMaxLength(256);

            builder.Property(b => b.CreatedAt);



            builder.HasOne(b => b.Company)
                .WithMany(c => c.Branches)
                .HasForeignKey(b => b.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(b => b.Transactions)
                .WithOne(t => t.Branch)
                .HasForeignKey(t => t.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(b => b.Inventories)
                .WithOne(i => i.Branch)
                .HasForeignKey(i => i.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.BURelations)
                .WithOne(x => x.Branch)
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Cascade);

        }

    }
}
