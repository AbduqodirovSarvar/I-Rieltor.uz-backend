using System;

namespace Application.Exceptions
{
    /// <summary>
    /// The caller must wait before trying again (e.g. resending a confirmation email too soon).
    /// The API answers 429 with this message, so it must be safe to show to the client.
    /// </summary>
    public sealed class TooManyRequestsException(string message) : Exception(message)
    {
    }
}
