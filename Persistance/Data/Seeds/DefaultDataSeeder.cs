using Microsoft.Extensions.Logging;
using Application.Interfaces.Auths;
using Domain.Behaviours;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Persistance.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistance.Data.Seeds
{
    public static class DefaultDataSeeder
    {
        private const string LegacyDefaultPassword = "SuperAdmin@123";
        private const string BootstrapAdminLogin = SystemRoles.BootstrapAdminLogin;
        // Any constant: instances starting at the same time seed one after another instead of racing on unique indexes.
        private const long SeedLockKey = 7_212_605_001;

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hashService = scope.ServiceProvider.GetRequiredService<IHashService>();
            var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
            var adminConfiguration = scope.ServiceProvider.GetRequiredService<IOptions<DefaultAdminConfiguration>>().Value;
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DefaultDataSeeder));

            using var transaction = await context.Database.BeginTransactionAsync();
            await context.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_xact_lock({SeedLockKey})");

            var superAdminRole = await SeedRolesAsync(context);
            await SeedPermissionsAsync(context, superAdminRole, permissionService);
            await SeedAdminUserAsync(context, superAdminRole, hashService, adminConfiguration, logger);

            await transaction.CommitAsync();
        }

        private static async Task<UserRole> SeedRolesAsync(AppDbContext context)
        {
            var defaultRole = await context.UserRoles
                .FirstOrDefaultAsync(x => x.Name == SystemRoles.DefaultUser);

            if (defaultRole == null)
            {
                defaultRole = new UserRole
                {
                    Name = SystemRoles.DefaultUser,
                    Description = "Default role for any user"
                };

                await context.UserRoles.AddAsync(defaultRole);
            }

            var superAdminRole = await context.UserRoles
                .FirstOrDefaultAsync(x => x.Name == SystemRoles.SuperAdmin);

            if (superAdminRole == null)
            {
                superAdminRole = new UserRole
                {
                    Name = SystemRoles.SuperAdmin,
                    Description = "Super Administrator with full permissions"
                };

                await context.UserRoles.AddAsync(superAdminRole);
            }

            await context.SaveChangesAsync();
            return superAdminRole;
        }


        private static async Task SeedPermissionsAsync(
            AppDbContext context,
            UserRole superAdminRole,
            IPermissionService permissionService)
        {
            var allPermissions = await permissionService.GetAllPermission();

            var existing = await context.UserRolePermissions
                .Where(x => x.UserRoleId == superAdminRole.Id)
                .Select(x => x.Permission)
                .ToListAsync();

            var existingSet = new HashSet<string>(existing);

            var missing = allPermissions
                .Where(p => !existingSet.Contains(p))
                .Select(p => new UserRolePermission
                {
                    Permission = p,
                    UserRoleId = superAdminRole.Id
                })
                .ToList();

            if (missing.Any())
            {
                await context.UserRolePermissions.AddRangeAsync(missing);
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Makes sure at least one active user holds SUPER_ADMIN; it does not track one particular account.
        /// The bootstrap admin may be renamed freely, and the API refuses to delete or demote the last SUPER_ADMIN,
        /// so the recovery branches below only run after a manual database change.
        /// </summary>
        private static async Task SeedAdminUserAsync(
            AppDbContext context,
            UserRole role,
            IHashService hashService,
            DefaultAdminConfiguration adminConfiguration,
            ILogger logger)
        {
            // Earlier versions seeded a publicly known password; rotate it wherever it is still in use.
            var bootstrap = await context.Users.FirstOrDefaultAsync(x => x.Login == BootstrapAdminLogin);
            if (bootstrap != null && hashService.Verify(LegacyDefaultPassword, bootstrap.PasswordHash))
            {
                bootstrap.PasswordHash = hashService.Hash(GetValidatedPassword(adminConfiguration));
                await context.SaveChangesAsync();
            }

            if (await context.Users.AnyAsync(x => x.UserRoleId == role.Id))
                return;

            // Never promote an existing account: whoever holds a login is not proof of being the administrator.
            // A new account is created instead; if the bootstrap login is taken, a suffixed one, written to the log.
            var login = bootstrap == null ? BootstrapAdminLogin : $"{BootstrapAdminLogin}_{Guid.NewGuid():N}"[..(BootstrapAdminLogin.Length + 7)];

            // The configured email may meanwhile belong to someone else; the unique index would stop the app from starting.
            var email = adminConfiguration.Email?.Trim();
            if (!string.IsNullOrWhiteSpace(email) && await context.Users.AnyAsync(x => x.Email == email))
                email = null;

            logger.LogWarning("No active SUPER_ADMIN user found; creating user '{Login}' with DefaultAdmin__Password", login);
            await context.Users.AddAsync(new User
            {
                FullName = "Super Administrator",
                Login = login,
                Email = email,
                Type = ClientType.NONE,
                UserRoleId = role.Id,
                PasswordHash = hashService.Hash(GetValidatedPassword(adminConfiguration))
            });
            await context.SaveChangesAsync();
        }

        private static string GetValidatedPassword(DefaultAdminConfiguration adminConfiguration)
        {
            var password = adminConfiguration.Password;

            if (string.IsNullOrWhiteSpace(password)
                || password == LegacyDefaultPassword
                || !new PasswordValidationAttribute().IsValid(password))
                throw new InvalidOperationException(
                    "DefaultAdmin:Password is not configured or too weak. Set DefaultAdmin__Password in .env or environment variables " +
                    "(minimum 8 characters, 1 uppercase letter, 1 digit and 1 symbol).");

            return password;
        }
    }

}
