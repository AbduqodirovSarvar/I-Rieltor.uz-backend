using Domain.Behaviours;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.Dtos.Auths
{
    /// <summary>
    /// DTO used for user registration via E-IMZO.
    /// Contains only the fields that can be generated
    /// or validated using the E-IMZO digital signature.
    /// </summary>
    public record RegisterViaEimzoDto
    {
        /// <summary>
        /// Base64 or hex PKCS#7 signed data received from the E-IMZO client.
        /// This data is verified server-side to extract certificate information.
        /// </summary>
        [Required(ErrorMessage = "Signed data is required.")]
        public string SignedData { get; init; } = null!;
    }
}
