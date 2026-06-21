using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Products;

namespace Tanzeem.Persistence.Data.Configurations.ProductConfigurations {
    public class CategoryConfiguration : IEntityTypeConfiguration<Category> {
        public void Configure(EntityTypeBuilder<Category> builder) {

            builder.Property(x => x.Name)
                .HasMaxLength(256);
            
            builder.HasOne(p => p.Company)
               .WithMany(c => c.Categories)
               .HasForeignKey(p => p.CompanyId)
               .OnDelete(DeleteBehavior.Restrict);
        }


    }
}
