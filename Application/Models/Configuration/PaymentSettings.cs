namespace Application.Models.Configuration
{
    /// <summary>
    /// The "Payments" section: which payment methods are on and their merchant details. A method with empty details is off
    /// (the checkout answers 409 "not available"), so a missing secret never produces a half-working payment.
    /// </summary>
    public sealed class PaymentSettings
    {
        public const string SectionName = "Payments";

        /// <summary>Where the provider's page sends the payer back to (the web site's payment result page).</summary>
        public string ReturnUrl { get; set; } = string.Empty;

        public PaymeSettings Payme { get; set; } = new();
        public ClickSettings Click { get; set; } = new();
        public BankTransferSettings BankTransfer { get; set; } = new();

        public sealed class PaymeSettings
        {
            /// <summary>Merchant id from the Payme business cabinet.</summary>
            public string MerchantId { get; set; } = string.Empty;

            /// <summary>The cash desk key (password); Payme calls us with "Authorization: Basic base64(Paycom:Key)".</summary>
            public string Key { get; set; } = string.Empty;

            public string CheckoutUrl { get; set; } = "https://checkout.paycom.uz";

            public bool Enabled => !string.IsNullOrWhiteSpace(MerchantId) && !string.IsNullOrWhiteSpace(Key);
        }

        public sealed class ClickSettings
        {
            public string ServiceId { get; set; } = string.Empty;
            public string MerchantId { get; set; } = string.Empty;

            /// <summary>Secret key of the service; signs every request Click sends (MD5).</summary>
            public string SecretKey { get; set; } = string.Empty;

            public string CheckoutUrl { get; set; } = "https://my.click.uz/services/pay";

            public bool Enabled => !string.IsNullOrWhiteSpace(ServiceId) && !string.IsNullOrWhiteSpace(MerchantId) && !string.IsNullOrWhiteSpace(SecretKey);
        }

        public sealed class BankTransferSettings
        {
            /// <summary>Bank details and the payment purpose text shown to the payer. Empty: off.</summary>
            public string Instructions { get; set; } = string.Empty;

            public bool Enabled => !string.IsNullOrWhiteSpace(Instructions);
        }
    }
}
