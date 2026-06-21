using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Transactions;

namespace Tanzeem.Persistence.Data.Configurations.TransactionsConfigurations {
    public class TransactionItemsConfiguration : IEntityTypeConfiguration<TransactionItem> {

        public void Configure(EntityTypeBuilder<TransactionItem> builder) {

            builder.Property(x => x.QuantityOfTransactedItem)
                .HasColumnType("int");

            builder.Property(x => x.UnitPrice)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.UnitCost)
                .HasColumnType("decimal(18,2)");

            builder.Property(ti => ti.BatchNumber)
                .HasMaxLength(100);
            
            builder.HasOne(ti => ti.Transaction)
                .WithMany(t => t.TransactionItems)
                .HasForeignKey(ti => ti.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ti => ti.Product)
                .WithMany(p => p.TransactionItems)
                .HasForeignKey(ti => ti.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

        }

    }
}
