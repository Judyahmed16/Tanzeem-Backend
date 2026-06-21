using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Settings;

namespace Tanzeem.Persistence.Data.Configurations.SettingsConfiguration
{
    internal class SettingsConfiguration : IEntityTypeConfiguration<AlertConfigurations>
    {
        public void Configure(EntityTypeBuilder<AlertConfigurations> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(x => x.Branch).WithOne(x => x.AlertConfigurations).HasForeignKey<AlertConfigurations>(x => x.BranchId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
