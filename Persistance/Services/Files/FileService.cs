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
        private readonly string _rootPath;
        private readonly string _publicBaseUrl;
        private readonly long _maxFileSizeBytes;

        /// <param name="rootPath">Fayllar saqlanadigan papka (<see cref="FileStorageLocation"/>); ichida uploads/ va deleted/.</param>
        /// <param name="publicBaseUrl">
        /// Fayllar statik beriladigan server manzili (masalan "https://api.i-rieltor.uz").
        /// Bo'sh bo'lsa Url nisbiy yo'l ko'rinishida saqlanadi.
        /// </param>
        /// <param name="maxFileSizeMb">Bitta faylning ruxsat etilgan eng katta hajmi, MB.</param>
        public FileService(string rootPath, string? publicBaseUrl = null, int maxFileSizeMb = 20)
        {
            _rootPath = rootPath ?? throw new ArgumentNullException(nameof(rootPath));
            _publicBaseUrl = publicBaseUrl?.TrimEnd('/') ?? string.Empty;
            _maxFileSizeBytes = maxFileSizeMb * 1024L * 1024L;

            if (!Directory.Exists(_rootPath))
                Directory.CreateDirectory(_rootPath);

            var uploadsPath = Path.Combine(_rootPath, "uploads");
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);
        }
        private string GetUploadPath(string category = "default")
        {
            var path = Path.Combine(_rootPath, "uploads", category);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        public async Task<UploadedFile> SaveFileAsync(IFormFile file, string category = "default")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            if (file.Length > _maxFileSizeBytes)
                throw new ArgumentException($"File is larger than {_maxFileSizeBytes / (1024 * 1024)} MB.");

            // Only types that the static file middleware is allowed to serve may be stored.
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var mimeType = GetMimeType(extension);
            if (mimeType == FileMimeType.Unknown)
                throw new ArgumentException($"File type '{extension}' is not allowed.");

            // The extension comes from the client, so the content must match it too.
            if (!await HasExpectedSignatureAsync(file, mimeType))
                throw new ArgumentException($"File content does not match the '{extension}' type.");

            var uploadPath = GetUploadPath(category);
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var uniqueName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadPath, uniqueName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = category != null ? $"/uploads/{category}/{uniqueName}" : $"/uploads/{uniqueName}";

            return new UploadedFile
            {
                Name = uniqueName,
                Path = relativePath,
                Url = $"{_publicBaseUrl}{relativePath}",
                Type = mimeType,
                Size = file.Length,
                CreatedAt = DateTime.UtcNow
            };
        }

        public async Task DeleteFileAsync(string fileName, string category = "default")
        {
            var sourcePath = Path.Combine(GetUploadPath(category), fileName);
            if (!File.Exists(sourcePath))
                return;

            var deletedPath = Path.Combine(_rootPath, "deleted", category);
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

        public Task DiscardFileAsync(string fileName, string category = "default")
        {
            var path = Path.Combine(GetUploadPath(category), fileName);
            if (File.Exists(path))
                File.Delete(path);
            return Task.CompletedTask;
        }

        private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
        private static readonly byte[] Gif87Signature = "GIF87a"u8.ToArray();
        private static readonly byte[] Gif89Signature = "GIF89a"u8.ToArray();
        private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();
        private static readonly byte[] ZipSignature = [0x50, 0x4B, 0x03, 0x04]; // docx and xlsx are zip archives
        private static readonly byte[] Mp4BoxType = "ftyp"u8.ToArray();
        private static readonly byte[] Id3Signature = "ID3"u8.ToArray();

        private static async Task<bool> HasExpectedSignatureAsync(IFormFile file, FileMimeType mimeType)
        {
            var header = new byte[12];
            await using var stream = file.OpenReadStream();
            var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
            return MatchesSignature(header.AsSpan(0, read), mimeType);
        }

        private static bool MatchesSignature(ReadOnlySpan<byte> header, FileMimeType mimeType)
        {
            return mimeType switch
            {
                FileMimeType.ImagePng => header.StartsWith(PngSignature),
                FileMimeType.ImageJpeg => header.StartsWith(JpegSignature),
                FileMimeType.ImageGif => header.StartsWith(Gif87Signature) || header.StartsWith(Gif89Signature),
                FileMimeType.ApplicationPdf => header.StartsWith(PdfSignature),
                FileMimeType.ApplicationDocx or FileMimeType.ApplicationXlsx => header.StartsWith(ZipSignature),
                // MP4: 4-byte box size, then the "ftyp" box type.
                FileMimeType.VideoMp4 => header.Length >= 8 && header[4..8].SequenceEqual(Mp4BoxType),
                // MP3: ID3 tag, or an MPEG audio frame sync (11 set bits).
                FileMimeType.AudioMp3 => header.StartsWith(Id3Signature)
                                         || (header.Length >= 2 && header[0] == 0xFF && (header[1] & 0xE0) == 0xE0),
                _ => false
            };
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
