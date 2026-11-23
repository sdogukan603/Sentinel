
using Microsoft.EntityFrameworkCore;
using Sentinel.Core.Interfaces;
using Sentinel.Core.Services;
using Sentinel.Infrastructure.Persistence;
using Sentinel.Infrastructure.Repositories;
using Sentinel.Worker;

var builder = Host.CreateApplicationBuilder(args);

var connectionString =
    builder.Configuration["SENTINEL_DB_CONNECTION"]
    ?? throw new InvalidOperationException(
        "SENTINEL_DB_CONNECTION is not configured.");

builder.Services.AddDbContext<SentinelDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IJobRepository, PostgresJobRepository>();
builder.Services.AddScoped<JobService>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
