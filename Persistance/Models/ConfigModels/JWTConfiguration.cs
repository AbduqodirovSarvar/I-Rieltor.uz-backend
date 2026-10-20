using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistance.Models.ConfigModels
{
    public sealed class JWTConfiguration
    {
        public string ValidIssuer { get; set; } = null!;
        public string ValidAudience { get; set; } = null!;
        public string Secret { get; set; } = null!;

        /// <summary>
        /// Access tokens cannot be revoked, so they live briefly; clients renew them with a refresh token.
        /// </summary>
        public int AccessTokenMinutes { get; set; } = 30;

        /// <summary>
        /// A refresh token unused this long expires; each refresh starts the period again, up to <see cref="SessionDays"/>.
        /// </summary>
        public int RefreshTokenDays { get; set; } = 14;

        /// <summary>
        /// Absolute limit of one sign-in: after it the user must sign in again, however often the client refreshed.
        /// </summary>
        public int SessionDays { get; set; } = 30;
    }
}
