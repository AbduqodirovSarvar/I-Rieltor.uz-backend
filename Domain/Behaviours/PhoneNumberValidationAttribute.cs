using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Domain.Behaviours
{
    /// <summary>
    /// Validates that a string property is a valid phone number.
    /// Expected format: +<country code><number> (e.g., +998901234567)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class PhoneNumberValidationAttribute : ValidationAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PhoneNumberValidationAttribute"/> class
        /// with a default error message.
        /// </summary>
        public PhoneNumberValidationAttribute()
        {
            ErrorMessage = "Invalid phone number.";
        }

        /// <summary>
        /// Determines whether the specified value is a valid phone number.
        /// </summary>
        /// <param name="value">The value of the property being validated.</param>
        /// <returns>True if valid; otherwise, false.</returns>
        public override bool IsValid(object? value)
        {
            if (string.IsNullOrWhiteSpace(value?.ToString()))
                return true;

            string input = value.ToString()!;

            // "+", then 10 to 14 digits (at most 15 characters, the column size), e.g. +998901234567.
            const string phonePattern = @"^\+[1-9]\d{9,13}$";
            return Regex.IsMatch(input, phonePattern);
        }
    }
}
