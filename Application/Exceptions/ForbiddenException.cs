using System;

namespace Application.Exceptions
{
    /// <summary>
    /// The caller is signed in but may not do this (missing permission, managing a more powerful user).
    /// The API answers 403 with this message, so it must be safe to show to the client.
    /// </summary>
    public sealed class ForbiddenException(string message) : Exception(message)
    {
    }
}
