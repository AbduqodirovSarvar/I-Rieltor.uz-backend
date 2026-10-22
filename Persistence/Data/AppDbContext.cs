using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.Data
{
    /// <summary>
    /// Interceptors and logging options are configured once, in <c>AddPersistence</c>.
    /// </summary>
    public class AppDbContext(DbContextOptions<AppDbContext> options)
        : DbContext(options)
    {
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<UserRole> UserRoles { get; set; }
        public virtual DbSet<UserRolePermission> UserRolePermissions { get; set; }
        public virtual DbSet<UploadedFile> UploadedFiles { get; set; }
        public virtual DbSet<Slide> Slides { get; set; }
        public virtual DbSet<Company> Companies { get; set; }
        public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
        public virtual DbSet<PropertyCategory> PropertyCategories { get; set; }
        public virtual DbSet<Property> Properties { get; set; }
        public virtual DbSet<PropertyMedia> PropertyMedia { get; set; }
        public virtual DbSet<Auction> Auctions { get; set; }
        public virtual DbSet<AuctionParticipant> AuctionParticipants { get; set; }
        public virtual DbSet<Bid> Bids { get; set; }
        public virtual DbSet<AuctionResult> AuctionResults { get; set; }
        public virtual DbSet<AuctionAward> AuctionAwards { get; set; }
        public virtual DbSet<Notification> Notifications { get; set; }
        public virtual DbSet<AuditLog> AuditLogs { get; set; }
        public virtual DbSet<PrivateDocument> PrivateDocuments { get; set; }
        public virtual DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Case-insensitive text for Users.Email (a trusted extension: the database owner may create it).
            modelBuilder.HasPostgresExtension("citext");
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
