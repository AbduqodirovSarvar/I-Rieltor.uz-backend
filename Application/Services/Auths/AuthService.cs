using Application.Interfaces.Auths;
using Application.Interfaces.EntityServices;
using Application.Interfaces.Notifications;
using Application.Interfaces.Repositories;
using Application.Models.Dtos.Auths;
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
        IUserRoleService userRoleService
        ) : IAuthService
    {
        private readonly ITokenService _tokenService = tokenService;
        private readonly IEmailService _emailService = emailService;
        private readonly IHashService _hashService = hashService;
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IMapper _mapper = mapper;
        private readonly IEimzoService _eimzoService = eimzoService;
        private readonly IUserRoleService _userRoleService = userRoleService;
        public async Task<TokenViewModel> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken)
        {
            var user = await _userRepository
                                .Query(x =>
                                    x.Login == loginDto.Login ||
                                    x.Email == loginDto.Login ||
                                    x.INNPINFL == loginDto.Login, cancellationToken)
                                .Include(x => x.UserRole)
                                    .ThenInclude(x => x.Permissions)
                                .FirstOrDefaultAsync(cancellationToken)
                                ?? throw new UnauthorizedAccessException("Invalid login or password.");

            if (!_hashService.Verify(loginDto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid username or password.");
            }

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

        public async Task<bool> RegisterAsync(RegisterDto registerDto, CancellationToken cancellationToken)
        {
            var userExists = await _userRepository.AnyAsync(x => x.Login == registerDto.Login
                                                           || x.Email == registerDto.Email
                                                           || x.INNPINFL == registerDto.INNPINFL,
                                                           cancellationToken);
            if (userExists)
            {
                throw new InvalidOperationException("User with this login, email, or INNPINFL already exists.");
            }

            var user = _mapper.Map<User>(registerDto);
            user.PasswordHash = _hashService.Hash(registerDto.Password);

            return await _userRepository.CreateAsync(user, cancellationToken) != null;
        }

        public async Task<TokenViewModel> LoginViaEimzoAsync(LoginViaEimzoDto loginDto, CancellationToken cancellationToken)
        {
            var verifyResult = await _eimzoService.VerifyAsync(loginDto.SignedData, cancellationToken);

            if (!verifyResult.IsValid)
                throw new UnauthorizedAccessException("Invalid E-Imzo signature.");

            var user = await _userRepository
                                    .Query(x =>
                                        x.INNPINFL == verifyResult.Pinfl ||
                                        x.INNPINFL == verifyResult.Inn,
                                        cancellationToken)
                                    .Include(x => x.UserRole)
                                        .ThenInclude(x => x.Permissions)
                                    .FirstOrDefaultAsync(cancellationToken)
                                    ?? throw new UnauthorizedAccessException("User not registered.");

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
            var verifyResult = await _eimzoService.VerifyAsync(
                registerDto.SignedData,
                cancellationToken);

            if (!verifyResult.IsValid)
                throw new UnauthorizedAccessException("Invalid E-Imzo signature.");

            var userExists = await _userRepository.AnyAsync(
                x => x.INNPINFL == verifyResult.Pinfl ||
                     x.INNPINFL == verifyResult.Inn,
                cancellationToken);

            if (userExists)
                throw new InvalidOperationException("User already registered.");

            var user = new User();
            if(!string.IsNullOrEmpty(verifyResult.Pinfl))
{
                user.Type = ClientType.INDIVIDUAL;
                user.INNPINFL = verifyResult.Pinfl;
                user.FullName = $"{verifyResult.LastName} {verifyResult.FirstName}";
            }
            else {
                user.Type = ClientType.LEGAL;
                user.INNPINFL = verifyResult.Inn;
                user.LegalName = verifyResult.LegalName;
                user.FullName = verifyResult.LegalName!;
            }

            var defaultRoleResult = await _userRoleService.GetAsync(x => x.Name == "DEFAULT_USER", cancellationToken);

            user.UserRoleId = defaultRoleResult.Data?.Id;

            return await _userRepository.CreateAsync(user, cancellationToken) != null;
        }

        public Task<bool> ResetAsync(ResetPasswordDto resetPasswordDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ResetViaEimzoAsync(ResetPasswordByEImzoDto resetPasswordDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SendEmailAsync(string email, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
