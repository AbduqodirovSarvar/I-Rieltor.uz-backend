using System.Net;
using System.Text.RegularExpressions;

namespace Application.Notifications
{
    /// <summary>
    /// HTML body of an email. It can only be built from a template whose placeholders are HTML-encoded,
    /// so a name like <c>&lt;a href=...&gt;</c> entered by a user is shown as text and never becomes a link or markup.
    /// </summary>
    public sealed partial class HtmlEmailBody
    {
        public string Html { get; }

        private HtmlEmailBody(string html) => Html = html;

        /// <summary>
        /// Replaces every <c>{{key}}</c> in <paramref name="template"/> with the HTML-encoded value.
        /// The template is trusted markup written by developers; only the values come from users.
        /// A placeholder without a value, or a value without a placeholder, throws: both are template bugs.
        /// </summary>
        public static HtmlEmailBody FromTemplate(string template, IReadOnlyDictionary<string, string?> values)
        {
            ArgumentNullException.ThrowIfNull(template);
            ArgumentNullException.ThrowIfNull(values);

            var used = new HashSet<string>(StringComparer.Ordinal);
            var html = Placeholder().Replace(template, match =>
            {
                var key = match.Groups[1].Value;
                if (!values.TryGetValue(key, out var value))
                    throw new InvalidOperationException($"Email template placeholder '{key}' has no value.");
                used.Add(key);
                return WebUtility.HtmlEncode(value ?? string.Empty);
            });

            var unused = values.Keys.Where(k => !used.Contains(k)).ToList();
            if (unused.Count > 0)
                throw new InvalidOperationException($"Email template has no placeholder for: {string.Join(", ", unused)}.");

            return new HtmlEmailBody(html);
        }

        [GeneratedRegex(@"\{\{\s*([A-Za-z0-9_]+)\s*\}\}")]
        private static partial Regex Placeholder();
    }
}
