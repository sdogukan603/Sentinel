
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Sentinel.Infrastructure.Persistence;

namespace Sentinel.IntegrationTests;

internal static class TestDatabase
{
    public static string GetConnectionString()
    {
        var connectionString =
            Environment.GetEnvironmentVariable("SENTINEL_DB_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "SENTINEL_DB_CONNECTION is not configured.");
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        if (!string.Equals(
                builder.Database,
                "sentinel_test",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Integration tests must use the sentinel_test database.");
        }

        return connectionString;
    }

    public static DbContextOptions<SentinelDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<SentinelDbContext>()
            .UseNpgsql(GetConnectionString())
            .Options;
    }
}
