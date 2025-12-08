using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.ViewModels.Auths
{
    public record TokenViewModel
    {
        public string? AccessToken { get; init; }
        public string? RefreshToken { get; init; }
        public DateTime? AccessTokenExpiration { get; init; }
        public DateTime? RefreshTokenExpiration { get; init; }
        public string? TokenType { get; init; } = "Bearer";
        public string? UserId { get; init; }
    }
}
