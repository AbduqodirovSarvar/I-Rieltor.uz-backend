using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistence.Data.EntityTypeConfigurations.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.Data.EntityTypeConfigurations
{
    public class SlideEntityTypeConfiguration
        : AuditableEntityTypeConfiguration<Slide>
    {
        public override void Configure(EntityTypeBuilder<Slide> builder)
        {
            base.Configure(builder);
            builder.HasOne(s => s.UploadedFile)
                   .WithOne()
                   .HasForeignKey<Slide>(s => s.UploadedFileId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
