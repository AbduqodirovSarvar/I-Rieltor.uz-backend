using System;
using System.ComponentModel.DataAnnotations;

namespace Domain.Behaviours
{
    /// <summary>
    /// Validates that a string property is a valid absolute URL.
    /// Usage: Apply to properties, fields, or parameters.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class UriValidationAttribute : ValidationAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UriValidationAttribute"/> class
        /// with a default error message.
        /// </summary>
        public UriValidationAttribute()
        {
            ErrorMessage = "Invalid Url!";
        }

        /// <summary>
        /// Determines whether the specified value is a valid absolute URL.
        /// </summary>
        /// <param name="value">The value of the property being validated.</param>
        /// <returns>True if valid; otherwise, false.</returns>
        public override bool IsValid(object? value)
        {
            if (value == null || value is not string url)
            {
                return false;
            }

            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }
    }
}
