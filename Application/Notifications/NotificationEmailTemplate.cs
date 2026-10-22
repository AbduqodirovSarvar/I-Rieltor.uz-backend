namespace Application.Notifications
{
    public static class NotificationEmailTemplate
    {
        /// <summary>Placeholders: name, message. The message keeps its line breaks (pre-line).</summary>
        public const string Html = """
            <!DOCTYPE html>
            <html lang="uz">
            <body style="font-family: Arial, sans-serif; color: #1f2937; line-height: 1.5;">
              <p>Assalomu alaykum, {{name}}!</p>
              <p style="white-space: pre-line;">{{message}}</p>
              <p style="color: #6b7280; font-size: 13px;">I-Rieltor.uz</p>
            </body>
            </html>
            """;
    }
}
