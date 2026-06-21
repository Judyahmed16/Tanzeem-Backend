using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Transactions;

namespace Tanzeem.Persistence.Data.Configurations.TransactionsConfigurations {
    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction> {
        
        public void Configure(EntityTypeBuilder<Transaction> builder) {

            builder.Property(x => x.TransactionId)
                .HasMaxLength(128);

            builder.Property(x => x.CreatedAt);

            builder.Property(x => x.Value)
                .HasColumnType("decimal(18, 2)");

            builder.Property(x => x.TotalTransactedItems)
                .HasColumnType("int");

            builder.Property(x => x.ReferenceNumber)
                .HasMaxLength(128);

            builder.Property(x => x.Notes)
                .HasMaxLength(512);



            builder.HasOne(t => t.Branch)
                .WithMany(b => b.Transactions)
                .HasForeignKey(t => t.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(t => t.TransactionItems)
                .WithOne(ti => ti.Transaction)
                .HasForeignKey(ti => ti.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(t => t.PreformedByUser)
                .WithMany()
                .HasForeignKey(t => t.PerformedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }


    }
}
