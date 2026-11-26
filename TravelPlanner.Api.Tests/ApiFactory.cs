using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Npgsql;
using TravelPlanner.Api.DatabaseContext;

namespace TravelPlanner.Api.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    private const string TestKey = "travel-planner-integration-tests-key-0123456789";
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly string? _postgresConnection;
    private bool _databaseCreated;
    public DbCommandInterceptor? CommandInterceptor { get; set; }

    public ApiFactory()
    {
        var postgres = Environment.GetEnvironmentVariable("TRAVELPLANNER_TEST_POSTGRES");
        if (string.IsNullOrWhiteSpace(postgres))
            _connection.Open();
        else
            _postgresConnection = new NpgsqlConnectionStringBuilder(postgres)
            {
                Database = "travelplanner_test_" + Guid.NewGuid().ToString("N"),
                Pooling = false
            }.ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Jwt:Key"] = TestKey,
                ["Logging:LogLevel:Default"] = "Error",
                ["Logging:LogLevel:Microsoft.AspNetCore"] = "Error"
            }));
        builder.ConfigureServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,
                options => options.TokenValidationParameters.IssuerSigningKey =
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey)));
            services.RemoveAll<ContextDb>();
            services.RemoveAll<DbContextOptions<ContextDb>>();
            services.RemoveAll<DbContextOptions>();
            services.AddDbContext<ContextDb>(options =>
            {
                if (_postgresConnection == null) options.UseSqlite(_connection);
                else options.UseNpgsql(_postgresConnection);
                if (CommandInterceptor != null) options.AddInterceptors(CommandInterceptor);
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ContextDb>();
        database.Database.EnsureCreated();
        _databaseCreated = true;
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _databaseCreated && _postgresConnection != null)
        {
            _databaseCreated = false;
            using var scope = Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<ContextDb>().Database.EnsureDeleted();
        }
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}
