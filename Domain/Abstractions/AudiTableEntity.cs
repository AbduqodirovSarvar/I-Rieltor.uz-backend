using System;

namespace Domain.Abstractions
{
    /// <summary>
    /// Audit maydonlariga ega entity'lar uchun asosiy klass.
    /// Kim va qachon yaratgani, o'zgartirgani hamda o'chirilganlik belgisini saqlaydi.
    /// Maydonlarni <c>AuditableEntitySaveChangesInterceptor</c> avtomatik to'ldiradi.
    /// </summary>
    public abstract class AudiTableEntity : BaseEntity
    {
        /// <summary>
        /// Yozuv yaratilgan vaqt (UTC). Saqlashda avtomatik qo'yiladi.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Yozuvni yaratgan foydalanuvchining Id'si.
        /// Tizim yoki anonim foydalanuvchi yaratgan bo'lsa null.
        /// </summary>
        public Guid? CreatedBy { get; set; }

        /// <summary>
        /// Oxirgi o'zgartirilgan vaqt (UTC). Hech o'zgartirilmagan bo'lsa null.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Oxirgi marta o'zgartirgan foydalanuvchining Id'si. Hech o'zgartirilmagan bo'lsa null.
        /// </summary>
        public Guid? UpdatedBy { get; set; }

        /// <summary>
        /// Soft delete belgisi: true bo'lsa yozuv o'chirilgan hisoblanadi,
        /// lekin bazadan jismonan o'chirilmaydi.
        /// </summary>
        public bool IsDeleted { get; set; } = false;
    }
}
