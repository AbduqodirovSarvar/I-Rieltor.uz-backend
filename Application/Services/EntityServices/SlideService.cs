using Application.Interfaces.Auths;
using Application.Interfaces.EntityServices;
using Application.Interfaces.Repositories;
using Application.Models.Dtos.Slides;
using Application.Models.Dtos.UploadedFiles;
using Application.Models.Results;
using Application.Models.ViewModels.Slides;
using Application.Services.EntityServices.Abstractions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.EntityServices
{
    public class SlideService(
        ISlideRepository repository,
        IPermissionService permissionService,
        IMapper mapper,
        IUploadedFileService uploadedFileService)
        : GenericEntityService<Slide,
                               CreateSlideDto,
                               UpdateSlideDto,
                               SlideViewModel>(repository, permissionService, mapper), ISlideService
    {
        private readonly IUploadedFileService _uploadedFileService = uploadedFileService;
        public override async Task<ResponseModel<SlideViewModel>> CreateAsync(CreateSlideDto entityCreateDto, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(Slide).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.CREATE}_{entityTypeName}"))
                throw new UnauthorizedAccessException("You do not have permission to create this entity.");

            var entity = _mapper.Map<Slide>(entityCreateDto);

            var file = await _uploadedFileService.CreateAsync(new CreateUploadedFileDto()
            {
                File = entityCreateDto.File,
                Description = entityCreateDto.Content,
                Alt = entityCreateDto.Title,
                EntityName = entityTypeName.ToLower()
            }, cancellationToken);

            if (file.Data != null) 
            {
                entity.UploadedFileId = file.Data.Id;
            }
            
            var result = await _repository.CreateAsync(entity, cancellationToken)
                                ?? throw new InvalidOperationException("Failed to create entity.");

            return ResponseModel<SlideViewModel>.Ok(_mapper.Map<SlideViewModel>(result));
        }

        public override async Task<ResponseModel<SlideViewModel>> UpdateAsync(UpdateSlideDto entityUpdateDto, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(Slide).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.UPDATE}_{entityTypeName}"))
                throw new UnauthorizedAccessException("You do not have permission to create this entity.");

            var entity = _mapper.Map<Slide>(entityUpdateDto);

            var file = await _uploadedFileService.UpdateAsync(new UpdateUploadedFileDto()
            {
                Id = entity.UploadedFileId,
                File = entityUpdateDto.File,
                Description = entityUpdateDto.Content,
                Alt = entityUpdateDto.Title
            }, cancellationToken);

            if (file.Data != null)
            {
                entity.UploadedFileId = file.Data.Id;
            }

            var result = await _repository.UpdateAsync(entity, cancellationToken)
                                ?? throw new InvalidOperationException("Failed to update entity.");

            return ResponseModel<SlideViewModel>.Ok(_mapper.Map<SlideViewModel>(result));
        }

        public override async Task<ResponseModel<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(Slide).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.DELETE}_{entityTypeName}"))
                throw new UnauthorizedAccessException("You do not have permission to create this entity.");

            var entity = await _repository.GetAsync(x => x.Id == id, cancellationToken)
                                ?? throw new KeyNotFoundException("Entity not found");

            await _repository.DeleteAsync(entity, cancellationToken);

            return await _uploadedFileService.DeleteAsync(entity.UploadedFileId, cancellationToken);
        }

        public override async Task<ResponseModel<SlideViewModel?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(Slide).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.READ}_{entityTypeName}"))
                throw new UnauthorizedAccessException("You do not have permission to create this entity.");

            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                                ?? throw new KeyNotFoundException("Entity not found");

            var result = _mapper.Map<SlideViewModel>(entity);

            result.UploadedFile = (await _uploadedFileService.GetByIdAsync(entity.UploadedFileId, cancellationToken)).Data;

            return ResponseModel<SlideViewModel?>.Ok();
        }

        public override async Task<ResponseModel<SlideViewModel?>> GetAsync(Expression<Func<Slide, bool>> predicate, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(Slide).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.READ}_{entityTypeName}"))
                throw new UnauthorizedAccessException("You do not have permission to create this entity.");

            var entity = await _repository.GetAsync(predicate, cancellationToken)
                                ?? throw new KeyNotFoundException("Entity not found");

            var result = _mapper.Map<SlideViewModel>(entity);

            result.UploadedFile = (await _uploadedFileService.GetByIdAsync(entity.UploadedFileId, cancellationToken)).Data;
            return ResponseModel<SlideViewModel?>.Ok(result);
        }
    }
}
