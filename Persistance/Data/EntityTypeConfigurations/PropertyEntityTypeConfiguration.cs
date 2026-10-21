using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistance.Data.EntityTypeConfigurations.Abstractions;

namespace Persistance.Data.EntityTypeConfigurations
{
    public class PropertyEntityTypeConfiguration
        : AudiTableEntityTypeConfiguration<Property>
    {
        public override void Configure(EntityTypeBuilder<Property> builder)
        {
            base.Configure(builder);
            builder.Property(p => p.Title).HasMaxLength(200).IsRequired();
            builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(p => p.Region).HasMaxLength(100).IsRequired();
            builder.Property(p => p.District).HasMaxLength(100);
            builder.Property(p => p.Address).HasMaxLength(300).IsRequired();
            builder.Property(p => p.Latitude).HasPrecision(9, 6);
            builder.Property(p => p.Longitude).HasPrecision(9, 6);
            builder.Property(p => p.CadastralNumber).HasMaxLength(50);
            builder.Property(p => p.AreaSqm).HasPrecision(12, 2);
            builder.Property(p => p.LandAreaSotix).HasPrecision(12, 2);
            builder.Property(p => p.EstimatedValue).HasPrecision(18, 2);
            builder.Property(p => p.SellerName).HasMaxLength(200);

            builder.HasOne(p => p.Category)
                   .WithMany()
                   .HasForeignKey(p => p.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(p => p.CadastralNumber).IsUnique().HasFilter(NotDeletedSql + " AND \"CadastralNumber\" IS NOT NULL");
            builder.HasIndex(p => p.Status);
            builder.HasIndex(p => p.CategoryId);
            builder.HasIndex(p => p.Region);

            builder.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Properties_AreaSqm", "\"AreaSqm\" IS NULL OR \"AreaSqm\" > 0");
                t.HasCheckConstraint("CK_Properties_LandAreaSotix", "\"LandAreaSotix\" IS NULL OR \"LandAreaSotix\" > 0");
                t.HasCheckConstraint("CK_Properties_Rooms", "\"Rooms\" IS NULL OR \"Rooms\" >= 0");
                t.HasCheckConstraint("CK_Properties_TotalFloors", "\"TotalFloors\" IS NULL OR \"TotalFloors\" > 0");
                t.HasCheckConstraint("CK_Properties_EstimatedValue", "\"EstimatedValue\" IS NULL OR \"EstimatedValue\" >= 0");
                t.HasCheckConstraint("CK_Properties_Latitude", "\"Latitude\" IS NULL OR \"Latitude\" BETWEEN -90 AND 90");
                t.HasCheckConstraint("CK_Properties_Longitude", "\"Longitude\" IS NULL OR \"Longitude\" BETWEEN -180 AND 180");
            });
        }
    }
}
