using Application.Features.Abstractions.Queries.Interfaces;
using Application.Models.Requests.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.UploadedFiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.UploadedFiles.Queries.GetOne
{
    public record GetOneUploadedFileQuery
        : BaseGetOneQuery,
        IQuery<ResponseModel<UploadedFileViewModel>>
    {
    }
}
