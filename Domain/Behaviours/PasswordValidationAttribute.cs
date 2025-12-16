using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Domain.Behaviours
{
    /// <summary>
    /// Validates that a string property is a strong password.
    /// Criteria:
    /// - Minimum 8 characters
    /// - At least 1 uppercase letter
    /// - At least 1 digit
    /// - At least 1 symbol
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class PasswordValidationAttribute : ValidationAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordValidationAttribute"/> class
        /// with a default error message.
        /// </summary>
        public PasswordValidationAttribute()
        {
            ErrorMessage = "Invalid password format. It must have a minimum of 8 characters, at least 1 uppercase letter, 1 digit and 1 symbol!";
        }

        /// <summary>
        /// Determines whether the specified value is a valid password.
        /// </summary>
        /// <param name="value">The value of the property being validated.</param>
        /// <returns>True if valid; otherwise, false.</returns>
        public override bool IsValid(object? value)
        {
            if (string.IsNullOrWhiteSpace(value?.ToString()))
                return true;

            string input = value.ToString()!;

            const string passwordPattern = @"^(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$";
            return Regex.IsMatch(input, passwordPattern);
        }
    }
}
