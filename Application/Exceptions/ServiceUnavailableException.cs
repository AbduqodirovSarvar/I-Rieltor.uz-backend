using System;

namespace Application.Exceptions
{
    /// <summary>
    /// An external service the request depends on (for example E-IMZO-SERVER) is unreachable.
    /// The API answers 503 so clients know to retry later rather than fix their input.
    /// </summary>
    public sealed class ServiceUnavailableException(string message, Exception? innerException = null)
        : Exception(message, innerException)
    {
    }
}
