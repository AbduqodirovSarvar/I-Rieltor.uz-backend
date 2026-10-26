using Application.Exceptions;
using Application.Interfaces.Auths;
using Application.Interfaces.Cachings;
using Application.Interfaces.EntityServices;
using Application.Interfaces.Notifications;
using Application.Interfaces.Repositories;
using Application.Models.Dtos.Auths;
using Application.Models.Results;
using Application.Models.ViewModels.Auths;
using Application.Models.ViewModels.Users;
using Application.Notifications;
using MapsterMapper;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Auths
{
    public class AuthService(
        ITokenService tokenService,
        IEmailService emailService,
        IHashService hashService,
        IUserRepository userRepository,
        IMapper mapper,
        IEimzoService eimzoService,
        IUserRoleRepository userRoleRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ICurrentUserService currentUserService,
        IEmailConfirmationTokenService emailConfirmationTokenService,
        IPasswordResetTokenService passwordResetTokenService,
        IUnitOfWork unitOfWork,
        IRedisService redisService
        ) : IAuthService
    {
        private const string InvalidCredentialsMessage = "Invalid login or password.";
        private const string InvalidRefreshTokenMessage = "Session has expired. Sign in again.";
        // A rotated token presented again this soon is a client retry or a second tab, not theft.
        private static readonly TimeSpan RefreshReuseGracePeriod = TimeSpan.FromSeconds(30);

        private const string InvalidConfirmationLinkMessage = "The confirmation link is invalid or has expired.";

        // Hash used when the login is unknown, so a failed sign-in takes the same time either way.
        private static string? _dummyPasswordHash;

        private readonly ITokenService _tokenService = tokenService;
        private readonly IEmailService _emailService = emailService;
        private readonly IHashService _hashService = hashService;
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IMapper _mapper = mapper;
        private readonly IEimzoService _eimzoService = eimzoService;
        private readonly IUserRoleRepository _userRoleRepository = userRoleRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository = refreshTokenRepository;
        private readonly ICurrentUserService _currentUserService = currentUserService;
        private readonly IEmailConfirmationTokenService _emailConfirmationTokenService = emailConfirmationTokenService;
        private readonly IPasswordResetTokenService _passwordResetTokenService = passwordResetTokenService;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IRedisService _redisService = redisService;

        // Per user, on top of the per-IP rate limit: anyone signed in could otherwise mail any address through our
        // sender and use up its daily quota (Gmail ~500/day), so real confirmation emails would stop going out.
        private static readonly TimeSpan ConfirmationEmailCooldown = TimeSpan.FromSeconds(60);
        private const int ConfirmationEmailsPerDay = 10;
        public async Task<TokenViewModel> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken)
        {
            var identifier = loginDto.Login.Trim();
            var user = await _userRepository
                                .Query(x =>
                                    x.Login == identifier ||
                                    x.Email == identifier ||
                                    x.INNPINFL == identifier, cancellationToken)
                                .Include(x => x.UserRole)
                                    .ThenInclude(x => x!.Permissions)
                                .FirstOrDefaultAsync(cancellationToken);

            // An account without a password (E-IMZO only) costs the same hash work as an unknown login, so the response
            // time does not tell whether an account exists for a login, email or PINFL.
            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                _hashService.Verify(loginDto.Password, _dummyPasswordHash ??= _hashService.Hash(Guid.NewGuid().ToString()));
                throw new UnauthenticatedException(InvalidCredentialsMessage);
            }

            if (!_hashService.Verify(loginDto.Password, user.PasswordHash))
            {
                throw new UnauthenticatedException(InvalidCredentialsMessage);
            }

            return await IssueTokensAsync(user, cancellationToken);
        }

        public async Task<bool> RegisterAsync(RegisterDto registerDto, CancellationToken cancellationToken)
        {
            if (SystemRoles.IsReservedLogin(registerDto.Login))
                throw new BadRequestException("This login is reserved.");

            if (await _userRepository.ExistsWithAnyAsync(registerDto.Login, registerDto.Email, null, registerDto.Phone, cancellationToken: cancellationToken))
            {
                throw new ConflictException("User with this login, email or phone already exists.");
            }

            var user = _mapper.Map<User>(registerDto);
            // Unverified identity: only an E-IMZO certificate may make an account INDIVIDUAL or LEGAL.
            user.Type = ClientType.NONE;
            user.PasswordHash = _hashService.Hash(registerDto.Password);
            user.UserRoleId = await GetDefaultRoleIdAsync(cancellationToken);

            var created = await _userRepository.CreateAsync(user, cancellationToken) != null;

            // Registration must not depend on the mail server: a failure is logged and the user can resend the link.
            if (!string.IsNullOrWhiteSpace(user.Email))
                await _unitOfWork.AfterCommitAsync(() => SendConfirmationLinkAsync(user, CancellationToken.None));

            return created;
        }

        public async Task<TokenViewModel> LoginViaEimzoAsync(LoginViaEimzoDto loginDto, CancellationToken cancellationToken)
            => await LoginWithCertificateAsync(await AuthenticateWithEimzoAsync(loginDto.SignedData, cancellationToken), cancellationToken);

        public async Task<TokenViewModel> LoginViaEimzoMobileAsync(string documentId, CancellationToken cancellationToken)
            => await LoginWithCertificateAsync(await AuthenticateMobileWithEimzoAsync(documentId, cancellationToken), cancellationToken);

        private async Task<TokenViewModel> LoginWithCertificateAsync(EimzoCertificateInfo certificate, CancellationToken cancellationToken)
        {
            var identifier = GetEimzoAccountIdentifier(certificate);

            var user = await _userRepository
                                    .Query(x => x.INNPINFL == identifier, cancellationToken)
                                    .Include(x => x.UserRole)
                                        .ThenInclude(x => x!.Permissions)
                                    .FirstOrDefaultAsync(cancellationToken)
                                    ?? throw new UnauthenticatedException("User not registered.");

            return await IssueTokensAsync(user, cancellationToken);
        }

        public Task<EimzoChallengeResult> GetEimzoChallengeAsync(CancellationToken cancellationToken)
            => _eimzoService.GetChallengeAsync(cancellationToken);

        /// <summary>
        /// Ends the session the refresh token belongs to. Always succeeds, so the answer
        /// does not tell a caller whether a token exists. The access token stays valid until it expires.
        /// </summary>
        public async Task<bool> LogOutAsync(string token, CancellationToken cancellationToken)
        {
            var stored = await _refreshTokenRepository.GetByHashAsync(_tokenService.HashRefreshToken(token), cancellationToken);
            if (stored != null)
                await _refreshTokenRepository.TryRevokeAsync(stored.Id, null, DateTime.UtcNow, cancellationToken);

            return true;
        }

        /// <summary>
        /// Exchanges a refresh token for a new access token and a new refresh token (rotation).
        /// Permissions are re-read, so role changes reach the user at the next refresh.
        /// </summary>
        public async Task<TokenViewModel> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var stored = await _refreshTokenRepository.GetByHashAsync(_tokenService.HashRefreshToken(refreshToken), cancellationToken)
                            ?? throw new UnauthenticatedException(InvalidRefreshTokenMessage);

            // Rows created before sessions existed have no session values; each counts as its own session.
            var sessionId = stored.SessionId == Guid.Empty ? stored.Id : stored.SessionId;
            var sessionExpiresAt = stored.SessionExpiresAt == default ? stored.ExpiresAt : stored.SessionExpiresAt;

            // Expired tokens are refused without side effects, so an old leaked token cannot be used to log anyone out.
            if (stored.ExpiresAt <= now || sessionExpiresAt <= now)
                throw new UnauthenticatedException(InvalidRefreshTokenMessage);

            if (stored.RevokedAt != null)
            {
                // Only a token that was exchanged for a newer one and comes back later means someone holds a copy:
                // end that session, so the copy and the tokens issued from it stop working. Tokens revoked by
                // logout or "sign out everywhere" are simply refused.
                if (stored.ReplacedByTokenId != null && now - stored.RevokedAt.Value > RefreshReuseGracePeriod)
                    await _refreshTokenRepository.RevokeSessionAsync(stored.UserId, sessionId, now, cancellationToken);
                throw new UnauthenticatedException(InvalidRefreshTokenMessage);
            }

            var user = await _userRepository
                                .Query(x => x.Id == stored.UserId, cancellationToken)
                                .Include(x => x.UserRole)
                                    .ThenInclude(x => x!.Permissions)
                                .FirstOrDefaultAsync(cancellationToken)
                                ?? throw new UnauthenticatedException(InvalidRefreshTokenMessage);

            // Revoking the old token and storing the new one commit together: a failed insert does not sign the user out,
            // and a concurrent request with the same token waits on the row lock and then finds it revoked.
            return await _unitOfWork.ExecuteAsync(async () =>
            {
                var newTokenId = Guid.CreateVersion7();
                // Another request exchanged the same token a moment earlier (second tab, retry): refuse this one only.
                if (!await _refreshTokenRepository.TryRevokeAsync(stored.Id, newTokenId, now, cancellationToken))
                    throw new UnauthenticatedException(InvalidRefreshTokenMessage);

                return await IssueTokensAsync(user, cancellationToken, new SessionToken(newTokenId, sessionId, sessionExpiresAt));
            }, cancellationToken);
        }

        /// <summary>
        /// Continues an existing session on rotation; null starts a new session (sign-in).
        /// </summary>
        private sealed record SessionToken(Guid TokenId, Guid SessionId, DateTime SessionExpiresAt);

        public async Task<bool> RegisterViaEimzoAsync(RegisterViaEimzoDto registerDto, CancellationToken cancellationToken)
            => await RegisterWithCertificateAsync(await AuthenticateWithEimzoAsync(registerDto.SignedData, cancellationToken), cancellationToken);

        public async Task<bool> RegisterViaEimzoMobileAsync(string documentId, CancellationToken cancellationToken)
            => await RegisterWithCertificateAsync(await AuthenticateMobileWithEimzoAsync(documentId, cancellationToken), cancellationToken);

        private async Task<bool> RegisterWithCertificateAsync(EimzoCertificateInfo certificate, CancellationToken cancellationToken)
        {
            var identifier = GetEimzoAccountIdentifier(certificate);

            if (await _userRepository.ExistsWithAnyAsync(null, null, identifier, null, cancellationToken: cancellationToken))
                throw new ConflictException("User already registered.");

            var personName = string.Join(' ', new[] { certificate.LastName, certificate.FirstName }.Where(x => !string.IsNullOrWhiteSpace(x)));
            if (string.IsNullOrWhiteSpace(personName))
                personName = certificate.CommonName ?? string.Empty;

            var user = new User
            {
                INNPINFL = identifier,
                // E-IMZO users have no email until they add one in their profile.
                Email = null,
                UserRoleId = await GetDefaultRoleIdAsync(cancellationToken)
            };

            if (certificate.IsLegalEntity)
            {
                user.Type = ClientType.LEGAL;
                user.LegalName = certificate.Organization;
                user.FullName = certificate.Organization ?? personName;
            }
            else
            {
                user.Type = ClientType.INDIVIDUAL;
                user.FullName = personName;
            }

            return await _userRepository.CreateAsync(user, cancellationToken) != null;
        }

        /// <summary>
        /// Checks a signed challenge with E-IMZO-SERVER and returns the signer's certificate.
        /// </summary>
        private async Task<EimzoCertificateInfo> AuthenticateWithEimzoAsync(string signedChallenge, CancellationToken cancellationToken)
        {
            var result = await _eimzoService.AuthenticateAsync(signedChallenge, cancellationToken);
            if (!result.IsSuccess || result.Certificate == null)
                throw new UnauthenticatedException($"E-IMZO: {result.Message}");

            return result.Certificate;
        }

        /// <summary>
        /// A legal-entity key acts for the organisation, so it maps to the organisation's account (INN);
        /// a personal key maps to the person's account (PINFL).
        /// </summary>
        private static string GetEimzoAccountIdentifier(EimzoCertificateInfo certificate)
        {
            var identifier = certificate.IsLegalEntity ? certificate.LegalInn : certificate.Pinfl;
            if (string.IsNullOrWhiteSpace(identifier))
                throw new UnauthenticatedException("E-IMZO: the certificate contains neither PINFL nor organisation INN.");

            return identifier;
        }

        /// <summary>
        /// Starts a session: a short-lived access token plus a refresh token whose hash is stored.
        /// <paramref name="user"/> must be loaded with its role and permissions.
        /// </summary>
        private async Task<TokenViewModel> IssueTokensAsync(User user, CancellationToken cancellationToken, SessionToken? session = null)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Role, user.UserRole?.Name?.ToString() ?? "NONE")
            };

            user.UserRole?.Permissions?.ToList().ForEach(permission =>
            {
                claims.Add(new Claim("permission", permission.Permission.ToString()));
            });
            var (accessToken, accessTokenExpiresAt) = _tokenService.GenerateAccessToken([.. claims]);

            var now = DateTime.UtcNow;
            var tokenId = session?.TokenId ?? Guid.CreateVersion7();
            var sessionExpiresAt = session?.SessionExpiresAt ?? now.Add(_tokenService.SessionLifetime);
            // Each refresh extends the token, never past the end of the session.
            var candidateExpiresAt = now.Add(_tokenService.RefreshTokenLifetime);
            var refreshTokenExpiresAt = candidateExpiresAt < sessionExpiresAt ? candidateExpiresAt : sessionExpiresAt;

            var refreshToken = _tokenService.GenerateRefreshToken();
            await _refreshTokenRepository.CreateAsync(new RefreshToken
            {
                Id = tokenId,
                UserId = user.Id,
                TokenHash = _tokenService.HashRefreshToken(refreshToken),
                ExpiresAt = refreshTokenExpiresAt,
                SessionId = session?.SessionId ?? tokenId,
                SessionExpiresAt = sessionExpiresAt,
                CreatedByIp = _currentUserService.IpAddress
            }, cancellationToken);

            return new TokenViewModel
            {
                AccessToken = accessToken,
                AccessTokenExpiration = accessTokenExpiresAt,
                RefreshToken = refreshToken,
                RefreshTokenExpiration = refreshTokenExpiresAt,
                TokenType = "Bearer",
                UserId = user.Id.ToString()
            };
        }

        /// <summary>
        /// Read straight from the repository: registration is anonymous, so the
        /// permission-checked role service would always refuse.
        /// </summary>
        private async Task<Guid?> GetDefaultRoleIdAsync(CancellationToken cancellationToken)
        {
            var role = await _userRoleRepository.GetAsync(x => x.Name == SystemRoles.DefaultUser, cancellationToken);
            return role?.Id;
        }

        /// <summary>
        /// A signed challenge proves who the user is, so it can set a new password without email confirmation.
        /// Also lets users who registered through E-IMZO add a password.
        /// </summary>
        public async Task<bool> ResetViaEimzoAsync(ResetPasswordByEImzoDto resetPasswordDto, CancellationToken cancellationToken)
            => await ResetWithCertificateAsync(await AuthenticateWithEimzoAsync(resetPasswordDto.SignedData, cancellationToken), resetPasswordDto.NewPassword, cancellationToken);

        public async Task<bool> ResetViaEimzoMobileAsync(string documentId, string newPassword, CancellationToken cancellationToken)
            => await ResetWithCertificateAsync(await AuthenticateMobileWithEimzoAsync(documentId, cancellationToken), newPassword, cancellationToken);

        private async Task<bool> ResetWithCertificateAsync(EimzoCertificateInfo certificate, string newPassword, CancellationToken cancellationToken)
        {
            var identifier = GetEimzoAccountIdentifier(certificate);

            var user = await _userRepository.GetAsync(x => x.INNPINFL == identifier, cancellationToken)
                            ?? throw new NotFoundException("User not registered.");

            user.PasswordHash = _hashService.Hash(newPassword);

            // The new password and the end of the old sessions commit together: never one without the other.
            return await _unitOfWork.ExecuteAsync(async () =>
            {
                var updated = await _userRepository.UpdateAsync(user, cancellationToken) != null;
                // Whoever knew the old password must not stay signed in.
                await _refreshTokenRepository.RevokeAllActiveAsync(user.Id, DateTime.UtcNow, cancellationToken);
                return updated;
            }, cancellationToken);
        }

        /// <summary>
        /// Profile settings: sets a new email, unconfirmed until the user opens the link sent to it.
        /// The link is sent before the commit, so if the mail server is down nothing changes (503) and the user can retry.
        /// </summary>
        public Task<bool> ChangeMyEmailAsync(string email, CancellationToken cancellationToken)
            => _unitOfWork.ExecuteAsync(async () =>
            {
                var user = await GetSignedInUserAsync(cancellationToken);
                email = email.Trim();

                if (string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    if (user.EmailConfirmedAt != null)
                        throw new BadRequestException("This email is already confirmed.");
                }
                else
                {
                    // Users.Email is citext: the comparison ignores letter case.
                    if (await _userRepository.AnyAsync(u => u.Id != user.Id && u.Email == email, cancellationToken))
                        throw new ConflictException("This email is used by another account.");

                    user.Email = email;
                    user.EmailConfirmedAt = null;
                    await _userRepository.UpdateAsync(user, cancellationToken);
                }

                await SendConfirmationLinkAsync(user, cancellationToken);
                return true;
            }, cancellationToken);

        /// <summary>
        /// Contact details of the signed-in user. The name of an E-IMZO-verified account is the key's and stays; a phone is
        /// unique among accounts, as at sign-up.
        /// </summary>
        public Task<UserViewModel> UpdateMyProfileAsync(Application.Features.Auths.Commands.UpdateProfile.UpdateMyProfileCommand profile, CancellationToken cancellationToken)
            => _unitOfWork.ExecuteAsync(async () =>
            {
                var user = await GetSignedInUserAsync(cancellationToken);

                if (profile.FullName != null)
                {
                    var name = string.Join(' ', profile.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                    if (name.Length == 0)
                        throw new BadRequestException("The name cannot be empty.");
                    if (user.Type != ClientType.NONE && !string.Equals(name, user.FullName, StringComparison.Ordinal))
                        throw new BadRequestException("The name of an account verified with E-IMZO comes from the key and cannot be changed.");
                    user.FullName = name;
                }

                if (profile.Phone != null)
                {
                    var phone = profile.Phone.Trim();
                    if (phone.Length > 0 && await _userRepository.AnyAsync(u => u.Id != user.Id && u.Phone == phone, cancellationToken))
                        throw new ConflictException("This phone number is used by another account.");
                    user.Phone = phone.Length == 0 ? null : phone;
                }
                if (profile.SecondaryPhone != null)
                    user.SecondaryPhone = profile.SecondaryPhone.Trim().Length == 0 ? null : profile.SecondaryPhone.Trim();
                if (profile.Address != null)
                    user.Address = profile.Address.Trim().Length == 0 ? null : profile.Address.Trim();

                await _userRepository.UpdateAsync(user, cancellationToken);
                return _mapper.Map<UserViewModel>(user);
            }, cancellationToken);

        /// <summary>
        /// Needs the current password, so a stolen access token alone cannot take the account over. An account without
        /// a password (E-IMZO only) sets one with reset-password-via-eimzo instead, which proves the person by signature.
        /// All sessions end, the caller receives a new token pair.
        /// </summary>
        public async Task<TokenViewModel> ChangeMyPasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken)
        {
            var signedIn = await GetSignedInUserAsync(cancellationToken);
            if (string.IsNullOrEmpty(signedIn.PasswordHash))
                throw new BadRequestException("This account has no password yet. Set one with reset-password-via-eimzo.");
            if (!_hashService.Verify(currentPassword, signedIn.PasswordHash))
                throw new BadRequestException("The current password is incorrect.");
            if (_hashService.Verify(newPassword, signedIn.PasswordHash))
                throw new BadRequestException("The new password must differ from the current one.");

            // Only for the claims of the new access token. It is not saved: Update() on it would also write the
            // attached role and permission rows.
            var withPermissions = await _userRepository
                                .Query(x => x.Id == signedIn.Id, cancellationToken)
                                .Include(x => x.UserRole)
                                    .ThenInclude(x => x!.Permissions)
                                .FirstAsync(cancellationToken);

            return await _unitOfWork.ExecuteAsync(async () =>
            {
                signedIn.PasswordHash = _hashService.Hash(newPassword);
                await _userRepository.UpdateAsync(signedIn, cancellationToken);
                // Whoever knew the old password, on any device, is signed out; this device continues with new tokens.
                await _refreshTokenRepository.RevokeAllActiveAsync(signedIn.Id, DateTime.UtcNow, cancellationToken);
                return await IssueTokensAsync(withPermissions, cancellationToken);
            }, cancellationToken);
        }

        public async Task<bool> ResendEmailConfirmationAsync(CancellationToken cancellationToken)
        {
            var user = await GetSignedInUserAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(user.Email))
                throw new BadRequestException("No email is set. Add one in the profile settings.");
            if (user.EmailConfirmedAt != null)
                throw new BadRequestException("This email is already confirmed.");

            await SendConfirmationLinkAsync(user, cancellationToken);
            return true;
        }

        /// <summary>
        /// Opening the link twice is fine. A link for an email the user has since changed, or for a deleted user, is refused.
        /// </summary>
        public async Task<bool> ConfirmEmailAsync(string token, CancellationToken cancellationToken)
        {
            if (!_emailConfirmationTokenService.TryValidate(token, out var userId, out var email))
                throw new BadRequestException(InvalidConfirmationLinkMessage);

            var user = await _userRepository.GetAsync(x => x.Id == userId, cancellationToken);
            if (user?.Email == null || !string.Equals(user.Email.Trim(), email, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException(InvalidConfirmationLinkMessage);

            if (user.EmailConfirmedAt == null)
            {
                user.EmailConfirmedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user, cancellationToken);
            }
            return true;
        }

        private static readonly TimeSpan ResetEmailCooldown = TimeSpan.FromMinutes(2);
        private const int ResetEmailsPerDay = 5;
        private const string InvalidResetLinkMessage = "The reset link is invalid or has expired. Ask for a new one.";

        public async Task ForgotPasswordAsync(string email, CancellationToken cancellationToken)
        {
            var normalized = email.Trim().ToLower();
            var user = await _userRepository.GetAsync(
                x => x.Email != null && x.Email.ToLower() == normalized && x.EmailConfirmedAt != null,
                cancellationToken);
            // Nothing tells the caller whether the address is known: no account, an unconfirmed email and a limit that
            // is reached all end the same way as a sent mail.
            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
                return;

            if (!await _redisService.TryAcquireAsync($"v1:pwreset:cooldown:{user.Id}", ResetEmailCooldown))
                return;
            if (await _redisService.IncrementAsync($"v1:pwreset:day:{user.Id}:{DateTime.UtcNow:yyyyMMdd}", TimeSpan.FromDays(1)) > ResetEmailsPerDay)
                return;

            try
            {
                var (url, expiresAt) = _passwordResetTokenService.CreateLink(user.Id, user.PasswordHash);
                var minutes = Math.Max(1, (int)Math.Round((expiresAt - DateTime.UtcNow).TotalMinutes));
                var body = HtmlEmailBody.FromTemplate(EmailTemplates.PasswordReset, new Dictionary<string, string?>
                {
                    ["name"] = user.FullName,
                    ["link"] = url,
                    ["minutes"] = minutes.ToString()
                });
                await _emailService.SendEmailAsync(user.Email!, EmailTemplates.PasswordResetSubject, body, cancellationToken);
            }
            catch (ServiceUnavailableException)
            {
                // The mail server is down or the link is not configured: the person can ask again, and the answer
                // must not differ from the one for an unknown address.
                await _redisService.RemoveAsync($"v1:pwreset:cooldown:{user.Id}");
            }
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken)
        {
            if (!_passwordResetTokenService.TryValidate(token, out var userId, out var fingerprint))
                throw new BadRequestException(InvalidResetLinkMessage);

            var user = await _userRepository.GetAsync(x => x.Id == userId, cancellationToken);
            if (user == null || string.IsNullOrEmpty(user.PasswordHash)
                || !string.Equals(_passwordResetTokenService.Fingerprint(user.PasswordHash), fingerprint, StringComparison.Ordinal))
                throw new BadRequestException(InvalidResetLinkMessage);

            user.PasswordHash = _hashService.Hash(newPassword);
            return await _unitOfWork.ExecuteAsync(async () =>
            {
                var updated = await _userRepository.UpdateAsync(user, cancellationToken) != null;
                // Whoever held the old password, or the stolen mailbox, must not stay signed in.
                await _refreshTokenRepository.RevokeAllActiveAsync(user.Id, DateTime.UtcNow, cancellationToken);
                return updated;
            }, cancellationToken);
        }

        public async Task<UserViewModel> LinkEimzoAsync(string signedData, CancellationToken cancellationToken)
            => await LinkAsync(await AuthenticateWithEimzoAsync(signedData, cancellationToken), cancellationToken);

        public async Task<UserViewModel> LinkEimzoMobileAsync(string documentId, CancellationToken cancellationToken)
            => await LinkAsync(await AuthenticateMobileWithEimzoAsync(documentId, cancellationToken), cancellationToken);

        /// <summary>
        /// A signature proves the person holds the key, so it may set PINFL/INN where the admin forms may not.
        /// One key per account and one account per key; unlinking is not offered (it would let a stolen session
        /// detach the account from its owner's key).
        /// </summary>
        private async Task<UserViewModel> LinkAsync(EimzoCertificateInfo certificate, CancellationToken cancellationToken)
        {
            var user = await GetSignedInUserAsync(cancellationToken);
            var identifier = GetEimzoAccountIdentifier(certificate);

            if (user.INNPINFL != null)
            {
                if (user.INNPINFL == identifier)
                    return _mapper.Map<UserViewModel>(user);
                throw new ConflictException("This account is already linked to another E-IMZO key.");
            }
            if (await _userRepository.AnyAsync(u => u.INNPINFL == identifier && u.Id != user.Id, cancellationToken))
                throw new ConflictException("This E-IMZO key is already linked to another account.");

            user.INNPINFL = identifier;
            user.Type = certificate.IsLegalEntity ? ClientType.LEGAL : ClientType.INDIVIDUAL;
            if (certificate.IsLegalEntity && !string.IsNullOrWhiteSpace(certificate.Organization))
                user.LegalName = certificate.Organization;
            await _userRepository.UpdateAsync(user, cancellationToken);
            return _mapper.Map<UserViewModel>(user);
        }

        /// <summary>
        /// Reads the signer of a finished E-IMZO Mobile sign-in session.
        /// </summary>
        private async Task<EimzoCertificateInfo> AuthenticateMobileWithEimzoAsync(string documentId, CancellationToken cancellationToken)
        {
            var result = await _eimzoService.AuthenticateMobileAsync(documentId, cancellationToken);
            if (!result.IsSuccess || result.Certificate == null)
                throw new UnauthenticatedException($"E-IMZO: {result.Message}");
            return result.Certificate;
        }

        private async Task<User> GetSignedInUserAsync(CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId
                         ?? throw new UnauthenticatedException("Sign in first.");
            // A user deleted after the access token was issued.
            return await _userRepository.GetAsync(x => x.Id == userId, cancellationToken)
                   ?? throw new UnauthenticatedException("Sign in first.");
        }

        /// <summary>
        /// The limits are taken before sending (<see cref="ReserveConfirmationEmailAsync"/>), so parallel requests cannot
        /// all pass a check made before any of them has sent; a failed send gives them back.
        /// </summary>
        private async Task SendConfirmationLinkAsync(User user, CancellationToken cancellationToken)
        {
            await ReserveConfirmationEmailAsync(user.Id, user.Email!);
            try
            {
                await SendConfirmationEmailAsync(user, cancellationToken);
            }
            catch
            {
                await ReleaseConfirmationEmailAsync(user.Id, user.Email!);
                throw;
            }
        }

        private async Task SendConfirmationEmailAsync(User user, CancellationToken cancellationToken)
        {
            var (url, expiresAt) = _emailConfirmationTokenService.CreateLink(user.Id, user.Email!);
            var hours = Math.Max(1, (int)Math.Round((expiresAt - DateTime.UtcNow).TotalHours));
            var body = HtmlEmailBody.FromTemplate(EmailTemplates.EmailConfirmation, new Dictionary<string, string?>
            {
                ["name"] = user.FullName,
                ["link"] = url,
                ["hours"] = hours.ToString()
            });
            await _emailService.SendEmailAsync(user.Email!, EmailTemplates.EmailConfirmationSubject, body, cancellationToken);
        }

        // Cooldown per address: right after sign-up (link already sent) a mistyped address can still be corrected at once.
        // The daily cap is per user, so cycling through addresses to mail strangers stays limited.
        private static string CooldownKey(Guid userId, string email) => $"v1:emailconfirm:cooldown:{userId}:{email.Trim().ToLowerInvariant()}";
        private static string DailyKey(Guid userId) => $"v1:emailconfirm:day:{userId}:{DateTime.UtcNow:yyyyMMdd}";

        /// <summary>
        /// Takes the cooldown (SET NX) and one email of the daily cap (INCR) atomically in Redis.
        /// When Redis is down both pass and only the per-IP limit applies.
        /// </summary>
        private async Task ReserveConfirmationEmailAsync(Guid userId, string email)
        {
            if (!await _redisService.TryAcquireAsync(CooldownKey(userId, email), ConfirmationEmailCooldown))
                throw new TooManyRequestsException("A confirmation email was sent less than a minute ago. Wait a little and try again.", ConfirmationEmailCooldown);
            if (await _redisService.IncrementAsync(DailyKey(userId), TimeSpan.FromDays(1)) > ConfirmationEmailsPerDay)
            {
                await ReleaseConfirmationEmailAsync(userId, email);
                throw new TooManyRequestsException("Too many confirmation emails today. Try again tomorrow.");
            }
        }

        private async Task ReleaseConfirmationEmailAsync(Guid userId, string email)
        {
            await _redisService.RemoveAsync(CooldownKey(userId, email));
            await _redisService.IncrementAsync(DailyKey(userId), TimeSpan.FromDays(1), -1);
        }
    }
}
