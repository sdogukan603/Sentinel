
using System.Data;
using Microsoft.EntityFrameworkCore;
using Sentinel.Core.Entities;
using Sentinel.Core.Interfaces;
using Sentinel.Core.Results;
using Sentinel.Infrastructure.Persistence;

namespace Sentinel.Infrastructure.Repositories;

public sealed class PostgresJobRepository : IJobRepository
{
    private readonly SentinelDbContext _context;

    public PostgresJobRepository(SentinelDbContext context)
    {
        _context = context;
    }

    public Job Add(Job job)
    {
        ArgumentNullException.ThrowIfNull(job);

        _context.Jobs.Add(job);
        _context.SaveChanges();

        return job;
    }

    public Job? GetById(Guid id)
    {
        return _context.Jobs
            .AsNoTracking()
            .FirstOrDefault(job => job.Id == id);
    }

    public IReadOnlyCollection<Job> GetAll()
    {
        return _context.Jobs
            .AsNoTracking()
            .OrderBy(job => job.CreatedAtUtc)
            .ToArray();
    }

    public JobOperationResult TryExecute(
        Guid id,
        Func<Job, JobOperationResult> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        using var transaction = _context.Database.BeginTransaction(
            IsolationLevel.ReadCommitted);

        var job = _context.Jobs
            .FromSqlInterpolated(
                $"SELECT * FROM jobs WHERE id = {id} FOR UPDATE")
            .AsTracking()
            .SingleOrDefault();

        if (job is null)
        {
            return new JobOperationResult(
                JobOperationStatus.NotFound);
        }

        var result = operation(job);

        if (result.Status == JobOperationStatus.Success)
        {
            _context.SaveChanges();
            transaction.Commit();
        }
        else
        {
            // Baþarýsýz iþlemde deðiþtirilen Job nesnesinin
            // daha sonraki SaveChanges çaðrýlarýna sýzmasýný önle.
            _context.Entry(job).State = EntityState.Detached;
        }

        return result;
    }
}
