using System;

namespace Domain.Abstractions
{
    /// <summary>
    /// Base entity containing the primary key identifier.
    /// All entities in the system should inherit from this class.
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// Unique identifier for the entity.
        /// Initialized with a new GUID upon creation.
        /// </summary>
        public Guid Id { get; init; } = Guid.NewGuid();
    }
}
