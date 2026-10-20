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
        IUserRoleRepository userRoleRepository
        ) : IAuthService
    {
        private const string DefaultRoleName = "DEFAULT_USER";
        private const string InvalidCredentialsMessage = "Invalid login or password.";

        // Hash used when the login is unknown, so a failed sign-in takes the same time either way.
        private static string? _dummyPasswordHash;

        private readonly ITokenService _tokenService = tokenService;
        private readonly IEmailService _emailService = emailService;
        private readonly IHashService _hashService = hashService;
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IMapper _mapper = mapper;
        private readonly IEimzoService _eimzoService = eimzoService;
        private readonly IUserRoleRepository _userRoleRepository = userRoleRepository;
        public async Task<TokenViewModel> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken)
        {
            var user = await _userRepository
                                .Query(x =>
                                    x.Login == loginDto.Login ||
                                    x.Email == loginDto.Login ||
                                    x.INNPINFL == loginDto.Login, cancellationToken)
                                .Include(x => x.UserRole)
                                    .ThenInclude(x => x.Permissions)
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

            return CreateToken(user);
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
                                        .ThenInclude(x => x.Permissions)
                                    .FirstOrDefaultAsync(cancellationToken)
                                    ?? throw new UnauthorizedAccessException("User not registered.");

            return CreateToken(user);
        }

        public Task<EimzoChallengeResult> GetEimzoChallengeAsync(CancellationToken cancellationToken)
            => _eimzoService.GetChallengeAsync(cancellationToken);

        public Task<bool> LogOutAsync(string token, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<TokenViewModel> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
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

        private TokenViewModel CreateToken(User user)
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
            var accessToken = _tokenService.GenerateToken([.. claims]);

            return new TokenViewModel
            {
                AccessToken = accessToken,
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
            return await _userRepository.UpdateAsync(user, cancellationToken) != null;
        }

        public Task<bool> SendEmailAsync(string email, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
