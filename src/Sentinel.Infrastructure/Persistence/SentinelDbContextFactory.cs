
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sentinel.Infrastructure.Persistence;

public sealed class SentinelDbContextFactory
    : IDesignTimeDbContextFactory<SentinelDbContext>
{
    public SentinelDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                "SENTINEL_DB_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "SENTINEL_DB_CONNECTION ortam deðiþkeni tanýmlý deðil.");
        }

        var options = new DbContextOptionsBuilder<SentinelDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new SentinelDbContext(options);
    }
}
