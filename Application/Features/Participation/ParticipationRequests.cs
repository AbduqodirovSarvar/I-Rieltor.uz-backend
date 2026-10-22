using Application.Features.Abstractions.Commands.Interfaces;
using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Requests.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.Participation;
using Domain.Behaviours;
using System.ComponentModel.DataAnnotations;

namespace Application.Features.Participation
{
    public record GetApplicationTextQuery : IQuery<ResponseModel<ApplicationTextViewModel>>
    {
        [Required, NotEmptyGuid]
        public Guid AuctionId { get; init; }
    }

    public record ApplyCommand : ICommand<ResponseModel<ParticipantViewModel>>
    {
        [Required, NotEmptyGuid]
        public Guid AuctionId { get; init; }
        /// <summary>Base64 PKCS#7 of the application text (GET api/Participation/application-text), signed with E-IMZO.</summary>
        [Required]
        public string SignedData { get; init; } = null!;
    }

    public record ApplyMobileCommand : ICommand<ResponseModel<ParticipantViewModel>>
    {
        [Required, NotEmptyGuid]
        public Guid AuctionId { get; init; }
        /// <summary>The finished E-IMZO Mobile signing session (POST api/auth/eimzo/mobile/sign/start) that signed the application text.</summary>
        [Required, MaxLength(100)]
        public string DocumentId { get; init; } = null!;
    }

    public record WithdrawApplicationCommand : ICommand<ResponseModel<ParticipantViewModel>>
    {
        [Required, NotEmptyGuid]
        public Guid AuctionId { get; init; }
    }

    public record GetMyParticipationsQuery : IQuery<ResponseModel<List<MyParticipationViewModel>>>;

    public record GetParticipantListQuery : BaseGetListQuery, IQuery<ResponseModel<List<ParticipantAdminViewModel>>>;

    public record ApproveParticipantCommand : ICommand<ResponseModel<ParticipantAdminViewModel>>
    {
        [Required, NotEmptyGuid]
        public Guid Id { get; init; }
    }

    public record RejectParticipantCommand : ICommand<ResponseModel<ParticipantAdminViewModel>>
    {
        [Required, NotEmptyGuid]
        public Guid Id { get; init; }
        /// <summary>Shown to the applicant.</summary>
        [Required, MaxLength(500)]
        public string Reason { get; init; } = string.Empty;
    }

    public class ParticipationRequestHandlers(IParticipationService service)
        : IQueryHandler<GetApplicationTextQuery, ResponseModel<ApplicationTextViewModel>>,
          ICommandHandler<ApplyCommand, ResponseModel<ParticipantViewModel>>,
          ICommandHandler<ApplyMobileCommand, ResponseModel<ParticipantViewModel>>,
          ICommandHandler<WithdrawApplicationCommand, ResponseModel<ParticipantViewModel>>,
          IQueryHandler<GetMyParticipationsQuery, ResponseModel<List<MyParticipationViewModel>>>,
          IQueryHandler<GetParticipantListQuery, ResponseModel<List<ParticipantAdminViewModel>>>,
          ICommandHandler<ApproveParticipantCommand, ResponseModel<ParticipantAdminViewModel>>,
          ICommandHandler<RejectParticipantCommand, ResponseModel<ParticipantAdminViewModel>>
    {
        public Task<ResponseModel<ApplicationTextViewModel>> Handle(GetApplicationTextQuery request, CancellationToken cancellationToken)
            => service.GetApplicationTextAsync(request.AuctionId, cancellationToken);

        public Task<ResponseModel<ParticipantViewModel>> Handle(ApplyCommand request, CancellationToken cancellationToken)
            => service.ApplyAsync(request.AuctionId, request.SignedData, cancellationToken);

        public Task<ResponseModel<ParticipantViewModel>> Handle(ApplyMobileCommand request, CancellationToken cancellationToken)
            => service.ApplyMobileAsync(request.AuctionId, request.DocumentId, cancellationToken);

        public Task<ResponseModel<ParticipantViewModel>> Handle(WithdrawApplicationCommand request, CancellationToken cancellationToken)
            => service.WithdrawAsync(request.AuctionId, cancellationToken);

        public Task<ResponseModel<List<MyParticipationViewModel>>> Handle(GetMyParticipationsQuery request, CancellationToken cancellationToken)
            => service.GetMyAsync(cancellationToken);

        public Task<ResponseModel<List<ParticipantAdminViewModel>>> Handle(GetParticipantListQuery request, CancellationToken cancellationToken)
            => service.GetListAsync(request, cancellationToken);

        public Task<ResponseModel<ParticipantAdminViewModel>> Handle(ApproveParticipantCommand request, CancellationToken cancellationToken)
            => service.ApproveAsync(request.Id, cancellationToken);

        public Task<ResponseModel<ParticipantAdminViewModel>> Handle(RejectParticipantCommand request, CancellationToken cancellationToken)
            => service.RejectAsync(request.Id, request.Reason, cancellationToken);
    }
}
