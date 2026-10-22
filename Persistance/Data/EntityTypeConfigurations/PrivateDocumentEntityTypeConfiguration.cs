using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistance.Data.EntityTypeConfigurations.Abstractions;

namespace Persistance.Data.EntityTypeConfigurations
{
    public class PrivateDocumentEntityTypeConfiguration
        : AudiTableEntityTypeConfiguration<PrivateDocument>
    {
        public override void Configure(EntityTypeBuilder<PrivateDocument> builder)
        {
            base.Configure(builder);
            builder.Property(d => d.Purpose).HasConversion<string>().HasMaxLength(30);
            builder.Property(d => d.OriginalName).HasMaxLength(255).IsRequired();
            builder.Property(d => d.StoredName).HasMaxLength(60).IsRequired();
            builder.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
            builder.Property(d => d.Description).HasMaxLength(500);

            builder.HasOne(d => d.Owner)
                   .WithMany()
                   .HasForeignKey(d => d.OwnerId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Auction>()
                   .WithMany()
                   .HasForeignKey(d => d.AuctionId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Payment>()
                   .WithMany()
                   .HasForeignKey(d => d.PaymentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(d => d.StoredName).IsUnique();
            builder.HasIndex(d => new { d.OwnerId, d.CreatedAt });
            builder.HasIndex(d => d.AuctionId);

            builder.ToTable(t => t.HasCheckConstraint("CK_PrivateDocuments_Size", "\"Size\" > 0"));
        }
    }
}
