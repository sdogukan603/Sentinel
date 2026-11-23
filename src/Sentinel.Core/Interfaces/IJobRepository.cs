
using Sentinel.Core.Entities;
using Sentinel.Core.Results;

namespace Sentinel.Core.Interfaces;

public interface IJobRepository
{
	Job Add(Job job);
	Job? GetById(Guid id);
	IReadOnlyCollection<Job> GetAll();

	JobOperationResult TryExecute(
		Guid id,
		Func<Job, JobOperationResult> operation);
}
