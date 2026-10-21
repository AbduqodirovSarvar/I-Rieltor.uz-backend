using Domain.Abstractions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistance.Data.EntityTypeConfigurations.Abstractions
{
    public abstract class AudiTableEntityTypeConfiguration<TEntity> : BaseEntityTypeConfiguration<TEntity>
       where TEntity : AudiTableEntity
    {
        /// <summary>
        /// Name of the filter hiding soft-deleted rows; pass it to <c>IgnoreQueryFilters([...])</c> to see them.
        /// </summary>
        public const string SoftDeleteFilter = "SoftDelete";

        /// <summary>
        /// Unique indexes use this so a soft-deleted row does not block a new one with the same value
        /// (e.g. a deleted user registering again with the same PINFL).
        /// </summary>
        protected const string NotDeletedSql = "\"IsDeleted\" = false";

        public override void Configure(EntityTypeBuilder<TEntity> builder)
        {
            base.Configure(builder);
            // Deletes are turned into IsDeleted = true by AuditableEntitySaveChangesInterceptor;
            // every query, including Include and navigation subqueries, skips those rows.
            builder.HasQueryFilter(SoftDeleteFilter, e => !e.IsDeleted);
            // Npgsql maps a uint row version to the xmin system column: every UPDATE/DELETE is sent with
            // "WHERE xmin = <loaded value>", so a row changed or soft-deleted in between is not overwritten.
            builder.Property(e => e.Version).IsRowVersion();
        }
    }
}
