using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Products;

namespace Tanzeem.Persistence.Data.Configurations.ProductConfigurations {
    public class ProductConfiguration : IEntityTypeConfiguration<Product> {
        public void Configure(EntityTypeBuilder<Product> builder) {
        
            
            builder.Property(x => x.Name)
                .HasMaxLength(256);

            builder.Property(x => x.SKU)
                .HasMaxLength(256);

            builder.Property(x => x.CostPrice)
                .HasColumnType("decimal(18,2)");
            
            builder.Property(x => x.SellingPrice)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.ExpiryDate);

            builder.Property(x => x.Barcode)
                .HasMaxLength(256);
            
            builder.Property(x => x.Description)
                .HasMaxLength(512);

            builder.Property(x => x.ReorderLevel)
                .HasColumnType("int");

            builder.Property(x => x.Status)
                .HasMaxLength(256);
            


            builder.HasOne(p => p.Company)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.TransactionItems)
                .WithOne(ti => ti.Product)
                .HasForeignKey(ti => ti.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(p => p.Inventories)
                .WithOne(i => i.Product)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
             
            builder.HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

        }

    }
}
