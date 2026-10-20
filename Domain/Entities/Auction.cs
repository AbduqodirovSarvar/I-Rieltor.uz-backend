using Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    /// <summary>
    /// Bitta ko'chmas mulk obyekti bo'yicha o'tkaziladigan auksion (savdo).
    /// Bitta obyekt bir necha marta auksionga qo'yilishi mumkin (qayta savdo).
    /// </summary>
    public class Auction : AudiTableEntity
    {
        /// <summary>
        /// Auksion nomi.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Auksion shartlari va tavsifi.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Savdo boshlanadigan vaqt.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Savdo tugaydigan vaqt.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Auksion holati, matn ko'rinishida (<see cref="Enums.AuctionStatus"/> qiymatlari).
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Auksion yakunlangan narx, ya'ni g'olib taklif summasi. Tugamaguncha 0 bo'ladi.
        /// </summary>
        public decimal CompletedPrice { get; set; }

        /// <summary>
        /// Savdoga qo'yilgan obyektning Id'si (FK).
        /// </summary>
        public Guid PropertyId { get; set; }

        /// <summary>
        /// Savdoga qo'yilgan obyekt (navigatsiya).
        /// </summary>
        public Property? Property { get; set; } = null;

    }
}
