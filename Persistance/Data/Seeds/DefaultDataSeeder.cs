using Application.Interfaces.Auths;
using Domain.Behaviours;
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

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hashService = scope.ServiceProvider.GetRequiredService<IHashService>();
            var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
            var adminConfiguration = scope.ServiceProvider.GetRequiredService<IOptions<DefaultAdminConfiguration>>().Value;

            using var transaction = await context.Database.BeginTransactionAsync();

            var superAdminRole = await SeedRolesAsync(context);
            await SeedPermissionsAsync(context, superAdminRole, permissionService);
            await SeedAdminUserAsync(context, superAdminRole, hashService, adminConfiguration);

            await transaction.CommitAsync();
        }

        private static async Task<UserRole> SeedRolesAsync(AppDbContext context)
        {
            var defaultRole = await context.UserRoles
                .FirstOrDefaultAsync(x => x.Name == "DEFAULT_USER");

            if (defaultRole == null)
            {
                defaultRole = new UserRole
                {
                    Name = "DEFAULT_USER",
                    Description = "Default role for any user"
                };

                await context.UserRoles.AddAsync(defaultRole);
            }

            var superAdminRole = await context.UserRoles
                .FirstOrDefaultAsync(x => x.Name == "SUPER_ADMIN");

            if (superAdminRole == null)
            {
                superAdminRole = new UserRole
                {
                    Name = "SUPER_ADMIN",
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

        private static async Task SeedAdminUserAsync(
            AppDbContext context,
            UserRole role,
            IHashService hashService,
            DefaultAdminConfiguration adminConfiguration)
        {
            var admin = await context.Users
                .FirstOrDefaultAsync(x => x.Login == "SUPER_ADMIN");

            if (admin != null)
            {
                // Earlier versions seeded a publicly known password; rotate it if it is still in use.
                if (hashService.Verify(LegacyDefaultPassword, admin.PasswordHash))
                {
                    admin.PasswordHash = hashService.Hash(GetValidatedPassword(adminConfiguration));
                    await context.SaveChangesAsync();
                }
                return;
            }

            admin = new User
            {
                FullName = "Super Administrator",
                Login = "SUPER_ADMIN",
                Email = adminConfiguration.Email,
                Phone = "+998900000000",
                Type = ClientType.NONE,
                UserRoleId = role.Id,
                PasswordHash = hashService.Hash(GetValidatedPassword(adminConfiguration))
            };

            await context.Users.AddAsync(admin);
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
