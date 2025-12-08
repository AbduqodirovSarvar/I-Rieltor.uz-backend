using System;

namespace Domain.Abstractions
{
    /// <summary>
    /// Base entity for auditing purposes.
    /// Tracks creation, updates, and soft deletion status.
    /// Inherits from BaseEntity.
    /// </summary>
    public abstract class AudiTableEntity : BaseEntity
    {
        /// <summary>
        /// Date and time when the entity was created.
        /// Automatically set when creating a new record.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Identifier of the user who created the entity.
        /// Nullable for system-generated records.
        /// </summary>
        public Guid? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the entity was last updated.
        /// Nullable if the entity has never been updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Identifier of the user who last updated the entity.
        /// Nullable if the entity has never been updated.
        /// </summary>
        public Guid? UpdatedBy { get; set; }

        /// <summary>
        /// Indicates whether the entity is soft-deleted.
        /// If true, the entity is considered deleted but not removed from the database.
        /// </summary>
        public bool IsDeleted { get; set; } = false;
    }
}
