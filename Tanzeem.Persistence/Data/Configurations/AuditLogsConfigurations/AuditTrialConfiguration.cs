using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.AuditLogs;

namespace Tanzeem.Persistence.Data.Configurations.AuditLogsConfigurations
{
    public class AuditTrialConfiguration : IEntityTypeConfiguration<AuditTrial>
    {
        public void Configure(EntityTypeBuilder<AuditTrial> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EntityName).IsRequired();
            builder.Property(x => x.Action).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();

            builder.HasOne(x => x.User).WithMany(x => x.auditTrials).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
            builder.HasOne(x => x.Branch).WithMany(x => x.auditTrials).HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        }
    }
}
