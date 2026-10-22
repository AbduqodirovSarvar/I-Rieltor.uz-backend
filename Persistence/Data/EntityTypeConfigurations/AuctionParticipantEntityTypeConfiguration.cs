using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistence.Data.EntityTypeConfigurations.Abstractions;

namespace Persistence.Data.EntityTypeConfigurations
{
    public class AuctionParticipantEntityTypeConfiguration
        : AuditableEntityTypeConfiguration<AuctionParticipant>
    {
        public override void Configure(EntityTypeBuilder<AuctionParticipant> builder)
        {
            base.Configure(builder);
            builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(p => p.DepositStatus).HasConversion<string>().HasMaxLength(20);
            builder.Property(p => p.SignerIdentifier).HasMaxLength(14);
            builder.Property(p => p.RejectReason).HasMaxLength(500);

            builder.HasOne(p => p.Auction)
                   .WithMany(a => a.Participants)
                   .HasForeignKey(p => p.AuctionId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(p => p.User)
                   .WithMany()
                   .HasForeignKey(p => p.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // One application per user and auction; after withdrawing, the user may apply again.
            builder.HasIndex(p => new { p.AuctionId, p.UserId })
                   .IsUnique()
                   .HasFilter(NotDeletedSql + " AND \"Status\" <> 'WITHDRAWN'");
            builder.HasIndex(p => new { p.AuctionId, p.ParticipantNumber })
                   .IsUnique()
                   .HasFilter("\"ParticipantNumber\" IS NOT NULL");
            builder.HasIndex(p => p.UserId);

            builder.ToTable(t => t.HasCheckConstraint("CK_AuctionParticipants_ParticipantNumber", "\"ParticipantNumber\" IS NULL OR \"ParticipantNumber\" > 0"));
        }
    }
}
