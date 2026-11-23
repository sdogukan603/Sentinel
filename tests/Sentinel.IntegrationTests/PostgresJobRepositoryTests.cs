using Microsoft.EntityFrameworkCore;
using Sentinel.Core.Entities;
using Sentinel.Core.Enums;
using Sentinel.Core.Results;
using Sentinel.Core.Services;
using Sentinel.Infrastructure.Persistence;
using Sentinel.Infrastructure.Repositories;
using Xunit;

namespace Sentinel.IntegrationTests;

public sealed class PostgresJobRepositoryTests
{
    [Fact]
    public void Add_ShouldPersistJobAcrossDbContexts()
    {
        var options = TestDatabase.CreateOptions();

        var job = new Job("PostgreSQL persistence test");

        try
        {
            using (var context = new SentinelDbContext(options))
            {
                var repository = new PostgresJobRepository(context);
                repository.Add(job);
            }

            using (var context = new SentinelDbContext(options))
            {
                var repository = new PostgresJobRepository(context);
                var storedJob = repository.GetById(job.Id);

                Assert.NotNull(storedJob);
                Assert.Equal(job.Id, storedJob.Id);
                Assert.Equal(job.Name, storedJob.Name);
                Assert.Equal(job.Status, storedJob.Status);
                Assert.Equal(
    job.CreatedAtUtc,
    storedJob.CreatedAtUtc,
    TimeSpan.FromMicroseconds(1));
            }
        }
        finally
        {
            using var cleanupContext = new SentinelDbContext(options);

            cleanupContext.Jobs
                .Where(storedJob => storedJob.Id == job.Id)
                .ExecuteDelete();
        }
    }
   [Fact]
public void StateTransitions_ShouldPersistAcrossDbContexts()
{
    var options = TestDatabase.CreateOptions();

    var job = new Job("Persistence State Transition Test");

    try
    {
        // 1. Görevi PostgreSQL'e kaydet.
        using (var context = new SentinelDbContext(options))
        {
            var repository = new PostgresJobRepository(context);
            repository.Add(job);
        }

        // 2. Farklı bir DbContext ile görevi başlat.
        using (var context = new SentinelDbContext(options))
        {
            var service = new JobService(
                new PostgresJobRepository(context));

            var result = service.Start(job.Id);

            Assert.Equal(
                JobOperationStatus.Success,
                result.Status);
        }

        // 3. Yeni bir DbContext ile Running durumunu doğrula.
        using (var context = new SentinelDbContext(options))
        {
            var repository = new PostgresJobRepository(context);
            var storedJob = repository.GetById(job.Id);

            Assert.NotNull(storedJob);
            Assert.Equal(JobStatus.Running, storedJob.Status);
            Assert.NotNull(storedJob.StartedAtUtc);
        }

        // 4. Başka bir DbContext ile görevi tamamla.
        using (var context = new SentinelDbContext(options))
        {
            var service = new JobService(
                new PostgresJobRepository(context));

            var result = service.Complete(job.Id);

            Assert.Equal(
                JobOperationStatus.Success,
                result.Status);
        }

        // 5. Son DbContext ile Completed durumunu doğrula.
        using (var context = new SentinelDbContext(options))
        {
            var repository = new PostgresJobRepository(context);
            var storedJob = repository.GetById(job.Id);

            Assert.NotNull(storedJob);
            Assert.Equal(JobStatus.Completed, storedJob.Status);
            Assert.NotNull(storedJob.CompletedAtUtc);
        }
    }
    finally
    {
        // Test verisini temizle.
        using var cleanupContext = new SentinelDbContext(options);

        cleanupContext.Jobs
            .Where(x => x.Id == job.Id)
            .ExecuteDelete();
    }
}
    [Fact]
    public void FailedOperation_ShouldNotLeakChangesIntoLaterSave()
    {
        var options = TestDatabase.CreateOptions();

        var job = new Job("Failed Operation Isolation Test");
        var unrelatedJob = new Job("Unrelated Job");

        try
        {
            using (var context = new SentinelDbContext(options))
            {
                var repository = new PostgresJobRepository(context);

                repository.Add(job);

                // İşlem nesneyi değiştiriyor fakat başarısız sonuç dönüyor.
                var result = repository.TryExecute(job.Id, currentJob =>
                {
                    currentJob.Start();

                    return new JobOperationResult(
                        JobOperationStatus.InvalidState);
                });

                Assert.Equal(
                    JobOperationStatus.InvalidState,
                    result.Status);

                repository.Add(unrelatedJob);

                // Önceki başarısız işlem veritabanına sızmamalı.
            }

            using (var verificationContext = new SentinelDbContext(options))
            {
                var repository =
                    new PostgresJobRepository(verificationContext);

                var storedJob = repository.GetById(job.Id);

                Assert.NotNull(storedJob);
                Assert.Equal(JobStatus.Pending, storedJob.Status);
                Assert.Null(storedJob.StartedAtUtc);
            }
        }
        finally
        {
            using var cleanupContext = new SentinelDbContext(options);

            cleanupContext.Jobs
                .Where(x =>
                    x.Id == job.Id ||
                    x.Id == unrelatedJob.Id)
                .ExecuteDelete();
        }
    }
    [Fact]
    public async Task ConcurrentStart_ShouldAllowOnlyOneSuccessfulTransition()
    {
        var options = TestDatabase.CreateOptions();

        var job = new Job("Concurrent Start Test");

        try
        {
            using (var context = new SentinelDbContext(options))
            {
                new PostgresJobRepository(context).Add(job);
            }

            using var startGate = new ManualResetEventSlim(false);
            using var readyGate = new CountdownEvent(2);

            Task<JobOperationResult> StartTask() => Task.Run(() =>
            {
                using var context = new SentinelDbContext(options);
                var service = new JobService(
                    new PostgresJobRepository(context));

                readyGate.Signal();
                startGate.Wait();

                return service.Start(job.Id);
            });

            var firstTask = StartTask();
            var secondTask = StartTask();

            try
            {
                Assert.True(
                    readyGate.Wait(TimeSpan.FromSeconds(10)),
                    "Both workers should be ready.");
            }
            finally
            {
                startGate.Set();
            }

            var results = await Task.WhenAll(firstTask, secondTask);

            Assert.Single(
                results,
                result => result.Status == JobOperationStatus.Success);

            Assert.Single(
                results,
                result => result.Status == JobOperationStatus.InvalidState);

            using var verificationContext = new SentinelDbContext(options);
            var storedJob = new PostgresJobRepository(verificationContext)
                .GetById(job.Id);

            Assert.NotNull(storedJob);
            Assert.Equal(JobStatus.Running, storedJob.Status);
            Assert.NotNull(storedJob.StartedAtUtc);
        }
        finally
        {
            using var cleanupContext = new SentinelDbContext(options);

            cleanupContext.Jobs
                .Where(x => x.Id == job.Id)
                .ExecuteDelete();
        }
    }
}
