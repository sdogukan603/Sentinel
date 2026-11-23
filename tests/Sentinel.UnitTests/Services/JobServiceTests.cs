
using Sentinel.Core.Entities;
using Sentinel.Core.Enums;
using Sentinel.Core.Interfaces;
using Sentinel.Core.Results;
using Sentinel.Core.Services;

namespace Sentinel.UnitTests.Services;

public sealed class JobServiceTests
{
    [Fact]
    public void Create_ValidName_CreatesPendingJob()
    {
        var service = CreateService();

        var job = service.Create("Send Email");

        Assert.Equal("Send Email", job.Name);
        Assert.Equal(JobStatus.Pending, job.Status);
    }

    [Fact]
    public void Create_EmptyName_ThrowsArgumentException()
    {
        var service = CreateService();

        Assert.Throws<ArgumentException>(
            () => service.Create(""));
    }

    [Fact]
    public void Start_PendingJob_ReturnsSuccess()
    {
        var service = CreateService();
        var job = service.Create("Start Test");

        var result = service.Start(job.Id);

        Assert.Equal(JobOperationStatus.Success, result.Status);
        Assert.Equal(JobStatus.Running, result.Job?.Status);
    }

    [Fact]
    public void Start_RunningJob_ReturnsInvalidState()
    {
        var service = CreateService();
        var job = service.Create("Duplicate Start");

        service.Start(job.Id);

        var result = service.Start(job.Id);

        Assert.Equal(JobOperationStatus.InvalidState, result.Status);
    }

    [Fact]
    public void Start_UnknownJob_ReturnsNotFound()
    {
        var service = CreateService();

        var result = service.Start(Guid.NewGuid());

        Assert.Equal(JobOperationStatus.NotFound, result.Status);
    }

    [Fact]
    public void Complete_RunningJob_ReturnsSuccess()
    {
        var service = CreateService();
        var job = service.Create("Complete Test");

        service.Start(job.Id);

        var result = service.Complete(job.Id);

        Assert.Equal(JobOperationStatus.Success, result.Status);
        Assert.Equal(JobStatus.Completed, result.Job?.Status);
    }

    [Fact]
    public void Complete_PendingJob_ReturnsInvalidState()
    {
        var service = CreateService();
        var job = service.Create("Invalid Complete");

        var result = service.Complete(job.Id);

        Assert.Equal(JobOperationStatus.InvalidState, result.Status);
    }

    [Fact]
    public void Cancel_PendingJob_ReturnsSuccess()
    {
        var service = CreateService();
        var job = service.Create("Cancel Test");

        var result = service.Cancel(job.Id);

        Assert.Equal(JobOperationStatus.Success, result.Status);
        Assert.Equal(JobStatus.Cancelled, result.Job?.Status);
    }

    [Fact]
    public void Cancel_RunningJob_ReturnsInvalidState()
    {
        var service = CreateService();
        var job = service.Create("Invalid Cancel");

        service.Start(job.Id);

        var result = service.Cancel(job.Id);

        Assert.Equal(JobOperationStatus.InvalidState, result.Status);
    }

    [Fact]
    public void GetById_ExistingJob_ReturnsJob()
    {
        var service = CreateService();
        var job = service.Create("Find Me");

        var found = service.GetById(job.Id);

        Assert.NotNull(found);
        Assert.Equal(job.Id, found.Id);
    }

    [Fact]
    public void GetAll_ReturnsCreatedJobs()
    {
        var service = CreateService();

        service.Create("Job A");
        service.Create("Job B");

        var jobs = service.GetAll();

        Assert.Equal(2, jobs.Count);
    }

    private static JobService CreateService()
    {
        return new JobService(new FakeJobRepository());
    }

    private sealed class FakeJobRepository : IJobRepository
    {
        private readonly Dictionary<Guid, Job> _jobs = new();

        public Job Add(Job job)
        {
            _jobs.Add(job.Id, job);
            return job;
        }

        public Job? GetById(Guid id)
        {
            return _jobs.GetValueOrDefault(id);
        }

        public IReadOnlyCollection<Job> GetAll()
        {
            return _jobs.Values.ToArray();
        }

        public JobOperationResult TryExecute(
            Guid id,
            Func<Job, JobOperationResult> operation)
        {
            if (!_jobs.TryGetValue(id, out var job))
            {
                return new JobOperationResult(
                    JobOperationStatus.NotFound);
            }

            return operation(job);
        }
    }
}
