using System.Text.Json;
using JobFlow.Core.Enums;
using JobFlow.Core.Interfaces;
using JobFlow.Core.Models;

namespace JobFlow.Worker.Handlers;

public class ReportGenerationJobHandler : IJobHandler
{
    public string HandlerType => "ReportGenerationJobHandler";

    public async Task<JobExecutionResult> ExecuteAsync(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        await context.LogAsync(LogLevelType.Information, $"Compiling analytics dataset for report '{context.JobName}'.");
        await context.ReportProgressAsync(10, "Querying metrics data marts...");

        await Task.Delay(700, cancellationToken);
        await context.LogAsync(LogLevelType.Information, "Aggregated 15,400 transaction records across 4 dimensions.");
        await context.ReportProgressAsync(40, "Calculating statistical distributions and percentiles...");

        await Task.Delay(900, cancellationToken);
        await context.LogAsync(LogLevelType.Information, "Rendering PDF summary charts and data tables.");
        await context.ReportProgressAsync(70, "Compiling PDF document layout...");

        await Task.Delay(800, cancellationToken);
        var artifactName = $"Report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
        await context.LogAsync(LogLevelType.Information, $"Report artifact generated: '{artifactName}' (Size: 1.42 MB). Uploaded to storage.");
        await context.ReportProgressAsync(100, "Report compiled and published.");

        var result = new
        {
            artifactName = artifactName,
            format = "PDF",
            fileSizeBytes = 1488976,
            downloadUrl = $"/api/reports/{artifactName}",
            generatedAt = DateTime.UtcNow
        };

        return JobExecutionResult.SuccessResult(JsonSerializer.Serialize(result));
    }
}
