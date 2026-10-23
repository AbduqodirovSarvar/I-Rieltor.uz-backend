namespace Application.Interfaces.Auths
{
    /// <summary>Who a user is, in the words an admin list needs.</summary>
    public sealed record UserBrief(string FullName, string? Phone);

    /// <summary>Looks up names and phones of many users in one query, so lists show people and not bare ids.</summary>
    public interface IUserDirectory
    {
        Task<IReadOnlyDictionary<Guid, UserBrief>> GetAsync(IEnumerable<Guid?> ids, CancellationToken cancellationToken = default);
    }
}
