using Application.Interfaces.Files;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistance.Services.Files
{
    public class FileService : IFileService
    {
        private readonly string _webRootPath;

        public FileService(string webRootPath)
        {
            _webRootPath = webRootPath ?? throw new ArgumentNullException(nameof(webRootPath));

            if (!Directory.Exists(_webRootPath))
                Directory.CreateDirectory(_webRootPath);

            var uploadsPath = Path.Combine(_webRootPath, "uploads");
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);
        }
        private string GetUploadPath(string category = "default")
        {
            var path = Path.Combine(_webRootPath, "uploads", category);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        public async Task<UploadedFile> SaveFileAsync(IFormFile file, string category = "default")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            var uploadPath = GetUploadPath(category);
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var extension = Path.GetExtension(file.FileName);
            var uniqueName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadPath, uniqueName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return new UploadedFile
            {
                Id = Guid.NewGuid(),
                Name = uniqueName,
                Path = $"/uploads/{uniqueName}",
                Type = GetMimeType(extension),
                Size = file.Length,
                CreatedAt = DateTime.UtcNow
            };
        }

        public async Task<byte[]?> GetFileAsync(string fileName, string category = "default")
        {
            var filePath = Path.Combine(GetUploadPath(category), fileName);

            if (!File.Exists(filePath))
                return null;

            return await File.ReadAllBytesAsync(filePath);
        }

        public async Task DeleteFileAsync(string fileName, string category = "default")
        {
            var sourcePath = Path.Combine(GetUploadPath(category), fileName);
            if (!File.Exists(sourcePath))
                return;

            var deletedPath = Path.Combine(_webRootPath, "deleted", category);
            if (!Directory.Exists(deletedPath))
                Directory.CreateDirectory(deletedPath);

            var destinationPath = Path.Combine(deletedPath, fileName);

            if (File.Exists(destinationPath))
            {
                throw new IOException($"File '{fileName}' already exists in deleted/{category}.");
            }
            File.Move(sourcePath, destinationPath);
            await Task.CompletedTask;
        }



        private static FileMimeType GetMimeType(string extension)
        {
            return extension.ToLower() switch
            {
                ".png" => FileMimeType.ImagePng,
                ".jpg" or ".jpeg" => FileMimeType.ImageJpeg,
                ".gif" => FileMimeType.ImageGif,
                ".pdf" => FileMimeType.ApplicationPdf,
                ".docx" => FileMimeType.ApplicationDocx,
                ".xlsx" => FileMimeType.ApplicationXlsx,
                ".mp4" => FileMimeType.VideoMp4,
                ".mp3" => FileMimeType.AudioMp3,
                _ => FileMimeType.Unknown
            };
        }
    }
}
