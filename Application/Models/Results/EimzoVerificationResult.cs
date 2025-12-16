using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.Results
{
    public class EimzoVerificationResult
    {
        public bool IsValid { get; init; }
        public string? Pinfl { get; init; }
        public string? Inn { get; init; }
        public string? FullName { get; init; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? LegalName { get; set; }
        public DateTime? ValidTo { get; init; }
    }
}
