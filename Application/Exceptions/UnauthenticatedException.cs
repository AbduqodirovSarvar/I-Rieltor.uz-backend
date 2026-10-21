using System;

namespace Application.Exceptions
{
    /// <summary>
    /// The caller is not (or no longer) signed in: wrong credentials, an invalid or expired token, a deleted account.
    /// The API answers 401 with this message. The BCL types with similar names
    /// (UnauthorizedAccessException from file I/O, KeyNotFoundException from a dictionary) are server faults and give 500.
    /// </summary>
    public sealed class UnauthenticatedException(string message) : Exception(message)
    {
    }
}
