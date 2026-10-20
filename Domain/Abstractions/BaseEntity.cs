using System;

namespace Domain.Abstractions
{
    /// <summary>
    /// Barcha entity'lar uchun eng asosiy klass. Faqat birlamchi kalitni saqlaydi.
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// Yozuvning noyob identifikatori (birlamchi kalit, PK).
        /// Obyekt yaratilishi bilan avtomatik UUIDv7 beriladi: vaqt bo'yicha tartiblangan,
        /// shuning uchun yangi yozuvlar indeks oxiriga qo'shiladi va B-tree bo'linib ketmaydi.
        /// </summary>
        public Guid Id { get; init; } = Guid.CreateVersion7();
    }
}
