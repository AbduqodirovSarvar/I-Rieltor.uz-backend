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
    public class UploadedFileEntityTypeConfiguration
        : AudiTableEntityTypeConfiguration<UploadedFile>
    {
        public override void Configure(EntityTypeBuilder<UploadedFile> builder)
        {
            base.Configure(builder);
            builder.HasIndex(x => x.Url).IsUnique();
            builder.HasIndex(x => x.Name).IsUnique();
        }
    }
}
