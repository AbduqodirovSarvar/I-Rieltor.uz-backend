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
using Microsoft.EntityFrameworkCore;
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
        IUploadedFileService uploadedFileService,
        IUnitOfWork unitOfWork)
        : GenericEntityService<Slide,
                               CreateSlideDto,
                               UpdateSlideDto,
                               SlideViewModel>(repository, permissionService, mapper), ISlideService
    {
        private readonly IUploadedFileService _uploadedFileService = uploadedFileService;
        // The slide row and its image (record and file on disk) are saved or rolled back together.
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        public override Task<ResponseModel<SlideViewModel>> CreateAsync(CreateSlideDto entityCreateDto, CancellationToken cancellationToken = default)
            => _unitOfWork.ExecuteAsync(() => CreateInTransactionAsync(entityCreateDto, cancellationToken), cancellationToken);

        private async Task<ResponseModel<SlideViewModel>> CreateInTransactionAsync(CreateSlideDto entityCreateDto, CancellationToken cancellationToken)
        {
            string entityTypeName = typeof(Slide).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.CREATE}_{entityTypeName}"))
                throw new UnauthorizedAccessException("You do not have permission to create this entity.");

            var entity = _mapper.Map<Slide>(entityCreateDto);

            var file = (await _uploadedFileService.CreateAsync(new CreateUploadedFileDto()
            {
                File = entityCreateDto.File,
                Description = entityCreateDto.Content,
                Alt = entityCreateDto.Title,
                EntityName = entityTypeName.ToLower()
            }, cancellationToken)).Data ?? throw new InvalidOperationException("Failed to create entity.");

            entity.UploadedFileId = file.Id;

            var result = await _repository.CreateAsync(entity, cancellationToken)
                                ?? throw new InvalidOperationException("Failed to create entity.");

            return ResponseModel<SlideViewModel>.Ok(_mapper.Map<SlideViewModel>(result));
        }

        public override Task<ResponseModel<SlideViewModel>> UpdateAsync(UpdateSlideDto entityUpdateDto, CancellationToken cancellationToken = default)
            => _unitOfWork.ExecuteAsync(() => UpdateInTransactionAsync(entityUpdateDto, cancellationToken), cancellationToken);

        private async Task<ResponseModel<SlideViewModel>> UpdateInTransactionAsync(UpdateSlideDto entityUpdateDto, CancellationToken cancellationToken)
        {
            string entityTypeName = typeof(Slide).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.UPDATE}_{entityTypeName}"))
                throw new UnauthorizedAccessException("You do not have permission to create this entity.");

            var entity = await _repository.GetAsync(x => x.Id == entityUpdateDto.Id, cancellationToken)
                                ?? throw new KeyNotFoundException("Entity not found");
            _mapper.Map(entityUpdateDto, entity);

            if (entityUpdateDto.File != null)
            {
                // Replaces the content of the slide's existing file record; its Id stays the same.
                _ = (await _uploadedFileService.UpdateAsync(new UpdateUploadedFileDto()
                {
                    Id = entity.UploadedFileId,
                    File = entityUpdateDto.File,
                    Description = entityUpdateDto.Content,
                    Alt = entityUpdateDto.Title
                }, cancellationToken)).Data ?? throw new InvalidOperationException("Failed to update entity.");
            }

            var result = await _repository.UpdateAsync(entity, cancellationToken)
                                ?? throw new InvalidOperationException("Failed to update entity.");

            return ResponseModel<SlideViewModel>.Ok(_mapper.Map<SlideViewModel>(result));
        }

        public override Task<ResponseModel<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => _unitOfWork.ExecuteAsync(() => DeleteInTransactionAsync(id, cancellationToken), cancellationToken);

        private async Task<ResponseModel<bool>> DeleteInTransactionAsync(Guid id, CancellationToken cancellationToken)
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
            // GetAsync checks the READ permission.
            return await GetAsync(x => x.Id == id, cancellationToken);
        }

        public override async Task<ResponseModel<SlideViewModel?>> GetAsync(Expression<Func<Slide, bool>> predicate, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(Slide).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.READ}_{entityTypeName}"))
                throw new UnauthorizedAccessException("You do not have permission to create this entity.");

            // The image is loaded with the slide: READ_SLIDE is enough, READ_UPLOADEDFILE is not required.
            var entity = await _repository.Query(predicate, cancellationToken)
                                .Include(x => x.UploadedFile)
                                .FirstOrDefaultAsync(cancellationToken)
                                ?? throw new KeyNotFoundException("Entity not found");

            return ResponseModel<SlideViewModel?>.Ok(_mapper.Map<SlideViewModel>(entity));
        }

        public override async Task<ResponseModel<List<SlideViewModel>>> GetAllAsync(Expression<Func<Slide, bool>>? predicate = null, int pageIndex = 0, int pageSize = 10, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(Slide).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.READ}_{entityTypeName}"))
                throw new UnauthorizedAccessException("You do not have permission to read this entity.");

            var query = _repository.Query(predicate, cancellationToken);
            var totalCount = await query.CountAsync(cancellationToken);
            var slides = await query.Include(x => x.UploadedFile)
                                    .OrderBy(x => x.Order)
                                    .ThenBy(x => x.CreatedAt)
                                    .Skip(pageIndex * pageSize)
                                    .Take(pageSize)
                                    .ToListAsync(cancellationToken);

            return ResponseModel<List<SlideViewModel>>.Ok(_mapper.Map<List<SlideViewModel>>(slides), pageIndex, pageSize, totalCount);
        }

        /// <summary>
        /// Slides for the home page carousel, in display order. Open to anonymous visitors.
        /// </summary>
        public async Task<ResponseModel<List<SlideViewModel>>> GetPublicListAsync(CancellationToken cancellationToken = default)
        {
            var slides = await _repository.Query(cancellationToken: cancellationToken)
                                .Include(x => x.UploadedFile)
                                .OrderBy(x => x.Order)
                                .ThenBy(x => x.CreatedAt)
                                .ToListAsync(cancellationToken);

            return ResponseModel<List<SlideViewModel>>.Ok(_mapper.Map<List<SlideViewModel>>(slides));
        }
    }
}
