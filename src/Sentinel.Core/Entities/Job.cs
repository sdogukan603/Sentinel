using Sentinel.Core.Enums;

namespace Sentinel.Core.Entities;

public sealed class Job
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public JobStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    private Job()
    {
        Name = string.Empty;
    }

    public Job(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Job name cannot be empty.", nameof(name));

        Id = Guid.NewGuid();
        Name = name.Trim();
        Status = JobStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Job name cannot be empty.",
                nameof(name));
        }

        name = name.Trim();

        if (name.Length > 200)
        {
            throw new ArgumentException(
                "Job name cannot exceed 200 characters.",
                nameof(name));
        }
    }

    public void Start()
    {
        if (Status != JobStatus.Pending)
            throw new InvalidOperationException(
                $"Cannot start a job in {Status} status.");

        Status = JobStatus.Running;
        StartedAtUtc = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != JobStatus.Running)
            throw new InvalidOperationException(
                $"Cannot complete a job in {Status} status.");

        Status = JobStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void Fail()
    {
        if (Status != JobStatus.Running)
            throw new InvalidOperationException(
                $"Cannot fail a job in {Status} status.");

        Status = JobStatus.Failed;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status != JobStatus.Pending)
            throw new InvalidOperationException(
                $"Cannot cancel a job in {Status} status.");

        Status = JobStatus.Cancelled;
        CompletedAtUtc = DateTime.UtcNow;
    }
}