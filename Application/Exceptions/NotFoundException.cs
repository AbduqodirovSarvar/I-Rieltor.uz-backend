using System;

namespace Application.Exceptions
{
    /// <summary>
    /// The requested record does not exist (or is soft-deleted).
    /// The API answers 404 with this message. The BCL types with similar names
    /// (UnauthorizedAccessException from file I/O, KeyNotFoundException from a dictionary) are server faults and give 500.
    /// </summary>
    public sealed class NotFoundException(string message) : Exception(message)
    {
    }
}
