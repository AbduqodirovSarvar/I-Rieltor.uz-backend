using Application.Exceptions;
using Application.Extentions;
using Application.Interfaces.Auths;
using Application.Interfaces.EntityServices;
using Application.Interfaces.Files;
using Application.Interfaces.Repositories;
using Application.Models.Dtos.PrivateDocuments;
using Application.Models.Requests.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.PrivateDocuments;
using Domain.Entities;
using MapsterMapper;

namespace Application.Services.EntityServices
{
    public class PrivateDocumentService(
        IPrivateDocumentRepository documents,
        IPrivateFileStore store,
        IAuctionRepository auctions,
        IPaymentRepository payments,
        ICurrentUserService currentUser,
        IPermissionService permissionService,
        IMapper mapper,
        IUnitOfWork unitOfWork) : IPrivateDocumentService
    {
        // A person needs a handful; the cap keeps one account from filling the disk.
        private const int MaxDocumentsPerUser = 100;

        public Task<ResponseModel<PrivateDocumentViewModel>> UploadAsync(UploadPrivateDocumentDto dto, CancellationToken cancellationToken = default)
            => unitOfWork.ExecuteAsync(async () =>
            {
                var userId = currentUser.UserId ?? throw new UnauthenticatedException("Sign in first.");
                if (await documents.GetPaginatedAsync(d => d.OwnerId == userId, 0, 1, null, cancellationToken) is { TotalItems: >= MaxDocumentsPerUser })
                    throw new ConflictException($"You can keep at most {MaxDocumentsPerUser} documents; delete some first.");
                if (dto.AuctionId is { } auctionId && !await auctions.AnyAsync(a => a.Id == auctionId, cancellationToken))
                    throw new BadRequestException("The auction does not exist.");
                // A receipt can only be attached to one's own payment: the payment id would otherwise let anyone attach files to others.
                if (dto.PaymentId is { } paymentId && !await payments.AnyAsync(p => p.Id == paymentId && p.UserId == userId, cancellationToken))
                    throw new BadRequestException("The payment does not exist.");

                var stored = await store.SaveAsync(dto.File, cancellationToken);
                unitOfWork.OnRollback(() => { store.Discard(stored.StoredName); return Task.CompletedTask; });

                var entity = await documents.CreateAsync(new PrivateDocument
                {
                    OwnerId = userId,
                    Purpose = dto.Purpose,
                    AuctionId = dto.AuctionId,
                    PaymentId = dto.PaymentId,
                    OriginalName = stored.OriginalName,
                    StoredName = stored.StoredName,
                    ContentType = stored.ContentType,
                    Size = stored.Size,
                    Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                }, cancellationToken);
                return ResponseModel<PrivateDocumentViewModel>.Ok(mapper.Map<PrivateDocumentViewModel>(entity));
            }, cancellationToken);

        public async Task<ResponseModel<List<PrivateDocumentViewModel>>> GetMyAsync(BaseGetListQuery query, CancellationToken cancellationToken = default)
        {
            var userId = currentUser.UserId ?? throw new UnauthenticatedException("Sign in first.");
            return await ListAsync(query, d => d.OwnerId == userId, cancellationToken);
        }

        public async Task<ResponseModel<List<PrivateDocumentViewModel>>> GetAllAsync(BaseGetListQuery query, CancellationToken cancellationToken = default)
        {
            if (!await permissionService.Check("READ_PRIVATEDOCUMENT"))
                throw new ForbiddenException("You do not have permission to read this entity.");
            return await ListAsync(query, null, cancellationToken);
        }

        private async Task<ResponseModel<List<PrivateDocumentViewModel>>> ListAsync(BaseGetListQuery query, System.Linq.Expressions.Expression<Func<PrivateDocument, bool>>? scope, CancellationToken cancellationToken)
        {
            var filter = query.BuildPredicate<PrivateDocument>();
            // The scope (the caller's own documents) is part of the predicate, whatever filters the client sends.
            System.Linq.Expressions.Expression<Func<PrivateDocument, bool>>? predicate = (scope, filter) switch
            {
                (null, _) => filter,
                (_, null) => scope,
                _ => scope.AndAlso(filter),
            };
            var (data, total) = await documents.GetPaginatedAsync(predicate, query.PageIndex, query.PageSize,
                query.BuildOrderBy<PrivateDocument>() ?? (q => q.OrderByDescending(d => d.CreatedAt).ThenByDescending(d => d.Id)), cancellationToken);
            return ResponseModel<List<PrivateDocumentViewModel>>.Ok(mapper.Map<List<PrivateDocumentViewModel>>(data), query.PageIndex, query.PageSize, total);
        }

        public async Task<PrivateDocumentDownload> DownloadAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var document = await FindAccessibleAsync(id, "READ_PRIVATEDOCUMENT", cancellationToken);
            return new PrivateDocumentDownload(store.OpenRead(document.StoredName), document.ContentType, document.OriginalName);
        }

        public Task<ResponseModel<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => unitOfWork.ExecuteAsync(async () =>
            {
                var document = await FindAccessibleAsync(id, "DELETE_PRIVATEDOCUMENT", cancellationToken);
                var deleted = await documents.DeleteAsync(document, cancellationToken);
                await unitOfWork.AfterCommitAsync(() => { store.MoveToDeleted(document.StoredName); return Task.CompletedTask; });
                return ResponseModel<bool>.Ok(deleted);
            }, cancellationToken);

        /// <summary>
        /// The owner, or staff with <paramref name="staffPermission"/>. Everybody else: not found.
        /// </summary>
        private async Task<PrivateDocument> FindAccessibleAsync(Guid id, string staffPermission, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId ?? throw new UnauthenticatedException("Sign in first.");
            var document = await documents.GetAsync(d => d.Id == id, cancellationToken);
            if (document == null || (document.OwnerId != userId && !await permissionService.Check(staffPermission)))
                throw new NotFoundException("Document not found");
            return document;
        }
    }
}
