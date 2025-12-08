using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Domain.Behaviours
{
    /// <summary>
    /// Validates that a string property is a valid email address.
    /// Usage: Apply to properties, fields, or parameters.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class MailValidationAttribute : ValidationAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MailValidationAttribute"/> class
        /// with a default error message.
        /// </summary>
        public MailValidationAttribute()
        {
            ErrorMessage = "Invalid mail address!";
        }

        /// <summary>
        /// Determines whether the specified value is a valid email address.
        /// </summary>
        /// <param name="value">The value of the property being validated.</param>
        /// <returns>True if valid; otherwise, false.</returns>
        public override bool IsValid(object? value)
        {
            if (value == null || value is not string email)
            {
                return false;
            }

            const string emailPattern = @"^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$";
            return Regex.IsMatch(email, emailPattern);
        }
    }
}
