using Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistence.Data.EntityTypeConfigurations.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.Data.EntityTypeConfigurations
{
    public class UserEntityTypeConfiguration
        : AuditableEntityTypeConfiguration<User>
    {
        public override void Configure(EntityTypeBuilder<User> builder)
        {
            base.Configure(builder);
            builder.HasOne(x => x.UserRole)
                    .WithMany(x => x.Users)
                    .HasForeignKey(x => x.UserRoleId)
                    .OnDelete(DeleteBehavior.NoAction);
            builder.Property(u => u.Login)
                   .HasMaxLength(50);
            builder.Property(u => u.LegalName)
                   .HasMaxLength(200);
            builder.Property(u => u.INNPINFL)
                   .HasMaxLength(14);
            builder.Property(u => u.Phone)
                   .HasMaxLength(15);
            builder.Property(u => u.SecondaryPhone)
                   .HasMaxLength(15);
            // Optional: users registered via E-IMZO have no email. PostgreSQL unique indexes allow many NULLs.
            // citext: comparisons and the unique index ignore letter case, so "A@x.uz" and "a@x.uz" are one address
            // everywhere (sign-in, duplicate checks, the index) without lower() in every query.
            builder.Property(u => u.Email)
                   .HasColumnType("citext")
                   .HasMaxLength(150);
            builder.HasIndex(u => u.Email)
                   .IsUnique()
                   .HasFilter(NotDeletedSql);
            builder.HasIndex(u => u.Login)
                   .IsUnique()
                   .HasFilter(NotDeletedSql);
            builder.HasIndex(u => u.INNPINFL)
                   .IsUnique()
                   .HasFilter(NotDeletedSql);
            builder.HasIndex(u => u.Phone)
                   .IsUnique()
                   .HasFilter(NotDeletedSql);
        }
    }
}
