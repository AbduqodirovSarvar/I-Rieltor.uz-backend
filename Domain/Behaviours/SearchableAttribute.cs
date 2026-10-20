using System;

namespace Domain.Behaviours
{
    /// <summary>
    /// Marks a text property that the list endpoints' <c>SearchText</c> looks in
    /// (case-insensitive "contains"; a row matches if any marked property matches).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class SearchableAttribute : Attribute
    {
    }
}
