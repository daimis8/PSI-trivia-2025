using System.Threading.Tasks;
using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace tests.Integration
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
    {
        public SqliteConnection Connection { get; }

        public CustomWebApplicationFactory()
        {
            Connection = new SqliteConnection("DataSource=:memory:");
            Connection.Open();
        }
    

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Test");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                var testSettings = new Dictionary<string, string>
                {
                    ["Jwt:Key"] = "0123456789ABCDEFGHIJKLMNOPQRSTUV",
                    ["Jwt:Issuer"] = "TestIssuer",
                    ["Jwt:Audience"] = "TestAudience"
                };
                config.AddInMemoryCollection(testSettings);
            });

            builder.ConfigureServices(services =>
            {
                var toRemove = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    d.ServiceType == typeof(AppDbContext) ||
                    (d.ServiceType.IsGenericType &&
                    d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextFactory<>)))
                    .ToList(); 

                foreach (var d in toRemove)
                    services.Remove(d);

                services.AddDbContext<AppDbContext>(options => 
                {
                    options.UseSqlite(Connection);
                });

                var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
            });
        }

        public new void Dispose()
        {
            base.Dispose();
            Connection?.Close();
        }
    }
}