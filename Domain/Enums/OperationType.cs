using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    /// <summary>
    /// CRUD amal turi. Ruxsat nomini yasashda ishlatiladi (masalan CREATE_USER).
    /// </summary>
    public enum OperationType
    {
        /// <summary>
        /// Amal yo'q.
        /// </summary>
        NONE = 0,
        /// <summary>
        /// Yangi yozuv yaratish.
        /// </summary>
        CREATE = 1,
        /// <summary>
        /// Yozuvni o'qish yoki ro'yxatini olish.
        /// </summary>
        READ = 2,
        /// <summary>
        /// Mavjud yozuvni o'zgartirish.
        /// </summary>
        UPDATE = 3,
        /// <summary>
        /// Yozuvni o'chirish.
        /// </summary>
        DELETE = 4,
    }
}
