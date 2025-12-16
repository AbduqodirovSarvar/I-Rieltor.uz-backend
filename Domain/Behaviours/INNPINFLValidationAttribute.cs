using Domain.Entities;
using Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Domain.Behaviours
{
    /// <summary>
    /// Validation attribute for INNPINFL property of the <see cref="User"/> entity.
    /// Ensures that:
    /// - For Individual clients, PINFL must be exactly 14 digits.
    /// - For Legal clients, INN must be exactly 9 digits.
    /// </summary>
    public class INNPINFLValidationAttribute : ValidationAttribute
    {
        /// <summary>
        /// Validates the specified value based on the <see cref="User.Type"/>.
        /// </summary>
        /// <param name="value">The value of the property being validated (INNPINFL/INN).</param>
        /// <param name="validationContext">Context information about the validation operation.</param>
        /// <returns>
        /// A <see cref="ValidationResult"/> indicating whether validation succeeded.
        /// Returns <see cref="ValidationResult.Success"/> if valid; otherwise, an error message.
        /// </returns>
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(value?.ToString()))
                return ValidationResult.Success;

            string input = value.ToString()!;

            if (!Regex.IsMatch(input, @"^\d{9,14}$"))
            {
                return new ValidationResult(
                    "Value must contain only digits and be between 9 and 14 characters long."
                );
            }

            return ValidationResult.Success;
        }
    }
}
