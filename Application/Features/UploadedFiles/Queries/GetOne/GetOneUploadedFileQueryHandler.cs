using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Interfaces.Files;
using Application.Models.Results;
using Application.Models.ViewModels.UploadedFiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.UploadedFiles.Queries.GetOne
{
    public class GetOneUploadedFileQueryHandler(
        IUploadedFileService uploadedFileService)
        : IQueryHandler<GetOneUploadedFileQuery, ResponseModel<UploadedFileViewModel?>>
    {
        private readonly IUploadedFileService _uploadedFileService = uploadedFileService;
        public async Task<ResponseModel<UploadedFileViewModel?>> Handle(GetOneUploadedFileQuery request, CancellationToken cancellationToken)
        {
            return await _uploadedFileService.GetByIdAsync(request.Id, cancellationToken);
        }
    }
}
