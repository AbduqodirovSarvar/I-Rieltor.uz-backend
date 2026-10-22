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
    public class UserRolePermissionEntityTypeConfiguration
        : AuditableEntityTypeConfiguration<UserRolePermission>
    {
        public override void Configure(EntityTypeBuilder<UserRolePermission> builder)
        {
            base.Configure(builder);
            builder.HasOne(x => x.UserRole)
                .WithMany(ur => ur.Permissions)
                .HasForeignKey(urp => urp.UserRoleId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Property(urp => urp.Permission)
                .IsRequired();
            builder.HasIndex(urp => new { urp.UserRoleId, urp.Permission })
                .IsUnique()
                .HasFilter(NotDeletedSql);

        }
    }
}
