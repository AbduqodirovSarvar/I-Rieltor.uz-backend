using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistence.Data.EntityTypeConfigurations.Abstractions;

namespace Persistence.Data.EntityTypeConfigurations
{
    public class PropertyMediaEntityTypeConfiguration
        : AuditableEntityTypeConfiguration<PropertyMedia>
    {
        public override void Configure(EntityTypeBuilder<PropertyMedia> builder)
        {
            base.Configure(builder);
            builder.Property(m => m.Kind).HasConversion<string>().HasMaxLength(20);
            builder.Property(m => m.Title).HasMaxLength(200);

            builder.HasOne(m => m.Property)
                   .WithMany(p => p.Media)
                   .HasForeignKey(m => m.PropertyId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(m => m.UploadedFile)
                   .WithMany()
                   .HasForeignKey(m => m.UploadedFileId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(m => new { m.PropertyId, m.UploadedFileId }).IsUnique().HasFilter(NotDeletedSql);
            // One cover image per property.
            builder.HasIndex(m => m.PropertyId)
                   .IsUnique()
                   .HasFilter(NotDeletedSql + " AND \"IsCover\"")
                   .HasDatabaseName("IX_PropertyMedia_PropertyId_Cover");
        }
    }
}
