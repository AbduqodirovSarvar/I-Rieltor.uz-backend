using Domain.Abstractions;
using Domain.Behaviours;
using Domain.Enums;

namespace Domain.Entities
{
    /// <summary>
    /// Bitta obyekt bo'yicha savdo (lot). Narx va qoidalar shu yerda, har auksion uchun alohida.
    /// Barcha vaqtlar UTC.
    /// </summary>
    public class Auction : AudiTableEntity
    {
        /// <summary>
        /// Lot raqami (masalan "2026-000125"). Takrorlanmaydi.
        /// </summary>
        [Searchable]
        public string LotNumber { get; set; } = string.Empty;

        /// <summary>
        /// Sarlavha (odatda obyekt sarlavhasi).
        /// </summary>
        [Searchable]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Savdo shartlari, qo'shimcha ma'lumot.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Sotiladigan obyekt Id'si (FK). Obyektda bir vaqtda faqat bitta faol (qoralama, e'lon qilingan
        /// yoki ketayotgan) auksion bo'ladi.
        /// </summary>
        public Guid PropertyId { get; set; }

        /// <summary>
        /// Obyekt (navigatsiya).
        /// </summary>
        public Property? Property { get; set; }

        /// <summary>
        /// Holat.
        /// </summary>
        public AuctionStatus Status { get; set; } = AuctionStatus.DRAFT;

        /// <summary>
        /// Boshlang'ich narx, so'm. Musbat.
        /// </summary>
        public decimal StartingPrice { get; set; }

        /// <summary>
        /// Eng kichik oshirish qadami, so'm: yangi taklif joriy narxdan kamida shuncha katta bo'lishi kerak. Musbat.
        /// </summary>
        public decimal MinBidStep { get; set; }

        /// <summary>
        /// Qatnashish uchun zakalat, so'm. 0 = zakalatsiz.
        /// </summary>
        public decimal DepositAmount { get; set; }

        /// <summary>
        /// Arizalar qabul qilinishining oxirgi vaqti. Savdo boshlanishidan kech emas.
        /// </summary>
        public DateTime ApplicationDeadline { get; set; }

        /// <summary>
        /// Savdo boshlanish vaqti.
        /// </summary>
        public DateTime StartsAt { get; set; }

        /// <summary>
        /// Savdo tugash vaqti; <see cref="ExtensionMinutes"/> bo'yicha cho'zilishi mumkin. Boshlanishidan keyin.
        /// </summary>
        public DateTime EndsAt { get; set; }

        /// <summary>
        /// Oxirgi daqiqalarda taklif tushsa, savdo shuncha daqiqaga cho'ziladi ("so'nggi soniya" taklifiga qarshi).
        /// 0 = cho'zilmaydi.
        /// </summary>
        public int ExtensionMinutes { get; set; }

        /// <summary>
        /// Joriy (eng yuqori) narx. Taklif yo'q bo'lsa null. Taklif qabul qilinganda yangilanadi; auksion qatorining
        /// versiyasi (xmin) bir vaqtdagi ikki taklifdan faqat bittasini o'tkazadi.
        /// </summary>
        public decimal? CurrentPrice { get; set; }

        /// <summary>
        /// Eng yuqori taklif Id'si.
        /// </summary>
        public Guid? LeadingBidId { get; set; }

        /// <summary>
        /// G'olib voz kechsa yoki to'lamasa, obyekt ikkinchi o'rindagi ishtirokchiga uning eng yuqori taklifi
        /// narxida taklif qilinadimi.
        /// </summary>
        public bool OfferToRunnerUp { get; set; } = true;

        /// <summary>
        /// Bekor qilinganda sabab.
        /// </summary>
        public string? CancelReason { get; set; }

        /// <summary>
        /// Qatnashish arizalari.
        /// </summary>
        public ICollection<AuctionParticipant> Participants { get; set; } = [];
    }
}
