using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistence.Data.EntityTypeConfigurations.Abstractions;

namespace Persistence.Data.EntityTypeConfigurations
{
    public class AuditLogEntityTypeConfiguration
        : AuditableEntityTypeConfiguration<AuditLog>
    {
        public override void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            base.Configure(builder);
            builder.Property(a => a.Action).HasMaxLength(200).IsRequired();
            builder.Property(a => a.Method).HasMaxLength(10).IsRequired();
            builder.Property(a => a.Path).HasMaxLength(300).IsRequired();
            builder.Property(a => a.IpAddress).HasMaxLength(45);
            builder.Property(a => a.UserAgent).HasMaxLength(300);
            builder.Property(a => a.CorrelationId).HasMaxLength(64);

            builder.HasOne(a => a.User)
                   .WithMany()
                   .HasForeignKey(a => a.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(a => a.OccurredAt);
            builder.HasIndex(a => new { a.UserId, a.OccurredAt });
        }
    }
}
