using Application.Interfaces.Auths;
using Application.Interfaces.EntityServices;
using Application.Interfaces.Notifications;
using Application.Interfaces.Repositories;
using Application.Models.Dtos.Auths;
using Application.Models.Results;
using Application.Models.ViewModels.Auths;
using AutoMapper;
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
        ICurrentUserService currentUserService
        ) : IAuthService
    {
        private const string DefaultRoleName = "DEFAULT_USER";
        private const string InvalidCredentialsMessage = "Invalid login or password.";
        private const string InvalidRefreshTokenMessage = "Session has expired. Sign in again.";

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
        public async Task<TokenViewModel> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken)
        {
            var user = await _userRepository
                                .Query(x =>
                                    x.Login == loginDto.Login ||
                                    x.Email == loginDto.Login ||
                                    x.INNPINFL == loginDto.Login, cancellationToken)
                                .Include(x => x.UserRole)
                                    .ThenInclude(x => x!.Permissions)
                                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
            {
                _hashService.Verify(loginDto.Password, _dummyPasswordHash ??= _hashService.Hash(Guid.NewGuid().ToString()));
                throw new UnauthorizedAccessException(InvalidCredentialsMessage);
            }

            if (!_hashService.Verify(loginDto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException(InvalidCredentialsMessage);
            }

            return await IssueTokensAsync(user, cancellationToken);
        }

        public async Task<bool> RegisterAsync(RegisterDto registerDto, CancellationToken cancellationToken)
        {
            if (await _userRepository.ExistsWithAnyAsync(registerDto.Login, registerDto.Email, null, registerDto.Phone, cancellationToken: cancellationToken))
            {
                throw new InvalidOperationException("User with this login, email or phone already exists.");
            }

            var user = _mapper.Map<User>(registerDto);
            user.PasswordHash = _hashService.Hash(registerDto.Password);
            user.UserRoleId = await GetDefaultRoleIdAsync(cancellationToken);

            return await _userRepository.CreateAsync(user, cancellationToken) != null;
        }

        public async Task<TokenViewModel> LoginViaEimzoAsync(LoginViaEimzoDto loginDto, CancellationToken cancellationToken)
        {
            var certificate = await AuthenticateWithEimzoAsync(loginDto.SignedData, cancellationToken);
            var identifier = GetEimzoAccountIdentifier(certificate);

            var user = await _userRepository
                                    .Query(x => x.INNPINFL == identifier, cancellationToken)
                                    .Include(x => x.UserRole)
                                        .ThenInclude(x => x!.Permissions)
                                    .FirstOrDefaultAsync(cancellationToken)
                                    ?? throw new UnauthorizedAccessException("User not registered.");

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
                            ?? throw new UnauthorizedAccessException(InvalidRefreshTokenMessage);

            if (stored.RevokedAt != null)
            {
                // A token that was already exchanged is being used again: someone holds a copy.
                // End every session of the user so the copy, and anything issued from it, stops working.
                await _refreshTokenRepository.RevokeAllActiveAsync(stored.UserId, now, cancellationToken);
                throw new UnauthorizedAccessException(InvalidRefreshTokenMessage);
            }

            if (stored.ExpiresAt <= now)
                throw new UnauthorizedAccessException(InvalidRefreshTokenMessage);

            var user = await _userRepository
                                .Query(x => x.Id == stored.UserId, cancellationToken)
                                .Include(x => x.UserRole)
                                    .ThenInclude(x => x!.Permissions)
                                .FirstOrDefaultAsync(cancellationToken)
                                ?? throw new UnauthorizedAccessException(InvalidRefreshTokenMessage);

            var newTokenId = Guid.CreateVersion7();
            if (!await _refreshTokenRepository.TryRevokeAsync(stored.Id, newTokenId, now, cancellationToken))
            {
                // Another request exchanged the same token a moment earlier; treat it like reuse.
                await _refreshTokenRepository.RevokeAllActiveAsync(stored.UserId, now, cancellationToken);
                throw new UnauthorizedAccessException(InvalidRefreshTokenMessage);
            }

            return await IssueTokensAsync(user, cancellationToken, newTokenId);
        }

        public async Task<bool> RegisterViaEimzoAsync(RegisterViaEimzoDto registerDto, CancellationToken cancellationToken)
        {
            var certificate = await AuthenticateWithEimzoAsync(registerDto.SignedData, cancellationToken);
            var identifier = GetEimzoAccountIdentifier(certificate);

            if (await _userRepository.ExistsWithAnyAsync(null, null, identifier, null, cancellationToken: cancellationToken))
                throw new InvalidOperationException("User already registered.");

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
                throw new UnauthorizedAccessException($"E-IMZO: {result.Message}");

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
                throw new UnauthorizedAccessException("E-IMZO: the certificate contains neither PINFL nor organisation INN.");

            return identifier;
        }

        /// <summary>
        /// Starts a session: a short-lived access token plus a refresh token whose hash is stored.
        /// <paramref name="user"/> must be loaded with its role and permissions.
        /// </summary>
        private async Task<TokenViewModel> IssueTokensAsync(User user, CancellationToken cancellationToken, Guid? refreshTokenId = null)
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

            var refreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenExpiresAt = DateTime.UtcNow.Add(_tokenService.RefreshTokenLifetime);
            await _refreshTokenRepository.CreateAsync(new RefreshToken
            {
                Id = refreshTokenId ?? Guid.CreateVersion7(),
                UserId = user.Id,
                TokenHash = _tokenService.HashRefreshToken(refreshToken),
                ExpiresAt = refreshTokenExpiresAt,
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
            var role = await _userRoleRepository.GetAsync(x => x.Name == DefaultRoleName, cancellationToken);
            return role?.Id;
        }

        public Task<bool> ResetAsync(ResetPasswordDto resetPasswordDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// A signed challenge proves who the user is, so it can set a new password without email confirmation.
        /// Also lets users who registered through E-IMZO add a password.
        /// </summary>
        public async Task<bool> ResetViaEimzoAsync(ResetPasswordByEImzoDto resetPasswordDto, CancellationToken cancellationToken)
        {
            var certificate = await AuthenticateWithEimzoAsync(resetPasswordDto.SignedData, cancellationToken);
            var identifier = GetEimzoAccountIdentifier(certificate);

            var user = await _userRepository.GetAsync(x => x.INNPINFL == identifier, cancellationToken)
                            ?? throw new KeyNotFoundException("User not registered.");

            user.PasswordHash = _hashService.Hash(resetPasswordDto.NewPassword);
            var updated = await _userRepository.UpdateAsync(user, cancellationToken) != null;

            // Whoever knew the old password must not stay signed in.
            await _refreshTokenRepository.RevokeAllActiveAsync(user.Id, DateTime.UtcNow, cancellationToken);
            return updated;
        }

        public Task<bool> SendEmailAsync(string email, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
