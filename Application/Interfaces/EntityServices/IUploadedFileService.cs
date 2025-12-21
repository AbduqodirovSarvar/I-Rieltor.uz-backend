using Application.Interfaces.EntityServices.Abstractions;
using Application.Models.Dtos.UploadedFiles;
using Application.Models.ViewModels.UploadedFiles;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.EntityServices
{
    public interface IUploadedFileService
        : IGenericEntityService<
            UploadedFile,
            CreateUploadedFileDto,
            UpdateUploadedFileDto,
            UploadedFileViewModel>
    {
    }
}
