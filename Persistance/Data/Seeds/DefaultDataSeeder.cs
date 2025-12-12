using Application.Interfaces.Auths;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistance.Data.Seeds
{
    public static class DefaultDataSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hashService = scope.ServiceProvider.GetRequiredService<IHashService>();
            var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();

            using var transaction = await context.Database.BeginTransactionAsync();

            var superAdminRole = await SeedRolesAsync(context);
            await SeedPermissionsAsync(context, superAdminRole, permissionService);
            await SeedAdminUserAsync(context, superAdminRole, hashService);

            await transaction.CommitAsync();
        }

        private static async Task<UserRole> SeedRolesAsync(AppDbContext context)
        {
            var role = await context.UserRoles
                .FirstOrDefaultAsync(x => x.Name == "SUPER_ADMIN");

            if (role != null)
                return role;

            role = new UserRole()
            {
                Name = "SUPER_ADMIN",
                Description = "Super Administrator with full permissions"
            };

            await context.UserRoles.AddAsync(role);
            await context.SaveChangesAsync();

            return role;
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
            IHashService hashService)
        {
            var admin = await context.Users
                .FirstOrDefaultAsync(x => x.Login == "SUPER_ADMIN");

            if (admin != null)
                return;

            admin = new User
            {
                FullName = "Super Administrator",
                Login = "SUPER_ADMIN",
                Email = "admin@example.com",
                Phone = "+998900000000",
                Type = ClientType.NONE,
                UserRoleId = role.Id,
                PasswordHash = hashService.Hash("SuperAdmin@123")
            };

            await context.Users.AddAsync(admin);
            await context.SaveChangesAsync();
        }
    }

}
