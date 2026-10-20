using System;

namespace Domain.Behaviours
{
    /// <summary>
    /// Marks a property that list endpoints must never filter on (for example a password hash),
    /// because a filter like "startswith" would let a caller guess its value character by character.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class NotFilterableAttribute : Attribute
    {
    }
}
