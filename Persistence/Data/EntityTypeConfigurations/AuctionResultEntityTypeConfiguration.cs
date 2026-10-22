using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistence.Data.EntityTypeConfigurations.Abstractions;

namespace Persistence.Data.EntityTypeConfigurations
{
    public class AuctionResultEntityTypeConfiguration
        : AuditableEntityTypeConfiguration<AuctionResult>
    {
        public override void Configure(EntityTypeBuilder<AuctionResult> builder)
        {
            base.Configure(builder);
            builder.Property(r => r.Outcome).HasConversion<string>().HasMaxLength(20);
            builder.Property(r => r.FinalPrice).HasPrecision(18, 2);

            builder.HasOne(r => r.Auction)
                   .WithMany()
                   .HasForeignKey(r => r.AuctionId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(r => r.WinnerParticipant)
                   .WithMany()
                   .HasForeignKey(r => r.WinnerParticipantId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Bid>()
                   .WithMany()
                   .HasForeignKey(r => r.WinningBidId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(r => r.ProtocolFile)
                   .WithMany()
                   .HasForeignKey(r => r.ProtocolFileId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(r => r.AuctionId).IsUnique().HasFilter(NotDeletedSql);

            builder.ToTable(t =>
            {
                // A buyer (with the bid and the price) exactly while the payment is awaited and once sold.
                t.HasCheckConstraint("CK_AuctionResults_Winner",
                    "(\"Outcome\" IN ('AWAITING_PAYMENT', 'SOLD')) = (\"WinnerParticipantId\" IS NOT NULL AND \"WinningBidId\" IS NOT NULL AND \"FinalPrice\" IS NOT NULL)");
                t.HasCheckConstraint("CK_AuctionResults_FinalPrice", "\"FinalPrice\" IS NULL OR \"FinalPrice\" > 0");
            });
        }
    }
}
