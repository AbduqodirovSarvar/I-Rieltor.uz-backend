using Application.Features.Abstractions.Queries.Interfaces;
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
        IFileService fileService)
        : IQueryHandler<GetOneUploadedFileQuery, ResponseModel<UploadedFileViewModel>>
    {
        public Task<ResponseModel<UploadedFileViewModel>> Handle(GetOneUploadedFileQuery request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
