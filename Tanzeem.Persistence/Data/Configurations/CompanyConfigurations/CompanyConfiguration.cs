using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Companies;

namespace Tanzeem.Persistence.Data.Configurations.CompanyConfigurations {
    public class CompanyConfiguration : IEntityTypeConfiguration<Company> {
        public void Configure(EntityTypeBuilder<Company> builder) {


            builder.Property(x => x.Field)
                .HasMaxLength(256);

            builder.Property(x => x.Name)
                .HasMaxLength(256);

            builder.Property(x => x.Email)
                .HasMaxLength(256);

            builder.Property(x => x.Phone)
                .HasMaxLength(20);

            builder.Property(x => x.IsActive)
                .HasDefaultValue(true);

            builder.Property(x => x.StripeCustomerId)
                .HasMaxLength(450);

            builder.HasIndex(x => x.StripeCustomerId)
                .IsUnique()
                .HasFilter("[StripeCustomerId] IS NOT NULL");


            builder.HasMany(c => c.Branches)
                .WithOne(b => b.Company)
                .HasForeignKey(b => b.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.Products)
                .WithOne(p => p.Company)
                .HasForeignKey(p => p.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.Users)
                .WithOne(u => u.Company)
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

        }

    }
}
