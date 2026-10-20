using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    /// <summary>
    /// Mijoz (foydalanuvchi) turi.
    /// </summary>
    public enum ClientType
    {
        /// <summary>
        /// Aniqlanmagan (masalan administrator).
        /// </summary>
        NONE,
        /// <summary>
        /// Jismoniy shaxs, PINFL bilan aniqlanadi.
        /// </summary>
        INDIVIDUAL,
        /// <summary>
        /// Yuridik shaxs, INN bilan aniqlanadi.
        /// </summary>
        LEGAL
    }
}
