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
            await SeedRolesAsync(serviceProvider);
            await SeedAdminUserAsync(serviceProvider);
            await Task.CompletedTask;
        }

        private static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {
            await Task.CompletedTask;
        }

        private static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
        {
            await Task.CompletedTask;
        }
    }
}
