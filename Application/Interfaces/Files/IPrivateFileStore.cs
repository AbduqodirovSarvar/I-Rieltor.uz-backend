using Microsoft.AspNetCore.Http;

namespace Application.Interfaces.Files
{
    public sealed record StoredPrivateFile(string StoredName, string OriginalName, string ContentType, long Size);

    /// <summary>
    /// Storage of personal documents: a folder that no URL leads to. Files are read back only through
    /// <see cref="OpenRead"/>, after the caller's access has been checked.
    /// </summary>
    public interface IPrivateFileStore
    {
        /// <summary>
        /// Checks size and type (pdf, png, jpg, docx: the content must match the extension) and stores the file under a random name.
        /// Throws <see cref="Exceptions.BadRequestException"/> for a file that is not acceptable.
        /// </summary>
        Task<StoredPrivateFile> SaveAsync(IFormFile file, CancellationToken cancellationToken = default);

        /// <summary>Reads a stored file; <see cref="Exceptions.NotFoundException"/> when it is missing on disk.</summary>
        Stream OpenRead(string storedName);

        /// <summary>Removes for good a file whose record was never committed.</summary>
        void Discard(string storedName);

        /// <summary>Moves a file of a deleted document to deleted/private, where it waits for the retention period.</summary>
        void MoveToDeleted(string storedName);
    }
}
