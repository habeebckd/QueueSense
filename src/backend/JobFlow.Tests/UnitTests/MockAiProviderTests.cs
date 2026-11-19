using JobFlow.Core.Models;
using JobFlow.Infrastructure.Services;
using Xunit;

namespace JobFlow.Tests.UnitTests;

public class MockAiProviderTests
{
    private readonly MockAiProvider _provider = new();

    [Fact]
    public async Task AnalyzeFailureAsync_ShouldIdentify_TimeoutException()
    {
        var context = new AiAnalysisContext
        {
            ExecutionId = Guid.NewGuid(),
            JobName = "Nightly Reconciliation",
            HandlerType = "SampleDataProcessingJobHandler",
            ErrorMessage = "Database command execution timed out after 15,000ms.",
            RecentLogs = new List<string> { "Querying database...", "Timeout detected." },
            DurationSeconds = 15.2,
            AttemptNumber = 1
        };

        var output = await _provider.AnalyzeFailureAsync(context);

        Assert.NotNull(output);
        Assert.Equal("High", output.Severity);
        Assert.Equal("High", output.Confidence);
        Assert.True(output.RetryRecommended);
        Assert.Contains(output.ProbableCauses, c => c.ToLower().Contains("timeout") || c.ToLower().Contains("database"));
    }

    [Fact]
    public async Task AnalyzeFailureAsync_ShouldIdentify_ConnectionRefused()
    {
        var context = new AiAnalysisContext
        {
            ExecutionId = Guid.NewGuid(),
            JobName = "Payment Sync",
            HandlerType = "BatchRecordProcessingJobHandler",
            ErrorMessage = "No connection could be made because the target machine actively refused it (socket error).",
            RecentLogs = new List<string> { "Attempting handshake..." },
            DurationSeconds = 1.0,
            AttemptNumber = 1
        };

        var output = await _provider.AnalyzeFailureAsync(context);

        Assert.NotNull(output);
        Assert.Equal("Critical", output.Severity);
        Assert.True(output.RetryRecommended);
        Assert.Contains(output.ProbableCauses, c => c.ToLower().Contains("connection") || c.ToLower().Contains("offline"));
    }

    [Fact]
    public async Task AnalyzeFailureAsync_ShouldIdentify_OutOfMemory()
    {
        var context = new AiAnalysisContext
        {
            ExecutionId = Guid.NewGuid(),
            JobName = "Heavy Aggregator",
            HandlerType = "ReportGenerationJobHandler",
            ErrorMessage = "Exception of type 'System.OutOfMemoryException' was thrown.",
            RecentLogs = new List<string> { "Memory allocation failure" },
            DurationSeconds = 8.5,
            AttemptNumber = 1
        };

        var output = await _provider.AnalyzeFailureAsync(context);

        Assert.NotNull(output);
        Assert.Equal("Critical", output.Severity);
        Assert.False(output.RetryRecommended); // Should not retry immediately without code or resource fix
    }
}
