using Domain.Enums.Actions;

namespace Application.Authorization
{
    /// <summary>
    /// Every permission name the system knows: the members of the enums in <c>Domain.Enums.Actions</c>
    /// (CREATE_USER, READ_SLIDE, ...). Each one is an authorization policy and a valid
    /// <c>UserRolePermission.Permission</c> value.
    /// </summary>
    public static class PermissionCatalog
    {
        private static readonly Lazy<IReadOnlyCollection<string>> Permissions = new(() =>
            typeof(BaseActions).Assembly
                .GetTypes()
                // BaseActions only marks the namespace; its NONE value is not a permission.
                .Where(t => t.IsEnum && t.Namespace == typeof(BaseActions).Namespace && t != typeof(BaseActions))
                .SelectMany(Enum.GetNames)
                .Distinct()
                .ToArray());

        public static IReadOnlyCollection<string> All => Permissions.Value;

        public static bool Exists(string permission) => All.Contains(permission);
    }
}
