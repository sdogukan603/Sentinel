
using Sentinel.Core.Entities;

namespace Sentinel.Core.Results;

public enum JobOperationStatus
{
    Success,
    NotFound,
    InvalidState
}

public sealed record JobOperationResult(
    JobOperationStatus Status,
    Job? Job = null);
