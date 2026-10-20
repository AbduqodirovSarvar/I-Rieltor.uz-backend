using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    /// <summary>
    /// Auksion g'olibining to'lov holati.
    /// </summary>
    public enum PaymentStatus
    {
        /// <summary>
        /// To'lov kutilmoqda, hali hech narsa to'lanmagan.
        /// </summary>
        PENDING,
        /// <summary>
        /// Summaning bir qismi to'langan.
        /// </summary>
        PARTIALLY_PAID,
        /// <summary>
        /// To'liq to'langan.
        /// </summary>
        PAID,
        /// <summary>
        /// To'lov muddati o'tib ketgan.
        /// </summary>
        OVERDUE
    }
}
