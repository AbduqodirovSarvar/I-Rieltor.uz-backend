using Application.Models.Requests.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.Participation;

namespace Application.Interfaces.EntityServices
{
    public sealed record SignedApplicationFile(byte[] Content, string FileName);

    public interface IParticipationService
    {
        /// <summary>The application text the signed-in user signs for the auction.</summary>
        Task<ResponseModel<ApplicationTextViewModel>> GetApplicationTextAsync(Guid auctionId, CancellationToken cancellationToken = default);

        /// <summary>Apply with a PKCS#7 of the application text signed in a browser or a USB token.</summary>
        Task<ResponseModel<ParticipantViewModel>> ApplyAsync(Guid auctionId, string signedData, CancellationToken cancellationToken = default);

        /// <summary>Apply with a signing session finished on the phone (E-IMZO Mobile / ID-card).</summary>
        Task<ResponseModel<ParticipantViewModel>> ApplyMobileAsync(Guid auctionId, string documentId, CancellationToken cancellationToken = default);

        /// <summary>Takes the application back before the sale starts; a paid deposit is queued for refund.</summary>
        Task<ResponseModel<ParticipantViewModel>> WithdrawAsync(Guid auctionId, CancellationToken cancellationToken = default);

        Task<ResponseModel<List<MyParticipationViewModel>>> GetMyAsync(CancellationToken cancellationToken = default);

        /// <summary>Staff: applications with the applicant's data.</summary>
        Task<ResponseModel<List<ParticipantAdminViewModel>>> GetListAsync(BaseGetListQuery query, CancellationToken cancellationToken = default);

        /// <summary>Staff: the E-IMZO signature (PKCS#7) of an application, as the file the applicant signed.</summary>
        Task<SignedApplicationFile> GetSignedApplicationAsync(Guid participantId, CancellationToken cancellationToken = default);

        Task<ResponseModel<ParticipantAdminViewModel>> ApproveAsync(Guid participantId, CancellationToken cancellationToken = default);

        Task<ResponseModel<ParticipantAdminViewModel>> RejectAsync(Guid participantId, string reason, CancellationToken cancellationToken = default);
    }
}
