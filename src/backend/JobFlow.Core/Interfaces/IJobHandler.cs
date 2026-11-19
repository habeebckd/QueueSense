using JobFlow.Core.Models;

namespace JobFlow.Core.Interfaces;

public interface IJobHandler
{
    string HandlerType { get; }
    Task<JobExecutionResult> ExecuteAsync(IJobExecutionContext context, CancellationToken cancellationToken);
}
