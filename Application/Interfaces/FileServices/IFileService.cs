using Domain.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.FileServices
{
    public interface IFileService
    {
        Task<UploadedFile> SaveFileAsync(IFormFile fileBytes);
        Task<byte[]?> GetFileAsync(string fileName);
        Task DeleteFileAsync(string fileName);
    }
}
