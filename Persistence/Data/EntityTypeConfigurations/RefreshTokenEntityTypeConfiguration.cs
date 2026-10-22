using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistence.Data.EntityTypeConfigurations.Abstractions;

namespace Persistence.Data.EntityTypeConfigurations
{
    public class RefreshTokenEntityTypeConfiguration
        : AuditableEntityTypeConfiguration<RefreshToken>
    {
        public override void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            base.Configure(builder);
            builder.HasOne(x => x.User)
                   .WithMany()
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
            // SHA-256 in hex is always 64 characters.
            builder.Property(x => x.TokenHash)
                   .HasMaxLength(64)
                   .IsRequired();
            builder.HasIndex(x => x.TokenHash)
                   .IsUnique();
            builder.HasIndex(x => x.UserId);
            // A detected token theft revokes one session by this key.
            builder.HasIndex(x => x.SessionId);
            // Long enough for an IPv6 address.
            builder.Property(x => x.CreatedByIp)
                   .HasMaxLength(45);
        }
    }
}
