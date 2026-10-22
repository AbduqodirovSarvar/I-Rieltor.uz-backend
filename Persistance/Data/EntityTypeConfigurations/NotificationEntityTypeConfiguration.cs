using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistance.Data.EntityTypeConfigurations.Abstractions;

namespace Persistance.Data.EntityTypeConfigurations
{
    public class NotificationEntityTypeConfiguration
        : AudiTableEntityTypeConfiguration<Notification>
    {
        public override void Configure(EntityTypeBuilder<Notification> builder)
        {
            base.Configure(builder);
            builder.Property(n => n.Channel).HasConversion<string>().HasMaxLength(10);
            builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(30);
            builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(10);
            builder.Property(n => n.Destination).HasMaxLength(150).IsRequired();
            builder.Property(n => n.Subject).HasMaxLength(200);
            builder.Property(n => n.Body).HasMaxLength(4000).IsRequired();
            builder.Property(n => n.LastError).HasMaxLength(500);

            builder.HasOne(n => n.User)
                   .WithMany()
                   .HasForeignKey(n => n.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // What the dispatcher asks every few seconds: due, still pending.
            builder.HasIndex(n => new { n.Status, n.NextAttemptAt });
            builder.HasIndex(n => new { n.UserId, n.CreatedAt });
            // "Has this reminder been sent already?"
            builder.HasIndex(n => new { n.UserId, n.Type, n.AuctionId });

            builder.ToTable(t => t.HasCheckConstraint("CK_Notifications_Attempts", "\"Attempts\" >= 0"));
        }
    }
}
