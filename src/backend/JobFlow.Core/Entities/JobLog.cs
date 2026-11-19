using JobFlow.Core.Enums;

namespace JobFlow.Core.Entities;

public class JobLog
{
    public Guid Id { get; set; }
    public Guid ExecutionId { get; set; }
    public LogLevelType Level { get; set; } = LogLevelType.Information;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public long SequenceNumber { get; set; }

    // Navigation property
    public JobExecution? Execution { get; set; }
}
