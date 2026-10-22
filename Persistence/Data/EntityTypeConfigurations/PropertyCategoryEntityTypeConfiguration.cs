using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistence.Data.EntityTypeConfigurations.Abstractions;

namespace Persistence.Data.EntityTypeConfigurations
{
    public class PropertyCategoryEntityTypeConfiguration
        : AuditableEntityTypeConfiguration<PropertyCategory>
    {
        public override void Configure(EntityTypeBuilder<PropertyCategory> builder)
        {
            base.Configure(builder);
            // citext: "Kvartira" and "kvartira" are one name for the unique index.
            builder.Property(c => c.Name).HasColumnType("citext").HasMaxLength(100).IsRequired();
            builder.Property(c => c.Description).HasMaxLength(500);
            builder.HasIndex(c => c.Name).IsUnique().HasFilter(NotDeletedSql);
            builder.HasOne(c => c.ParentCategory)
                   .WithMany()
                   .HasForeignKey(c => c.ParentCategoryId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.ToTable(t => t.HasCheckConstraint("CK_PropertyCategories_NotOwnParent", "\"ParentCategoryId\" IS NULL OR \"ParentCategoryId\" <> \"Id\""));
        }
    }
}
