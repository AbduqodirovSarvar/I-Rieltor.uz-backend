using Domain.Enums;
using System.Globalization;

namespace Application.Notifications
{
    /// <summary>
    /// Texts of the notifications (Uzbek). <c>{name}</c> placeholders are filled from the data of the event.
    /// An email carries the same text as the SMS; <see cref="Sms"/> = false keeps a message for email only
    /// (for example "outbid", which can come many times in a minute and would be an expensive stream of SMS).
    /// </summary>
    public static class NotificationTemplates
    {
        public sealed record Template(string Subject, string Text, bool Sms = true);

        private static readonly Dictionary<NotificationType, Template> Templates = new()
        {
            [NotificationType.APPLICATION_APPROVED] = new("Ariza tasdiqlandi",
                "Lot {lot}: arizangiz tasdiqlandi. Ishtirokchi raqamingiz: {number}. Zakalat: {deposit} so'm. Savdo {start} da boshlanadi."),
            [NotificationType.APPLICATION_REJECTED] = new("Ariza rad etildi",
                "Lot {lot}: arizangiz rad etildi. Sabab: {reason}."),
            [NotificationType.DEPOSIT_RECEIVED] = new("Zakalat qabul qilindi",
                "Lot {lot}: zakalat ({amount} so'm) qabul qilindi. Savdoda qatnasha olasiz."),
            [NotificationType.AUCTION_STARTING_SOON] = new("Savdo tez orada boshlanadi",
                "Lot {lot}: savdo {start} da boshlanadi."),
            [NotificationType.OUTBID] = new("Taklifingiz oshirildi",
                "Lot {lot}: taklifingiz oshirildi. Joriy narx: {price} so'm.", Sms: false),
            [NotificationType.AUCTION_WON] = new("Siz g'olib bo'ldingiz",
                "Lot {lot}: tabriklaymiz, siz g'olib bo'ldingiz! Narx: {price} so'm. Zakalat ayirilgach to'lanadigan summa: {due} so'm. To'lov muddati: {deadline}."),
            [NotificationType.PAYMENT_DUE_SOON] = new("To'lov muddati yaqin",
                "Lot {lot}: yakuniy to'lov muddati {deadline} da tugaydi. To'lanishi kerak: {due} so'm."),
            [NotificationType.RUNNER_UP_OFFER] = new("Obyektni sotib olish taklifi",
                "Lot {lot}: g'olib to'lamadi. Siz obyektni {price} so'm ga sotib olishingiz mumkin. Javob muddati: {respondBy}."),
            [NotificationType.WINNER_DEFAULTED] = new("To'lov qilinmadi",
                "Lot {lot}: yakuniy to'lov muddatida qilinmadi, zakalat qaytarilmaydi."),
            [NotificationType.AUCTION_CANCELLED] = new("Auksion bekor qilindi",
                "Lot {lot} auksioni bekor qilindi. Sabab: {reason}. Zakalat qaytariladi."),
            [NotificationType.DEPOSIT_REFUNDED] = new("Zakalat qaytarildi",
                "Lot {lot}: zakalat ({amount} so'm) qaytarildi."),
            [NotificationType.PAYMENT_RECEIVED] = new("To'lov qabul qilindi",
                "Lot {lot}: to'lov qabul qilindi, obyekt sizga sotildi. Rahmat!"),
        };

        public static Template Get(NotificationType type) => Templates[type];

        /// <summary>
        /// Replaces every <c>{key}</c>. A key without a value, or a value nobody uses, is a bug in the caller and throws.
        /// </summary>
        public static string Render(string text, IReadOnlyDictionary<string, string> data)
        {
            var used = new HashSet<string>();
            var result = System.Text.RegularExpressions.Regex.Replace(text, @"\{([a-zA-Z]+)\}", match =>
            {
                var key = match.Groups[1].Value;
                if (!data.TryGetValue(key, out var value))
                    throw new InvalidOperationException($"Notification template placeholder '{key}' has no value.");
                used.Add(key);
                return value;
            });
            var unused = data.Keys.Where(k => !used.Contains(k)).ToList();
            if (unused.Count > 0)
                throw new InvalidOperationException($"Notification template has no placeholder for: {string.Join(", ", unused)}.");
            return result;
        }

        /// <summary>"103 000 000" — thousands separated by a space; whole so'm without tiyin.</summary>
        public static string Money(decimal amount)
            => decimal.Round(amount, 0, MidpointRounding.AwayFromZero).ToString("N0", CultureInfo.InvariantCulture).Replace(',', ' ');

        /// <summary>
        /// Time for people: Tashkent (UTC+5, no daylight saving), "21.10.2026 14:30".
        /// </summary>
        public static string Time(DateTime utc)
            => utc.AddHours(5).ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
    }
}
