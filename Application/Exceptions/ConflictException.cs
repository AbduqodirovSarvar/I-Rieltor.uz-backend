using System;

namespace Application.Exceptions
{
    /// <summary>
    /// The request is valid but clashes with the current state (e.g. deleting a role that users still have).
    /// The API answers 409.
    /// </summary>
    public sealed class ConflictException(string message) : Exception(message)
    {
    }
}
