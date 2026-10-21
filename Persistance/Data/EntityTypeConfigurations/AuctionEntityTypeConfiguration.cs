using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistance.Data.EntityTypeConfigurations.Abstractions;

namespace Persistance.Data.EntityTypeConfigurations
{
    public class AuctionEntityTypeConfiguration
        : AudiTableEntityTypeConfiguration<Auction>
    {
        public override void Configure(EntityTypeBuilder<Auction> builder)
        {
            base.Configure(builder);
            builder.Property(a => a.LotNumber).HasMaxLength(30).IsRequired();
            builder.Property(a => a.Title).HasMaxLength(200).IsRequired();
            builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(a => a.StartingPrice).HasPrecision(18, 2);
            builder.Property(a => a.MinBidStep).HasPrecision(18, 2);
            builder.Property(a => a.DepositAmount).HasPrecision(18, 2);
            builder.Property(a => a.CurrentPrice).HasPrecision(18, 2);
            builder.Property(a => a.CancelReason).HasMaxLength(500);

            builder.HasOne(a => a.Property)
                   .WithMany()
                   .HasForeignKey(a => a.PropertyId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Bid>()
                   .WithMany()
                   .HasForeignKey(a => a.LeadingBidId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(a => a.LotNumber).IsUnique().HasFilter(NotDeletedSql);
            // A property is in at most one live auction; a failed or cancelled one does not block the next.
            builder.HasIndex(a => a.PropertyId)
                   .IsUnique()
                   .HasFilter(NotDeletedSql + " AND \"Status\" IN ('DRAFT', 'SCHEDULED', 'ONGOING')")
                   .HasDatabaseName("IX_Auctions_PropertyId_Live");
            builder.HasIndex(a => new { a.Status, a.StartsAt });
            builder.HasIndex(a => new { a.Status, a.EndsAt });

            builder.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Auctions_StartingPrice", "\"StartingPrice\" > 0");
                t.HasCheckConstraint("CK_Auctions_MinBidStep", "\"MinBidStep\" > 0");
                t.HasCheckConstraint("CK_Auctions_DepositAmount", "\"DepositAmount\" >= 0");
                t.HasCheckConstraint("CK_Auctions_Times", "\"ApplicationDeadline\" <= \"StartsAt\" AND \"StartsAt\" < \"EndsAt\"");
                t.HasCheckConstraint("CK_Auctions_ExtensionMinutes", "\"ExtensionMinutes\" BETWEEN 0 AND 60");
                t.HasCheckConstraint("CK_Auctions_CurrentPrice", "\"CurrentPrice\" IS NULL OR \"CurrentPrice\" >= \"StartingPrice\"");
            });
        }
    }
}
