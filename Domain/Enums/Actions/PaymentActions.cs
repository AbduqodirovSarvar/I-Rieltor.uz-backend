namespace Domain.Enums.Actions
{
    /// <summary>
    /// To'lovlar bilan ishlash. Foydalanuvchi o'z to'lovini ruxsatsiz boshlaydi va ko'radi.
    /// CREATE_PAYMENT: kelib tushgan to'lovni qo'lda kiritish; UPDATE_PAYMENT: bank o'tkazmasini tasdiqlash va
    /// zakalat qaytarilganini belgilash.
    /// </summary>
    public enum PaymentActions
    {
        CREATE_PAYMENT,
        READ_PAYMENT,
        UPDATE_PAYMENT,
    }
}
