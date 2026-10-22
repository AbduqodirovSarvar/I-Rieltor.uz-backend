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
    public class UserRoleEntityTypeConfiguration
        : AuditableEntityTypeConfiguration<UserRole>
    {
        public override void Configure(EntityTypeBuilder<UserRole> builder)
        {
            base.Configure(builder);
            builder.HasMany(ur => ur.Permissions)
                .WithOne(urp => urp.UserRole)
                .HasForeignKey(urp => urp.UserRoleId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            builder.Property(ur => ur.Name)
                .IsRequired()
                .HasMaxLength(100);
            builder.HasIndex(ur => ur.Name)
                .IsUnique()
                .HasFilter(NotDeletedSql);
        }
    }
}
