using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    /// <summary>
    /// Auksion holati.
    /// </summary>
    public enum AuctionStatus
    {
        /// <summary>
        /// Rejalashtirilgan, savdo hali boshlanmagan.
        /// </summary>
        UPCOMING,
        /// <summary>
        /// Savdo hozir davom etmoqda, takliflar qabul qilinadi.
        /// </summary>
        ONGOING,
        /// <summary>
        /// Savdo yakunlangan, g'olib aniqlangan.
        /// </summary>
        COMPLETED,
        /// <summary>
        /// Savdo bekor qilingan.
        /// </summary>
        CANCELLED
    }
}
