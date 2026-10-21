using Application.Models.Dtos.Abstractions;
using Domain.Behaviours;
using System.ComponentModel.DataAnnotations;

namespace Application.Models.Dtos.Auctions
{
    public record CreateAuctionDto : BaseCreateDto
    {
        /// <summary>Lot raqami; bo'sh qoldirilsa avtomatik beriladi (masalan 2026-000125).</summary>
        [MaxLength(30)]
        public string? LotNumber { get; set; }
        /// <summary>Sarlavha; bo'sh qoldirilsa obyekt sarlavhasi olinadi.</summary>
        [MaxLength(200)]
        public string? Title { get; set; }
        [MaxLength(10_000)]
        public string? Description { get; set; }
        [Required]
        [NotEmptyGuid]
        public Guid PropertyId { get; set; }
        /// <summary>Boshlang'ich narx, so'm.</summary>
        [Range(0.01, 1_000_000_000_000_000.0)]
        public decimal StartingPrice { get; set; }
        /// <summary>Eng kichik oshirish qadami, so'm.</summary>
        [Range(0.01, 1_000_000_000_000_000.0)]
        public decimal MinBidStep { get; set; }
        /// <summary>Zakalat, so'm; 0 = zakalatsiz.</summary>
        [Range(0.0, 1_000_000_000_000_000.0)]
        public decimal DepositAmount { get; set; }
        /// <summary>UTC ("Z") yoki offset bilan.</summary>
        public DateTime ApplicationDeadline { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        /// <summary>Oxirgi shuncha daqiqada taklif tushsa savdo shuncha cho'ziladi; 0 = cho'zilmaydi.</summary>
        [Range(0, 60)]
        public int ExtensionMinutes { get; set; }
        /// <summary>G'olib voz kechsa, obyekt ikkinchi o'ringa taklif qilinadimi.</summary>
        public bool OfferToRunnerUp { get; set; } = true;
    }

    /// <summary>To'liq almashtirish; faqat qoralama (DRAFT) auksionni o'zgartirish mumkin.</summary>
    public record UpdateAuctionDto : BaseUpdateDto
    {
        /// <summary>Lot raqami; bo'sh qoldirilsa avtomatik beriladi (masalan 2026-000125).</summary>
        [MaxLength(30)]
        public string? LotNumber { get; set; }
        /// <summary>Sarlavha; bo'sh qoldirilsa obyekt sarlavhasi olinadi.</summary>
        [MaxLength(200)]
        public string? Title { get; set; }
        [MaxLength(10_000)]
        public string? Description { get; set; }
        [Required]
        [NotEmptyGuid]
        public Guid PropertyId { get; set; }
        /// <summary>Boshlang'ich narx, so'm.</summary>
        [Range(0.01, 1_000_000_000_000_000.0)]
        public decimal StartingPrice { get; set; }
        /// <summary>Eng kichik oshirish qadami, so'm.</summary>
        [Range(0.01, 1_000_000_000_000_000.0)]
        public decimal MinBidStep { get; set; }
        /// <summary>Zakalat, so'm; 0 = zakalatsiz.</summary>
        [Range(0.0, 1_000_000_000_000_000.0)]
        public decimal DepositAmount { get; set; }
        /// <summary>UTC ("Z") yoki offset bilan.</summary>
        public DateTime ApplicationDeadline { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        /// <summary>Oxirgi shuncha daqiqada taklif tushsa savdo shuncha cho'ziladi; 0 = cho'zilmaydi.</summary>
        [Range(0, 60)]
        public int ExtensionMinutes { get; set; }
        /// <summary>G'olib voz kechsa, obyekt ikkinchi o'ringa taklif qilinadimi.</summary>
        public bool OfferToRunnerUp { get; set; } = true;
    }

    public record DeleteAuctionDto : BaseDeleteDto
    {
    }

    public record CancelAuctionDto
    {
        [Required]
        [NotEmptyGuid]
        public Guid Id { get; set; }
        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
    }
}
