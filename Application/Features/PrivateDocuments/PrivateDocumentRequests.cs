using Application.Features.Abstractions.Commands.Interfaces;
using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Dtos.PrivateDocuments;
using Application.Models.Requests.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.PrivateDocuments;

namespace Application.Features.PrivateDocuments
{
    public record UploadPrivateDocumentCommand : UploadPrivateDocumentDto, ICommand<ResponseModel<PrivateDocumentViewModel>>;
    public record DeletePrivateDocumentCommand : DeletePrivateDocumentDto, ICommand<ResponseModel<bool>>;
    public record GetMyPrivateDocumentsQuery : BaseGetListQuery, IQuery<ResponseModel<List<PrivateDocumentViewModel>>>;
    public record GetPrivateDocumentListQuery : BaseGetListQuery, IQuery<ResponseModel<List<PrivateDocumentViewModel>>>;

    public class PrivateDocumentRequestHandlers(IPrivateDocumentService service)
        : ICommandHandler<UploadPrivateDocumentCommand, ResponseModel<PrivateDocumentViewModel>>,
          ICommandHandler<DeletePrivateDocumentCommand, ResponseModel<bool>>,
          IQueryHandler<GetMyPrivateDocumentsQuery, ResponseModel<List<PrivateDocumentViewModel>>>,
          IQueryHandler<GetPrivateDocumentListQuery, ResponseModel<List<PrivateDocumentViewModel>>>
    {
        public Task<ResponseModel<PrivateDocumentViewModel>> Handle(UploadPrivateDocumentCommand request, CancellationToken cancellationToken)
            => service.UploadAsync(request, cancellationToken);

        public Task<ResponseModel<bool>> Handle(DeletePrivateDocumentCommand request, CancellationToken cancellationToken)
            => service.DeleteAsync(request.Id, cancellationToken);

        public Task<ResponseModel<List<PrivateDocumentViewModel>>> Handle(GetMyPrivateDocumentsQuery request, CancellationToken cancellationToken)
            => service.GetMyAsync(request, cancellationToken);

        public Task<ResponseModel<List<PrivateDocumentViewModel>>> Handle(GetPrivateDocumentListQuery request, CancellationToken cancellationToken)
            => service.GetAllAsync(request, cancellationToken);
    }
}
