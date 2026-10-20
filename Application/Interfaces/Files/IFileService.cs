using Domain.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Files
{
    public interface IFileService
    {
        Task<UploadedFile> SaveFileAsync(IFormFile fileBytes, string category = "default");
        Task DeleteFileAsync(string fileName, string category = "default");
    }
}
