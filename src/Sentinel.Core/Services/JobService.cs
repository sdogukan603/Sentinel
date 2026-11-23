
using Sentinel.Core.Entities;
using Sentinel.Core.Enums;
using Sentinel.Core.Interfaces;
using Sentinel.Core.Results;

namespace Sentinel.Core.Services;

public sealed class JobService
{
    private readonly IJobRepository _repository;

    public JobService(IJobRepository repository)
    {
        _repository = repository;
    }

    public Job Create(string name)
    {
        var job = new Job(name);
        return _repository.Add(job);
    }

    public Job? GetById(Guid id)
    {
        return _repository.GetById(id);
    }

    public IReadOnlyCollection<Job> GetAll()
    {
        return _repository.GetAll();
    }

    public JobOperationResult Start(Guid id)
    {
        return _repository.TryExecute(id, job =>
        {
            if (job.Status != JobStatus.Pending)
                return InvalidState();

            job.Start();
            return Success(job);
        });
    }

    public JobOperationResult Complete(Guid id)
    {
        return _repository.TryExecute(id, job =>
        {
            if (job.Status != JobStatus.Running)
                return InvalidState();

            job.Complete();
            return Success(job);
        });
    }

    public JobOperationResult Cancel(Guid id)
    {
        return _repository.TryExecute(id, job =>
        {
            if (job.Status != JobStatus.Pending)
                return InvalidState();

            job.Cancel();
            return Success(job);
        });
    }

    private static JobOperationResult Success(Job job)
    {
        return new JobOperationResult(
            JobOperationStatus.Success,
            job);
    }

    private static JobOperationResult InvalidState()
    {
        return new JobOperationResult(
            JobOperationStatus.InvalidState);
    }
}
