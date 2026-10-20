using Application.Interfaces.Auths;
using Application.Interfaces.EntityServices;
using Application.Interfaces.Repositories;
using Application.Models.Dtos.Companies;
using Application.Models.Dtos.UploadedFiles;
using Application.Models.Results;
using Application.Models.ViewModels.Companies;
using Application.Services.EntityServices.Abstractions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Application.Services.EntityServices
{
    public class CompanyService(
        ICompanyRepository repository,
        IPermissionService permissionService,
        IMapper mapper,
        IUploadedFileService uploadedFileService
        )
        : GenericEntityService<Company,
                               CreateCompanyDto,
                               UpdateCompanyDto,
                               CompanyViewModel>(repository, permissionService, mapper), ICompanyService
    {
        private readonly IUploadedFileService _uploadedFileService = uploadedFileService;
        public override async Task<ResponseModel<CompanyViewModel>> CreateAsync(CreateCompanyDto entityCreateDto, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(Company).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.CREATE}_{entityTypeName}"))
                throw new UnauthorizedAccessException("You do not have permission to create this entity.");

            var entity = _mapper.Map<Company>(entityCreateDto);

            if (entityCreateDto.Logo != null)
            {
                var file = (await _uploadedFileService.CreateAsync(new CreateUploadedFileDto()
                {
                    File = entityCreateDto.Logo,
                    Description = entityCreateDto.Description,
                    Alt = entityCreateDto.Name,
                    EntityName = entityTypeName.ToLower()
                }, cancellationToken)).Data ?? throw new InvalidOperationException();

                entity.LogoFileId = file.Id;
            }

            var result = await _repository.CreateAsync(entity, cancellationToken)
                                ?? throw new InvalidOperationException("Failed to create entity.");

            return ResponseModel<CompanyViewModel>.Ok(_mapper.Map<CompanyViewModel>(result));
        }

        public override async Task<ResponseModel<CompanyViewModel>> UpdateAsync(UpdateCompanyDto entityUpdateDto, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(Company).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.UPDATE}_{entityTypeName}"))
                throw new UnauthorizedAccessException("You do not have permission to create this entity.");

            var entity = await _repository.GetAsync(x => x.Id == entityUpdateDto.Id, cancellationToken)
                                ?? throw new KeyNotFoundException("Entity not found");
            _mapper.Map(entityUpdateDto, entity);

            var oldLogoFileId = entity.LogoFileId;
            if (entityUpdateDto.Logo != null)
            {
                var file = (await _uploadedFileService.CreateAsync(new CreateUploadedFileDto()
                {
                    File = entityUpdateDto.Logo,
                    Description = entityUpdateDto.Description,
                    Alt = entityUpdateDto.Name,
                    EntityName = entityTypeName.ToLower()
                }, cancellationToken)).Data ?? throw new InvalidOperationException();

                entity.LogoFileId = file.Id;
            }

            var result = await _repository.UpdateAsync(entity, cancellationToken)
                                ?? throw new InvalidOperationException("Failed to update entity.");

            // The old logo can only be removed after the company stops referencing it (FK).
            if (entityUpdateDto.Logo != null && oldLogoFileId != null)
                await _uploadedFileService.DeleteAsync(oldLogoFileId.Value, cancellationToken);

            return ResponseModel<CompanyViewModel>.Ok(_mapper.Map<CompanyViewModel>(result));
        }

        public override async Task<ResponseModel<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(Company).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.DELETE}_{entityTypeName}"))
                throw new UnauthorizedAccessException("You do not have permission to create this entity.");

            var entity = await _repository.GetAsync(x => x.Id == id, cancellationToken)
                                ?? throw new KeyNotFoundException("Entity not found");

            await _repository.DeleteAsync(entity, cancellationToken);

            if (entity.LogoFileId == null)
                return ResponseModel<bool>.Ok();

            return await _uploadedFileService.DeleteAsync(entity.LogoFileId ?? Guid.Empty, cancellationToken);
        }

        public override async Task<ResponseModel<CompanyViewModel?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(Company).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.READ}_{entityTypeName}"))
                throw new UnauthorizedAccessException("You do not have permission to create this entity.");

            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                                ?? throw new KeyNotFoundException("Entity not found");

            var result = _mapper.Map<CompanyViewModel>(entity);

            if(entity.LogoFileId == null)
                return ResponseModel<CompanyViewModel?>.Ok(result);

            result.LogoFile = (await _uploadedFileService.GetByIdAsync(entity.LogoFileId ?? Guid.Empty, cancellationToken)).Data;

            return ResponseModel<CompanyViewModel?>.Ok(result);
        }

        public override async Task<ResponseModel<CompanyViewModel?>> GetAsync(Expression<Func<Company, bool>> predicate, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(Company).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.READ}_{entityTypeName}"))
                throw new UnauthorizedAccessException("You do not have permission to create this entity.");

            var entity = await _repository.GetAsync(predicate, cancellationToken)
                                ?? throw new KeyNotFoundException("Entity not found");

            var result = _mapper.Map<CompanyViewModel>(entity);

            if (entity.LogoFileId == null)
                return ResponseModel<CompanyViewModel?>.Ok(result);

            result.LogoFile = (await _uploadedFileService.GetByIdAsync(entity.LogoFileId ?? Guid.Empty, cancellationToken)).Data;

            return ResponseModel<CompanyViewModel?>.Ok(result);
        }
    }
}
