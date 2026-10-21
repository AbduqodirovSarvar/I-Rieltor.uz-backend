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
        /// <summary>Moves a stored file to wwwroot/deleted, so it can still be recovered.</summary>
        Task DeleteFileAsync(string fileName, string category = "default");
        /// <summary>Removes for good a file whose database record was never committed.</summary>
        Task DiscardFileAsync(string fileName, string category = "default");
        /// <summary>Public address of a stored file: FileStorage:PublicBaseUrl + its Path (/uploads/...).</summary>
        string GetPublicUrl(string path);
    }
}
