
using System.Collections.Concurrent;
using Sentinel.Core.Entities;
using Sentinel.Core.Interfaces;
using Sentinel.Core.Results;

namespace Sentinel.Infrastructure.Repositories;

public sealed class InMemoryJobRepository : IJobRepository
{
    private readonly ConcurrentDictionary<Guid, Job> _jobs = new();

    public Job Add(Job job)
    {
        ArgumentNullException.ThrowIfNull(job);

        if (!_jobs.TryAdd(job.Id, job))
        {
            throw new InvalidOperationException(
                "A job with the same ID already exists.");
        }

        return job;
    }

    public Job? GetById(Guid id)
    {
        return _jobs.TryGetValue(id, out var job)
            ? job
            : null;
    }

    public IReadOnlyCollection<Job> GetAll()
    {
        return _jobs.Values.ToArray();
    }

    public JobOperationResult TryExecute(
        Guid id,
        Func<Job, JobOperationResult> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (!_jobs.TryGetValue(id, out var job))
        {
            return new JobOperationResult(
                JobOperationStatus.NotFound);
        }

        lock (job)
        {
            return operation(job);
        }
    }
}
