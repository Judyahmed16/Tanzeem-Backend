using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tanzeem.Domain.Entities.Users;

namespace Tanzeem.Persistence.Data.Configurations.UsersConfigurations
{
    public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
    {
        public void Configure(EntityTypeBuilder<UserSession> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.SessionKey)
                .HasMaxLength(64)
                .IsRequired();

            builder.HasIndex(x => x.SessionKey)
                .IsUnique();

            builder.Property(x => x.DeviceName)
                .HasMaxLength(120);

            builder.Property(x => x.IpAddress)
                .HasMaxLength(64);

            builder.Property(x => x.UserAgent)
                .HasMaxLength(512);

            builder.Property(x => x.RevokedReason)
                .HasMaxLength(200);

            builder.HasOne(x => x.User)
                .WithMany(x => x.Sessions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
