using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistance.Data.EntityTypeConfigurations.Abstractions;

namespace Persistance.Data.EntityTypeConfigurations
{
    public class PaymentEntityTypeConfiguration
        : AudiTableEntityTypeConfiguration<Payment>
    {
        public override void Configure(EntityTypeBuilder<Payment> builder)
        {
            base.Configure(builder);
            builder.Property(p => p.Purpose).HasConversion<string>().HasMaxLength(20);
            builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(p => p.Provider).HasConversion<string>().HasMaxLength(20);
            builder.Property(p => p.Amount).HasPrecision(18, 2);
            builder.Property(p => p.ProviderTransactionId).HasMaxLength(100);
            builder.Property(p => p.Note).HasMaxLength(500);

            builder.HasOne(p => p.User)
                   .WithMany()
                   .HasForeignKey(p => p.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(p => p.Auction)
                   .WithMany()
                   .HasForeignKey(p => p.AuctionId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(p => p.Participant)
                   .WithMany()
                   .HasForeignKey(p => p.ParticipantId)
                   .OnDelete(DeleteBehavior.Restrict);

            // A provider callback delivered twice must not become a second payment.
            builder.HasIndex(p => new { p.Provider, p.ProviderTransactionId })
                   .IsUnique()
                   .HasFilter("\"ProviderTransactionId\" IS NOT NULL");
            builder.HasIndex(p => p.UserId);
            builder.HasIndex(p => new { p.AuctionId, p.Purpose });

            builder.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Payments_Amount", "\"Amount\" > 0");
                t.HasCheckConstraint("CK_Payments_PaidAt", "\"Status\" NOT IN ('PAID', 'REFUNDED') OR \"PaidAt\" IS NOT NULL");
                t.HasCheckConstraint("CK_Payments_Participant", "\"Purpose\" = 'FINAL_PAYMENT' OR \"ParticipantId\" IS NOT NULL");
            });
        }
    }
}
