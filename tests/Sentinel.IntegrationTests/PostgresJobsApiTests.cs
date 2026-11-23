
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Sentinel.Core.Enums;
using Sentinel.Infrastructure.Persistence;
using Sentinel.Infrastructure.Repositories;
using Xunit;

namespace Sentinel.IntegrationTests;

public sealed class PostgresJobsApiTests
{
    [Fact]
    public async Task CreateJob_ShouldPersistThroughRealHttpApi()
    {
        var options = TestDatabase.CreateOptions();

        var jobId = Guid.Empty;

        try
        {
            using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            var response = await client.PostAsJsonAsync(
                "/api/jobs",
                new { Name = "Real HTTP PostgreSQL Test" });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var createdJob = await response.Content
                .ReadFromJsonAsync<CreatedJobResponse>();

            Assert.NotNull(createdJob);
            jobId = createdJob.Id;
            Assert.NotEqual(Guid.Empty, jobId);

            using var context = new SentinelDbContext(options);
            var repository = new PostgresJobRepository(context);

            var storedJob = repository.GetById(jobId);

            Assert.NotNull(storedJob);
            Assert.Equal(createdJob.Name, storedJob.Name);
            Assert.Equal(JobStatus.Pending, storedJob.Status);
        }
        finally
        {
            if (jobId != Guid.Empty)
            {
                using var cleanupContext =
                    new SentinelDbContext(options);

                await cleanupContext.Jobs
                    .Where(job => job.Id == jobId)
                    .ExecuteDeleteAsync();
            }
        }
    }

    private sealed record CreatedJobResponse(
        Guid Id,
        string Name);
}
