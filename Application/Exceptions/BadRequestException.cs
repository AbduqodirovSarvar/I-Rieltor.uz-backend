using System;

namespace Application.Exceptions
{
    /// <summary>
    /// The request is understood but refused by a business rule (invalid filter, wrong current password, reserved name...).
    /// The API answers 400 with this message, so it must be safe to show to the client.
    /// </summary>
    public sealed class BadRequestException(string message) : Exception(message)
    {
    }
}
