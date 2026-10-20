using Application.Interfaces.Auths;
using Application.Interfaces.EntityServices;
using Application.Interfaces.Repositories;
using Application.Interfaces.Repositories.Abstractions;
using Application.Models.Dtos.Users;
using Application.Models.Results;
using Application.Models.ViewModels.Users;
using Application.Services.EntityServices.Abstractions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.EntityServices
{
    public class UserService(
                           IUserRepository repository,
                           IPermissionService permissionService,
                           IMapper mapper,
                           IHashService hashService,
                           IRefreshTokenRepository refreshTokenRepository,
                           ICurrentUserService currentUserService)
        : GenericEntityService<User,
                               CreateUserDto,
                               UpdateUserDto,
                               UserViewModel>(repository, permissionService, mapper), IUserService
    {
        private readonly IUserRepository _userRepository = repository;
        private readonly IHashService _hashService = hashService;
        private readonly IRefreshTokenRepository _refreshTokenRepository = refreshTokenRepository;
        private readonly ICurrentUserService _currentUserService = currentUserService;

        public override async Task<ResponseModel<UserViewModel>> CreateAsync(CreateUserDto entityCreateDto, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(User).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.CREATE}_{entityTypeName}"))
                throw new UnauthorizedAccessException("You do not have permission to create this entity.");

            if (await _userRepository.ExistsWithAnyAsync(entityCreateDto.Login, entityCreateDto.Email, entityCreateDto.INNPINFL, entityCreateDto.Phone, cancellationToken: cancellationToken))
                throw new InvalidOperationException("User with this login, email, INNPINFL or phone already exists.");

            var entity = _mapper.Map<User>(entityCreateDto);
            entity.PasswordHash = _hashService.Hash(entityCreateDto.Password);

            var result = await _repository.CreateAsync(entity, cancellationToken)
                                ?? throw new InvalidOperationException("Failed to create entity.");

            return ResponseModel<UserViewModel>.Ok(_mapper.Map<UserViewModel>(result));
        }

        public override async Task<ResponseModel<UserViewModel>> UpdateAsync(UpdateUserDto entityUpdateDto, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(User).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.UPDATE}_{entityTypeName}"))
                throw new UnauthorizedAccessException("You do not have permission to update this entity.");

            var entity = await _repository.GetAsync(x => x.Id == entityUpdateDto.Id, cancellationToken)
                                ?? throw new KeyNotFoundException("Entity not found");

            if (await _userRepository.ExistsWithAnyAsync(entityUpdateDto.Login, entityUpdateDto.Email, entityUpdateDto.INNPINFL, entityUpdateDto.Phone, entityUpdateDto.Id, cancellationToken))
                throw new InvalidOperationException("User with this login, email, INNPINFL or phone already exists.");

            _mapper.Map(entityUpdateDto, entity);

            // An empty password means "keep the current one".
            var passwordChanged = !string.IsNullOrWhiteSpace(entityUpdateDto.Password);
            if (passwordChanged)
                entity.PasswordHash = _hashService.Hash(entityUpdateDto.Password!);

            var result = await _repository.UpdateAsync(entity, cancellationToken)
                                ?? throw new InvalidOperationException("Failed to update entity.");

            // Sessions opened with the old password end with it.
            if (passwordChanged)
                await _refreshTokenRepository.RevokeAllActiveAsync(entity.Id, DateTime.UtcNow, cancellationToken);

            return ResponseModel<UserViewModel>.Ok(_mapper.Map<UserViewModel>(result));
        }

        /// <summary>
        /// The signed-in user; every user may read their own profile, so no permission is required.
        /// </summary>
        public Task<User?> GetCurrentUser(CancellationToken cancellationToken = default)
            => _currentUserService.GetCurrentUserAsync(cancellationToken);
    }
}
