using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Settings;

namespace Tanzeem.Persistence.Data.Configurations.SettingsConfiguration
{
    public class AISettingsConfigurations : IEntityTypeConfiguration<AIConfigurations>
    {
        public void Configure(EntityTypeBuilder<AIConfigurations> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Branch).WithOne(x => x.AIConfiguration).HasForeignKey<AIConfigurations>(x => x.BranchId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
