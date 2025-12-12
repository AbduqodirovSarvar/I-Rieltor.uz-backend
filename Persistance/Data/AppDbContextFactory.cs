using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Persistance.Data.Interceptors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistance.Data
{
    //public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    //{
    //    public AppDbContext CreateDbContext(string[] args)
    //    {
    //        // Build config to read appsettings.json
    //        IConfigurationRoot configuration = new ConfigurationBuilder()
    //            //.SetBasePath(Directory.GetCurrentDirectory())
    //            //.AddJsonFile("appsettings.json", optional: false)
    //            .Build();

    //        // Read connection string
    //        var connectionString = configuration.GetConnectionString("DefaultConnection");

    //        var builder = new DbContextOptionsBuilder<AppDbContext>();
    //        builder.UseNpgsql(connectionString);

    //        return new AppDbContext(builder.Options, new AuditableEntitySaveChangesInterceptor());
    //    }
    //}
}
