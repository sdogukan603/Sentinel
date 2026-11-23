using Microsoft.EntityFrameworkCore;
using Sentinel.Infrastructure.Persistence;
using Sentinel.Core.Results;
using Sentinel.Core.Services;
using Sentinel.Core.Interfaces;
using Sentinel.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration["SENTINEL_DB_CONNECTION"]
    ?? throw new InvalidOperationException(
        "SENTINEL_DB_CONNECTION is not configured.");

builder.Services.AddDbContext<SentinelDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IJobRepository, PostgresJobRepository>();
builder.Services.AddScoped<JobService>();

var app = builder.Build();

app.MapGet("/", () =>
    Results.Ok(new
    {
        Application = "SENTINEL",
        Version = "0.3.0",
        Status = "Running"
    }));

app.MapPost("/api/jobs", (
    CreateJobRequest request,
    JobService service) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new
        {
            Error = "Job name cannot be empty."
        });
    }

    var job = service.Create(request.Name);

    return Results.Created(
        $"/api/jobs/{job.Id}",
        job);
});

app.MapGet("/api/jobs", (JobService service) =>
    Results.Ok(service.GetAll()));

app.MapGet("/api/jobs/{id:guid}", (
    Guid id,
    JobService service) =>
{
    var job = service.GetById(id);

    return job is null
        ? Results.NotFound()
        : Results.Ok(job);
});

app.MapPost("/api/jobs/{id:guid}/start", (
    Guid id,
    JobService service) =>
{
    var result = service.Start(id);

    return ToHttpResult(
        result,
        "Only pending jobs can be started.");
});

app.MapPost("/api/jobs/{id:guid}/complete", (
    Guid id,
    JobService service) =>
{
    var result = service.Complete(id);

    return ToHttpResult(
        result,
        "Only running jobs can be completed.");
});

app.MapPost("/api/jobs/{id:guid}/cancel", (
    Guid id,
    JobService service) =>
{
    var result = service.Cancel(id);

    return ToHttpResult(
        result,
        "Only pending jobs can be cancelled.");
});

app.Run();

static IResult ToHttpResult(
    JobOperationResult result,
    string invalidStateMessage)
{
    return result.Status switch
    {
        JobOperationStatus.Success =>
            Results.Ok(result.Job),

        JobOperationStatus.NotFound =>
            Results.NotFound(),

        JobOperationStatus.InvalidState =>
            Results.Conflict(new
            {
                Error = invalidStateMessage
            }),

        _ => Results.StatusCode(500)
    };
}

public sealed record CreateJobRequest(string? Name);

public partial class Program { }
