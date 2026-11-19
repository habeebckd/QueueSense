using System.Text.Json;
using JobFlow.Core.Enums;
using JobFlow.Core.Interfaces;
using JobFlow.Core.Models;

namespace JobFlow.Worker.Handlers;

public class SampleDataProcessingJobHandler : IJobHandler
{
    public string HandlerType => "SampleDataProcessingJobHandler";

    public async Task<JobExecutionResult> ExecuteAsync(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        await context.LogAsync(LogLevelType.Information, $"Starting Sample Data Processing for job '{context.JobName}' (Attempt #{context.AttemptNumber}).");
        await context.ReportProgressAsync(5, "Initializing ETL pipeline...");

        await Task.Delay(800, cancellationToken);
        await context.LogAsync(LogLevelType.Information, "Connecting to data source and fetching raw ingestion records.");
        await context.ReportProgressAsync(25, "Extracting records from source stream...");

        await Task.Delay(1000, cancellationToken);
        await context.LogAsync(LogLevelType.Information, "Validating schema integrity and parsing 250 data records.");
        await context.ReportProgressAsync(50, "Transforming and normalizing schema...");

        await Task.Delay(1200, cancellationToken);
        await context.LogAsync(LogLevelType.Information, "Writing transformed data chunks to warehouse destination.");
        await context.ReportProgressAsync(80, "Writing chunks to warehouse...");

        await Task.Delay(800, cancellationToken);
        await context.LogAsync(LogLevelType.Information, "ETL transaction committed. Cleaned up temporary ingestion buffers.");
        await context.ReportProgressAsync(100, "Processing completed successfully.");

        var resultData = new
        {
            recordsProcessed = 250,
            status = "Success",
            committedAt = DateTime.UtcNow,
            source = "CRM Ingestion Pipeline"
        };

        return JobExecutionResult.SuccessResult(JsonSerializer.Serialize(resultData));
    }
}
