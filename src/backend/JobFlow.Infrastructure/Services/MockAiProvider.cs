using JobFlow.Core.Interfaces;
using JobFlow.Core.Models;

namespace JobFlow.Infrastructure.Services;

public class MockAiProvider : IAiAnalysisProvider
{
    public string ProviderName => "Mock";

    public Task<AiAnalysisOutput> AnalyzeFailureAsync(AiAnalysisContext context, CancellationToken cancellationToken = default)
    {
        var error = context.ErrorMessage.ToLowerInvariant();
        var logs = string.Join(" ", context.RecentLogs).ToLowerInvariant();

        var output = new AiAnalysisOutput();

        if (error.Contains("timeout") || error.Contains("timed out") || logs.Contains("timeout"))
        {
            output.Summary = $"Execution of '{context.JobName}' failed due to an operation timeout during external resource communication.";
            output.ProbableCauses = new List<string>
            {
                "Database query execution exceeded command timeout limit",
                "High database load causing thread starvation or locked table contention",
                "Network socket latency spike between worker and database server"
            };
            output.SuggestedSolutions = new List<string>
            {
                "Review and optimize slow-running database queries and ensure appropriate indexes are present",
                "Increase CommandTimeout in DbContext options or connection string if large dataset processing is expected",
                "Check PostgreSQL pg_stat_activity for lingering blocking locks and long-running transactions"
            };
            output.Severity = "High";
            output.Confidence = "High";
            output.RetryRecommended = true;
        }
        else if (error.Contains("connection") || error.Contains("socket") || error.Contains("refused") || logs.Contains("connection refused"))
        {
            output.Summary = $"The job failed to establish a network connection to target service or database.";
            output.ProbableCauses = new List<string>
            {
                "Target host is offline, restarting, or unreachable",
                "Connection pool exhausted or port allocation limit reached",
                "Firewall or network routing policy preventing socket handshake"
            };
            output.SuggestedSolutions = new List<string>
            {
                "Verify host and port accessibility using network diagnostic tools (e.g. ping/telnet)",
                "Increase MaxPoolSize in database connection string configuration",
                "Configure exponential backoff and transient retry policy"
            };
            output.Severity = "Critical";
            output.Confidence = "High";
            output.RetryRecommended = true;
        }
        else if (error.Contains("out of memory") || error.Contains("memory") || logs.Contains("outofmemory"))
        {
            output.Summary = $"Job execution aborted because available process memory was exhausted.";
            output.ProbableCauses = new List<string>
            {
                "Unbounded dataset loading into memory without chunking or streaming pagination",
                "Memory leak or retained object graph references in job handler state",
                "Container memory limit reached in Docker environment"
            };
            output.SuggestedSolutions = new List<string>
            {
                "Implement chunked batch processing with AsNoTracking() in EF Core queries",
                "Increase container memory limit in docker-compose.yml",
                "Explicitly dispose IDisposable resources and call GC.Collect if necessary during large batches"
            };
            output.Severity = "Critical";
            output.Confidence = "High";
            output.RetryRecommended = false;
        }
        else if (error.Contains("cancellation") || error.Contains("canceled") || logs.Contains("cancellation"))
        {
            output.Summary = $"Job was stopped cooperatively via user or scheduler cancellation request.";
            output.ProbableCauses = new List<string>
            {
                "Operator clicked Cancel from the dashboard monitor",
                "System shutdown sequence triggered cooperative worker termination"
            };
            output.SuggestedSolutions = new List<string>
            {
                "No troubleshooting required if cancellation was intentionally requested",
                "Restart or retry job if the cancellation was triggered inadvertently"
            };
            output.Severity = "Low";
            output.Confidence = "High";
            output.RetryRecommended = true;
        }
        else
        {
            output.Summary = $"Execution failed due to an unhandled exception in handler '{context.HandlerType}': {context.ErrorMessage.Trim()}";
            output.ProbableCauses = new List<string>
            {
                "Unexpected input data format or null reference in record payload",
                "External dependency contract mismatch or API schema change",
                "Unhandled business logic validation constraint"
            };
            output.SuggestedSolutions = new List<string>
            {
                "Inspect structured execution logs and stack trace for exact line of failure",
                "Validate input parameters against the expected handler JSON schema",
                "Add defensive try-catch error handling around fragile operations"
            };
            output.Severity = "Medium";
            output.Confidence = "Medium";
            output.RetryRecommended = context.AttemptNumber < 3;
        }

        return Task.FromResult(output);
    }
}
