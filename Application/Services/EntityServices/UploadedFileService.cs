using Application.Interfaces.Auths;
using Application.Interfaces.EntityServices;
using Application.Interfaces.Files;
using Application.Interfaces.Repositories;
using Application.Models.Dtos.UploadedFiles;
using Application.Models.Results;
using Application.Models.ViewModels.UploadedFiles;
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

namespace Application.Services.EntityServices
{
    public class UploadedFileService(
                        IUploadedFileRepository repository,
                        IPermissionService permissionService,
                        IMapper mapper,
                        IFileService fileService)
        : GenericEntityService<UploadedFile,
                                CreateUploadedFileDto,
                                UpdateUploadedFileDto,
                                UploadedFileViewModel>(repository, permissionService, mapper), IUploadedFileService
    {
        private readonly IFileService _fileService = fileService;

        public override async Task<ResponseModel<UploadedFileViewModel>> CreateAsync(CreateUploadedFileDto entityCreateDto, CancellationToken cancellationToken = default)
        {
            var entity = await _fileService.SaveFileAsync(entityCreateDto.File, entityCreateDto.EntityName.ToLower())
                                ?? throw new FileLoadException("File cannot saved");

            entity.Alt = entityCreateDto.Alt;
            entity.Description = entityCreateDto.Description;
            entity.EntityName = entityCreateDto.EntityName;
            entity.EntityId = entityCreateDto.EntityId;

            var result = await _repository.CreateAsync(entity, cancellationToken)
                                ?? throw new InvalidOperationException("Failed to create entity.");

            return ResponseModel<UploadedFileViewModel>.Ok(_mapper.Map<UploadedFileViewModel>(result));
        }

        public override async Task<ResponseModel<UploadedFileViewModel>> UpdateAsync(UpdateUploadedFileDto entityUpdateDto, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetAsync(x => x.Id == entityUpdateDto.Id, cancellationToken)
                                ?? throw new KeyNotFoundException("Entity not found");

            if (entityUpdateDto.File != null)
            {
                var category = entity.EntityName.ToLower();
                var newFile = await _fileService.SaveFileAsync(entityUpdateDto.File, category)
                                    ?? throw new FileLoadException("File cannot saved");
                await _fileService.DeleteFileAsync(entity.Name, category);
                entity.Size = newFile.Size;
                entity.Url = newFile.Url;
                entity.Name = newFile.Name;
                entity.Path = newFile.Path;
                entity.Type = newFile.Type;
            }
            
            entity.Description = entityUpdateDto.Description;
            entity.Alt = entityUpdateDto.Alt;

            var result = await _repository.UpdateAsync(entity, cancellationToken)
                                ?? throw new InvalidOperationException("Failed to update entity.");

            return ResponseModel<UploadedFileViewModel>.Ok(_mapper.Map<UploadedFileViewModel>(result));
        }

        public override async Task<ResponseModel<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(UploadedFile).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.DELETE}_{entityTypeName}"))
                throw new UnauthorizedAccessException("You do not have permission to create this entity.");

            var entity = await _repository.GetAsync(x => x.Id == id, cancellationToken)
                                ?? throw new KeyNotFoundException("Entity not found");

            await _fileService.DeleteFileAsync(entity.Name, entity.EntityName.ToLower());

            return ResponseModel<bool>.Ok(await _repository.DeleteAsync(entity, cancellationToken));
        }

        public override async Task<ResponseModel<UploadedFileViewModel?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(UploadedFile).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.READ}_{entityTypeName}"))
                throw new UnauthorizedAccessException("You do not have permission to create this entity.");

            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                                ?? throw new KeyNotFoundException("Entity not found");

            return ResponseModel<UploadedFileViewModel?>.Ok(_mapper.Map<UploadedFileViewModel>(entity));
        }

        public override async Task<ResponseModel<UploadedFileViewModel?>> GetAsync(Expression<Func<UploadedFile, bool>> predicate, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(UploadedFile).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.READ}_{entityTypeName}"))
                throw new UnauthorizedAccessException("You do not have permission to create this entity.");

            var entity = await _repository.GetAsync(predicate, cancellationToken)
                                ?? throw new KeyNotFoundException("Entity not found");

            return ResponseModel<UploadedFileViewModel?>.Ok(_mapper.Map<UploadedFileViewModel>(entity));
        }
    }
}
