namespace Application.Interfaces.Notifications
{
    public interface ISmsSender
    {
        /// <summary>
        /// False when no SMS provider is configured: SMS notifications are then not even queued.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Sends one text message. <paramref name="phone"/> is in international format (+998901234567).
        /// Throws <see cref="Exceptions.ServiceUnavailableException"/> when the provider cannot take it now.
        /// </summary>
        Task SendAsync(string phone, string message, CancellationToken cancellationToken = default);
    }
}
