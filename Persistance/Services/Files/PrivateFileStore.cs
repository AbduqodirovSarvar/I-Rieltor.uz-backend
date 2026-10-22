using Application.Exceptions;
using Application.Interfaces.Files;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace Persistance.Services.Files
{
    /// <summary>
    /// &lt;root&gt;/private/&lt;guid&gt;&lt;ext&gt;. Nothing serves this folder: only the document endpoints read it.
    /// </summary>
    public sealed class PrivateFileStore : IPrivateFileStore
    {
        // Documents only: no video, audio or spreadsheets, and nothing a browser would run.
        private static readonly FileMimeType[] Allowed =
            [FileMimeType.ApplicationPdf, FileMimeType.ImagePng, FileMimeType.ImageJpeg, FileMimeType.ApplicationDocx];

        private readonly string _folder;
        private readonly string _deletedFolder;
        private readonly long _maxBytes;

        public PrivateFileStore(string rootPath, int maxFileSizeMb)
        {
            _folder = Path.Combine(rootPath, "private");
            _deletedFolder = Path.Combine(rootPath, "deleted", "private");
            _maxBytes = maxFileSizeMb * 1024L * 1024L;
            Directory.CreateDirectory(_folder);
        }

        public async Task<StoredPrivateFile> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
                throw new BadRequestException("File is empty");
            if (file.Length > _maxBytes)
                throw new BadRequestException($"File is larger than {_maxBytes / (1024 * 1024)} MB.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var type = FileService.GetMimeType(extension);
            if (!Allowed.Contains(type))
                throw new BadRequestException($"File type '{extension}' is not allowed. Use pdf, png, jpg or docx.");
            if (!await FileService.HasExpectedSignatureAsync(file, type))
                throw new BadRequestException($"File content does not match the '{extension}' type.");

            var storedName = $"{Guid.NewGuid()}{extension}";
            var path = Path.Combine(_folder, storedName);
            try
            {
                await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await file.CopyToAsync(stream, cancellationToken);
            }
            catch
            {
                if (File.Exists(path))
                    File.Delete(path);
                throw;
            }
            return new StoredPrivateFile(storedName, CleanName(file.FileName), ContentTypeOf(type), file.Length);
        }

        public Stream OpenRead(string storedName)
        {
            var path = PathOf(_folder, storedName);
            if (!File.Exists(path))
                throw new NotFoundException("The document file is missing.");
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        }

        public void Discard(string storedName)
        {
            var path = PathOf(_folder, storedName);
            if (File.Exists(path))
                File.Delete(path);
        }

        public void MoveToDeleted(string storedName)
        {
            var path = PathOf(_folder, storedName);
            if (!File.Exists(path))
                return;
            Directory.CreateDirectory(_deletedFolder);
            File.Move(path, PathOf(_deletedFolder, storedName), overwrite: true);
            File.SetLastWriteTimeUtc(PathOf(_deletedFolder, storedName), DateTime.UtcNow);
        }

        /// <summary>A stored name is ours (guid + extension); anything with a path in it is refused, not trusted.</summary>
        private static string PathOf(string folder, string storedName)
        {
            if (storedName != Path.GetFileName(storedName) || storedName.Length is 0 or > 60)
                throw new BadRequestException("Invalid document name.");
            return Path.Combine(folder, storedName);
        }

        private static string ContentTypeOf(FileMimeType type) => type switch
        {
            FileMimeType.ApplicationPdf => "application/pdf",
            FileMimeType.ImagePng => "image/png",
            FileMimeType.ImageJpeg => "image/jpeg",
            FileMimeType.ApplicationDocx => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };

        /// <summary>The client's file name for display: no path, no control characters, at most 255 characters.</summary>
        private static string CleanName(string fileName)
        {
            var name = Path.GetFileName(fileName.Replace('\\', '/'));
            var clean = new StringBuilder();
            foreach (var c in name)
                if (!char.IsControl(c))
                    clean.Append(c);
            var result = clean.ToString().Trim();
            if (result.Length > 255)
                result = result[..255];
            return result.Length == 0 ? "document" : result;
        }
    }
}
