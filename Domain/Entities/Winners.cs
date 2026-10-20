using Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    /// <summary>
    /// Auksion g'olibi va u bo'yicha to'lov holati.
    /// </summary>
    public class Winners : AudiTableEntity
    {
        /// <summary>
        /// G'olib aniqlangan auksionning Id'si (FK).
        /// </summary>
        public Guid AuctionId { get; set; }

        /// <summary>
        /// G'olib aniqlangan auksion (navigatsiya).
        /// </summary>
        public Auction? Auction { get; set; } = null;

        /// <summary>
        /// G'olib foydalanuvchining Id'si (FK).
        /// </summary>
        public Guid WinnerId { get; set; }

        /// <summary>
        /// G'olib foydalanuvchi (navigatsiya).
        /// </summary>
        public User? Winner { get; set; } = null;

        /// <summary>
        /// G'olib bo'lgan taklif summasi, ya'ni obyektning sotilish narxi.
        /// </summary>
        public decimal WinningBid { get; set; }

        /// <summary>
        /// G'olib hali to'lashi kerak bo'lgan qoldiq summa.
        /// </summary>
        public decimal RemainingPayment { get; set; }

        /// <summary>
        /// G'olib shu paytgacha to'lagan umumiy summa.
        /// </summary>
        public decimal TotalPayment { get; set; }

        /// <summary>
        /// To'lov holati, matn ko'rinishida (<see cref="Enums.PaymentStatus"/> qiymatlari).
        /// </summary>
        public string PaymentStatus { get; set; } = string.Empty;
    }
}
