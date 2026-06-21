using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Inventories;

namespace Tanzeem.Persistence.Data.Configurations.InventoryConfigurations {
    public class InventoryConfiguration : IEntityTypeConfiguration<Inventory> {
        public void Configure(EntityTypeBuilder<Inventory> builder) {
    
            builder.Property(x => x.Quantity)
                .HasColumnType("int");


            builder.HasOne(i => i.Product)
                .WithMany(p => p.Inventories)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(i => i.Branch)
                .WithMany(b => b.Inventories)
                .HasForeignKey(i => i.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

        }

    
    }
}
