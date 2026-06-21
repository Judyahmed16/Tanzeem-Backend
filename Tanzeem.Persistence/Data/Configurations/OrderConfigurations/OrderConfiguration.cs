using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Orders;
using Tanzeem.Domain.Enums;

namespace Tanzeem.Persistence.Data.Configurations.OrderConfigurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.OrderDate).HasColumnType("date").HasDefaultValueSql("GETDATE()"); ;
            builder.Property(x => x.Status).HasConversion<string>().HasDefaultValue(OrderStatus.Pending);
            builder.Property(x => x.ExpectedDeliveryDate).HasColumnType("date");
            builder.Property(x => x.RecievedDeliveryDate).HasColumnType("date").IsRequired(false);
            builder.Property(x => x.Total).HasPrecision(10, 2);
            builder.Property(o => o.OrderNumber).HasDefaultValue("Old-Record");

            builder.Property(x => x.ShippingCost).HasPrecision(7, 2);//num of nums , nums after point
            builder.Property(x => x.Taxes).HasPrecision(7, 2);
            


            //builder.Property(x => x.Notes).HasMaxLength(40);

            builder.HasOne(x => x.Supplier).WithMany(x => x.Orders).HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
            builder.HasOne(x => x.Branch).WithMany(x => x.Orders).HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);

        }
    }
}
