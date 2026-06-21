using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Notifications;

namespace Tanzeem.Persistence.Data.Configurations.NotificationConfigurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Message).HasMaxLength(500);
            builder.Property(x => x.Title).HasMaxLength(100);
            builder.Property(x => x.Type).HasConversion<string>();

            builder.HasOne(x => x.Branch).WithMany(x => x.Notifications).HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
