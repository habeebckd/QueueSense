namespace JobFlow.Core.Models;

public class DashboardSummaryResponse
{
    public int TotalJobs { get; set; }
    public int ActiveJobs { get; set; }
    public int RunningExecutions { get; set; }
    public int QueuedExecutions { get; set; }
    public int CompletedExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public double SuccessRatePercentage { get; set; }
    public double AverageDurationSeconds { get; set; }
}

public class MetricPointResponse
{
    public string Timestamp { get; set; } = string.Empty;
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public double AverageDuration { get; set; }
}
