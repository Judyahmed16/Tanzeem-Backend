using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tanzeem.Domain.Entities.Inventories;

namespace Tanzeem.Persistence.Data.Configurations.InventoryConfigurations;

public class InventoryBatchConfiguration : IEntityTypeConfiguration<InventoryBatch>
{
    public void Configure(EntityTypeBuilder<InventoryBatch> builder)
    {
        builder.Property(x => x.BatchNumber)
            .HasMaxLength(100);

        builder.Property(x => x.Quantity)
            .HasColumnType("int");

        builder.Property(x => x.CostPrice)
            .HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.Product)
            .WithMany(x => x.InventoryBatches)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BranchId, x.ProductId, x.BatchNumber });
    }
}
