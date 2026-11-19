using JobFlow.Core.Enums;

namespace JobFlow.Core.Interfaces;

public interface IJobExecutionContext
{
    Guid ExecutionId { get; }
    Guid JobId { get; }
    string JobName { get; }
    string? Configuration { get; }
    int AttemptNumber { get; }
    CancellationToken CancellationToken { get; }

    Task ReportProgressAsync(int percentage, string? message = null);
    Task LogAsync(LogLevelType level, string message);
}
