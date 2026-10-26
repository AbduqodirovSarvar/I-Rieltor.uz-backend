namespace Application.Notifications
{
    /// <summary>
    /// Email texts. Values are filled in with <see cref="HtmlEmailBody.FromTemplate"/>, which HTML-encodes them.
    /// </summary>
    public static class EmailTemplates
    {
        public const string EmailConfirmationSubject = "I-Rieltor: email manzilingizni tasdiqlang";

        public const string ContactMessageSubject = "I-Rieltor: saytdan yangi xabar";

        /// <summary>Placeholders: name, phone, email, message.</summary>
        public const string ContactMessage = """
            <!DOCTYPE html>
            <html lang="uz">
            <body style="font-family: Arial, sans-serif; color: #1f2937; line-height: 1.5;">
              <p>Saytdagi aloqa shakli orqali yangi xabar keldi.</p>
              <p><strong>Ism:</strong> {{name}}<br><strong>Telefon:</strong> {{phone}}<br><strong>Email:</strong> {{email}}</p>
              <p style="white-space: pre-wrap;">{{message}}</p>
            </body>
            </html>
            """;

        public const string PasswordResetSubject = "I-Rieltor: parolni tiklash";

        /// <summary>Placeholders: name, link, minutes.</summary>
        public const string PasswordReset = """
            <!DOCTYPE html>
            <html lang="uz">
            <body style="font-family: Arial, sans-serif; color: #1f2937; line-height: 1.5;">
              <p>Assalomu alaykum, {{name}}!</p>
              <p>I-Rieltor hisobingiz parolini tiklash so'raldi. Yangi parol o'rnatish uchun quyidagi tugmani bosing:</p>
              <p>
                <a href="{{link}}" style="display: inline-block; padding: 10px 20px; background: #1d4ed8; color: #ffffff; text-decoration: none; border-radius: 6px;">Parolni tiklash</a>
              </p>
              <p>Tugma ishlamasa, havolani brauzerga ko'chiring:<br>{{link}}</p>
              <p>Havola {{minutes}} daqiqa amal qiladi va bir marta ishlaydi. Agar buni siz so'ramagan bo'lsangiz, xatni e'tiborsiz qoldiring: parolingiz o'zgarmaydi.</p>
            </body>
            </html>
            """;

        /// <summary>Placeholders: name, link, hours.</summary>
        public const string EmailConfirmation = """
            <!DOCTYPE html>
            <html lang="uz">
            <body style="font-family: Arial, sans-serif; color: #1f2937; line-height: 1.5;">
              <p>Assalomu alaykum, {{name}}!</p>
              <p>Bu email manzilini I-Rieltor hisobingizga bog'lash uchun quyidagi tugmani bosing:</p>
              <p>
                <a href="{{link}}" style="display: inline-block; padding: 10px 20px; background: #1d4ed8; color: #ffffff; text-decoration: none; border-radius: 6px;">Emailni tasdiqlash</a>
              </p>
              <p>Tugma ishlamasa, havolani brauzerga ko'chiring:<br>{{link}}</p>
              <p>Havola {{hours}} soat amal qiladi. Agar buni siz so'ramagan bo'lsangiz, xatni e'tiborsiz qoldiring.</p>
            </body>
            </html>
            """;
    }
}
