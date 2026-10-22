using Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistence.Data.EntityTypeConfigurations.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.Data.EntityTypeConfigurations
{
    public class UploadedFileEntityTypeConfiguration
        : AuditableEntityTypeConfiguration<UploadedFile>
    {
        public override void Configure(EntityTypeBuilder<UploadedFile> builder)
        {
            base.Configure(builder);
            builder.HasIndex(x => x.Url).IsUnique();
            builder.HasIndex(x => x.Name).IsUnique();
        }
    }
}
