using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistence.Data.EntityTypeConfigurations.Abstractions;

namespace Persistence.Data.EntityTypeConfigurations
{
    public class BidEntityTypeConfiguration
        : AuditableEntityTypeConfiguration<Bid>
    {
        public override void Configure(EntityTypeBuilder<Bid> builder)
        {
            base.Configure(builder);
            builder.Property(b => b.Amount).HasPrecision(18, 2);
            // Long enough for an IPv6 address.
            builder.Property(b => b.CreatedByIp).HasMaxLength(45);

            builder.HasOne(b => b.Auction)
                   .WithMany()
                   .HasForeignKey(b => b.AuctionId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(b => b.Participant)
                   .WithMany()
                   .HasForeignKey(b => b.ParticipantId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Two bids of the same amount at the same moment: the database keeps one, the other gets 409.
            builder.HasIndex(b => new { b.AuctionId, b.Amount }).IsUnique().HasFilter(NotDeletedSql);
            builder.HasIndex(b => new { b.AuctionId, b.CreatedAt });

            builder.ToTable(t => t.HasCheckConstraint("CK_Bids_Amount", "\"Amount\" > 0"));
        }
    }
}
