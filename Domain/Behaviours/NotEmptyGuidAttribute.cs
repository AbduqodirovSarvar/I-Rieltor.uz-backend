using System;
using System.ComponentModel.DataAnnotations;

namespace Domain.Behaviours
{
    /// <summary>
    /// [Required] never fails for a Guid: a missing or zero id binds to Guid.Empty. This rejects it with 400
    /// instead of a lookup that answers 404.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class NotEmptyGuidAttribute : ValidationAttribute
    {
        public NotEmptyGuidAttribute()
        {
            ErrorMessage = "The {0} field is required.";
        }

        public override bool IsValid(object? value) => value is not Guid id || id != Guid.Empty;
    }
}
