using System;
using System.Collections.Generic;

namespace Application.Models.Results
{
    /// <summary>
    /// Challenge to be signed by the user's E-IMZO key.
    /// </summary>
    public sealed record EimzoChallengeResult(string Challenge, int TtlSeconds);

    /// <summary>
    /// A started E-IMZO Mobile session. The frontend builds the QR code / deeplink from <see cref="SiteId"/>, <see cref="DocumentId"/>
    /// and the hash of the challenge (sign-in) or of the document (signing) with e-imzo-mobile.js, and polls the status.
    /// </summary>
    public sealed record EimzoMobileSession(string SiteId, string DocumentId, string? Challenge);

    /// <summary>
    /// Where an E-IMZO Mobile session stands. <see cref="Status"/> is the E-IMZO-SERVER code:
    /// 1 = the phone has uploaded the signature, 2 = not yet (keep polling), -2 = unknown or expired.
    /// </summary>
    public sealed record EimzoMobileStatusResult(int Status, string Message)
    {
        public bool IsReady => Status == 1;
        public bool IsPending => Status == 2;
    }

    /// <summary>
    /// Result of verifying a document signed on the phone (/backend/mobile/verify).
    /// </summary>
    public sealed record EimzoMobileVerifyResult(
        bool IsSuccess,
        int Status,
        string Message,
        EimzoCertificateInfo? Certificate,
        string? Pkcs7Attached,
        string? SigningTime,
        string? TimestampedTime,
        string RawResponse);

    /// <summary>
    /// Owner data read from an E-IMZO certificate.
    /// </summary>
    public sealed record EimzoCertificateInfo
    {
        public string? SerialNumber { get; init; }

        /// <summary>
        /// Personal identification number of the key holder (14 digits).
        /// </summary>
        public string? Pinfl { get; init; }

        /// <summary>
        /// Individual taxpayer number of the key holder.
        /// </summary>
        public string? PersonInn { get; init; }

        /// <summary>
        /// Organisation taxpayer number (9 digits); present only in legal-entity keys.
        /// </summary>
        public string? LegalInn { get; init; }

        public string? CommonName { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? Organization { get; init; }
        public DateTime? ValidFrom { get; init; }
        public DateTime? ValidTo { get; init; }

        /// <summary>
        /// The key was issued to an organisation (its employee signs on its behalf).
        /// </summary>
        public bool IsLegalEntity => !string.IsNullOrWhiteSpace(LegalInn);
    }

    /// <summary>
    /// Result of checking a signed challenge. <see cref="Status"/> is the E-IMZO-SERVER status code (1 = success).
    /// </summary>
    public sealed record EimzoAuthResult(bool IsSuccess, int Status, string Message, EimzoCertificateInfo? Certificate);

    /// <summary>
    /// One signature of a verified document.
    /// </summary>
    public sealed record EimzoSignerInfo(
        EimzoCertificateInfo? Certificate,
        bool SignatureVerified,
        bool CertificateVerified,
        string? SigningTime,
        string? TimestampTime);

    /// <summary>
    /// Result of verifying a signed document. <see cref="RawResponse"/> is the full E-IMZO-SERVER answer,
    /// worth storing next to the document as evidence of the check.
    /// </summary>
    public sealed record EimzoDocumentVerificationResult(
        bool IsSuccess,
        int Status,
        string Message,
        string? DocumentBase64,
        IReadOnlyList<EimzoSignerInfo> Signers,
        string RawResponse);
}
