
using Sentinel.Core.Entities;
using Sentinel.Core.Enums;
using Sentinel.Core.Results;
using Sentinel.Infrastructure.Repositories;

namespace Sentinel.UnitTests.Repositories;

public sealed class InMemoryJobRepositoryTests
{
    [Fact]
    public void TryExecute_UnknownJob_ReturnsNotFound()
    {
        var repository = new InMemoryJobRepository();

        var result = repository.TryExecute(
            Guid.NewGuid(),
            job => new JobOperationResult(
                JobOperationStatus.Success,
                job));

        Assert.Equal(JobOperationStatus.NotFound, result.Status);
    }

    [Fact]
    public void Add_AndGetById_ReturnsSameJob()
    {
        var repository = new InMemoryJobRepository();
        var job = new Job("Repository Test");

        repository.Add(job);

        var retrieved = repository.GetById(job.Id);

        Assert.Same(job, retrieved);
    }

    [Fact]
    public async Task ConcurrentStart_OnlyOneOperationSucceeds()
    {
        var repository = new InMemoryJobRepository();
        var job = repository.Add(new Job("Concurrent Job"));

        const int workerCount = 50;

        using var gate = new ManualResetEventSlim(false);

        var ready = new CountdownEvent(workerCount);

        var tasks = Enumerable.Range(0, workerCount)
            .Select(_ => Task.Factory.StartNew(
                () =>
                {
                    ready.Signal();
                    gate.Wait();

                    return repository.TryExecute(job.Id, currentJob =>
                    {
                        if (currentJob.Status != JobStatus.Pending)
                        {
                            return new JobOperationResult(
                                JobOperationStatus.InvalidState);
                        }

                        currentJob.Start();

                        return new JobOperationResult(
                            JobOperationStatus.Success,
                            currentJob);
                    });
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default))
            .ToArray();

        try
        {
            Assert.True(
                ready.Wait(TimeSpan.FromSeconds(30)),
                "Workers did not become ready in time.");
        }
        finally
        {
            gate.Set();
        }

        var results = await Task.WhenAll(tasks);

        Assert.Single(
            results,
            x => x.Status == JobOperationStatus.Success);

        Assert.Equal(
            workerCount - 1,
            results.Count(x =>
                x.Status == JobOperationStatus.InvalidState));

        Assert.Equal(JobStatus.Running, job.Status);
    }
}
