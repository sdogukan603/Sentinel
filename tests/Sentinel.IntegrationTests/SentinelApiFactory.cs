using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sentinel.Core.Interfaces;
using Sentinel.Infrastructure.Repositories;

namespace Sentinel.IntegrationTests;

public sealed class SentinelApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(
            "SENTINEL_DB_CONNECTION",
            "Host=localhost;Database=sentinel_test_placeholder");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IJobRepository>();

            services.AddSingleton<IJobRepository, InMemoryJobRepository>();
        });
    }
}