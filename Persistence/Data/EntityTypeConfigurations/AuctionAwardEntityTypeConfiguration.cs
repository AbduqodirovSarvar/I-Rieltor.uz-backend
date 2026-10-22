using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistence.Data.EntityTypeConfigurations.Abstractions;

namespace Persistence.Data.EntityTypeConfigurations
{
    public class AuctionAwardEntityTypeConfiguration
        : AuditableEntityTypeConfiguration<AuctionAward>
    {
        public override void Configure(EntityTypeBuilder<AuctionAward> builder)
        {
            base.Configure(builder);
            builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(a => a.Price).HasPrecision(18, 2);

            builder.HasOne(a => a.AuctionResult)
                   .WithMany(r => r.Awards)
                   .HasForeignKey(a => a.AuctionResultId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(a => a.Participant)
                   .WithMany()
                   .HasForeignKey(a => a.ParticipantId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(a => a.Bid)
                   .WithMany()
                   .HasForeignKey(a => a.BidId)
                   .OnDelete(DeleteBehavior.Restrict);

            // One award per place and per participant; one live (offered or accepted) award at a time.
            builder.HasIndex(a => new { a.AuctionResultId, a.Rank }).IsUnique().HasFilter(NotDeletedSql);
            builder.HasIndex(a => new { a.AuctionResultId, a.ParticipantId }).IsUnique().HasFilter(NotDeletedSql);
            builder.HasIndex(a => a.AuctionResultId)
                   .IsUnique()
                   .HasFilter(NotDeletedSql + " AND \"Status\" IN ('OFFERED', 'ACCEPTED')")
                   .HasDatabaseName("IX_AuctionAwards_AuctionResultId_Live");

            builder.ToTable(t =>
            {
                t.HasCheckConstraint("CK_AuctionAwards_Rank", "\"Rank\" BETWEEN 1 AND 2");
                t.HasCheckConstraint("CK_AuctionAwards_Price", "\"Price\" > 0");
                // The winner is bound by the bidding: never asked, so never "declined" or "expired".
                t.HasCheckConstraint("CK_AuctionAwards_WinnerBound", "\"Rank\" = 2 OR \"Status\" NOT IN ('OFFERED', 'DECLINED', 'EXPIRED')");
            });
        }
    }
}
