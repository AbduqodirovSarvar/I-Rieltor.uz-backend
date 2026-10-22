using Application.Models.Dtos.PrivateDocuments;
using Application.Models.Requests.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.PrivateDocuments;

namespace Application.Interfaces.EntityServices
{
    public sealed record PrivateDocumentDownload(Stream Content, string ContentType, string FileName);

    public interface IPrivateDocumentService
    {
        /// <summary>Stores a document of the signed-in user.</summary>
        Task<ResponseModel<PrivateDocumentViewModel>> UploadAsync(UploadPrivateDocumentDto dto, CancellationToken cancellationToken = default);

        /// <summary>The signed-in user's own documents, newest first.</summary>
        Task<ResponseModel<List<PrivateDocumentViewModel>>> GetMyAsync(BaseGetListQuery query, CancellationToken cancellationToken = default);

        /// <summary>Everyone's documents (READ_PRIVATEDOCUMENT), e.g. the attachments of one auction's applicants.</summary>
        Task<ResponseModel<List<PrivateDocumentViewModel>>> GetAllAsync(BaseGetListQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// The file, for its owner or for READ_PRIVATEDOCUMENT. Anyone else gets "not found", the same answer as for an id
        /// that does not exist, so ids of other people's documents cannot be probed.
        /// </summary>
        Task<PrivateDocumentDownload> DownloadAsync(Guid id, CancellationToken cancellationToken = default);

        Task<ResponseModel<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
