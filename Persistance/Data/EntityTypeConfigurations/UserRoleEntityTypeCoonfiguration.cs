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
    public class UserRoleEntityTypeCoonfiguration
        : AudiTableEntityTypeConfiguration<UserRole>
    {
        public override void Configure(EntityTypeBuilder<UserRole> builder)
        {
            base.Configure(builder);
            builder.Property(ur => ur.Name)
                .IsRequired()
                .HasMaxLength(100);
            builder.HasIndex(ur => ur.Name)
                .IsUnique();
        }
    }
}
