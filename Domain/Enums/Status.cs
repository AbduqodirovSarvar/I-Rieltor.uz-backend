using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    /// <summary>
    /// Ko'chmas mulk obyektining sotuvdagi holati.
    /// </summary>
    public enum Status
    {
        /// <summary>
        /// Sotuvda (auksionga qo'yilgan).
        /// </summary>
        SOLDING,
        /// <summary>
        /// Qayta sotuvga qo'yilgan (oldingi savdo natijasiz tugagan).
        /// </summary>
        RESOLDING,
        /// <summary>
        /// Sotilgan.
        /// </summary>
        SOLD,
        /// <summary>
        /// Tasdiq kutilmoqda, hali sotuvga chiqarilmagan.
        /// </summary>
        PENDING
    }
}
