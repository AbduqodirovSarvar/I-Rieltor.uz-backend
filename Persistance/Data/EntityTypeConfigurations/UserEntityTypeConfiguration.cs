using Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistance.Data.EntityTypeConfigurations.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistance.Data.EntityTypeConfigurations
{
    public class UserEntityTypeConfiguration
        : AudiTableEntityTypeConfiguration<User>
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
            builder.Property(u => u.Email)
                   .HasMaxLength(150)
                   .IsRequired();
            builder.HasIndex(u => u.Email)
                   .IsUnique();
            builder.HasIndex(u => u.Login)
                   .IsUnique();
            builder.HasIndex(u => u.INNPINFL)
                   .IsUnique();
            builder.HasIndex(u => u.Phone)
                   .IsUnique();
        }
    }
}
