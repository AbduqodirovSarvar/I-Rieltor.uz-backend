using Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistance.Data.EntityTypeConfigurations.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistance.Data.EntityTypeConfigurations
{
    public class UserRolePermissionEntityTypeConfiguration
        : AudiTableEntityTypeConfiguration<UserRolePermission>
    {
        public override void Configure(EntityTypeBuilder<UserRolePermission> builder)
        {
            base.Configure(builder);
            builder.Property(urp => urp.Permission)
                .IsRequired();
            builder.HasIndex(urp => new { urp.UserRoleId, urp.Permission })
                .IsUnique();

        }
    }
}
