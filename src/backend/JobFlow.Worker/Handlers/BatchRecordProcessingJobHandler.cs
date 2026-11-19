using System.Text.Json;
using JobFlow.Core.Enums;
using JobFlow.Core.Interfaces;
using JobFlow.Core.Models;

namespace JobFlow.Worker.Handlers;

public class BatchRecordProcessingJobHandler : IJobHandler
{
    public string HandlerType => "BatchRecordProcessingJobHandler";

    public async Task<JobExecutionResult> ExecuteAsync(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        int totalRecords = 200;
        int chunkSize = 40;

        if (!string.IsNullOrWhiteSpace(context.Configuration))
        {
            try
            {
                using var doc = JsonDocument.Parse(context.Configuration);
                if (doc.RootElement.TryGetProperty("totalRecords", out var trProp))
                    totalRecords = trProp.GetInt32();
                if (doc.RootElement.TryGetProperty("chunkSize", out var csProp))
                    chunkSize = csProp.GetInt32();
            }
            catch
            {
                // Fall back to defaults
            }
        }

        await context.LogAsync(LogLevelType.Information, $"Starting Batch Record Processing: {totalRecords} records in chunks of {chunkSize}.");

        int processed = 0;
        int chunkNumber = 1;

        while (processed < totalRecords)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentBatch = Math.Min(chunkSize, totalRecords - processed);
            await Task.Delay(500, cancellationToken);

            processed += currentBatch;
            var pct = (int)((double)processed / totalRecords * 100);

            await context.LogAsync(LogLevelType.Information, $"Chunk {chunkNumber} committed: {processed}/{totalRecords} records processed.");
            await context.ReportProgressAsync(pct, $"Processed {processed} of {totalRecords} records.");

            chunkNumber++;
        }

        await context.LogAsync(LogLevelType.Information, $"Batch processing completed successfully. All {totalRecords} records reconciled.");

        var result = new
        {
            totalProcessed = totalRecords,
            batches = chunkNumber - 1,
            completedAt = DateTime.UtcNow
        };

        return JobExecutionResult.SuccessResult(JsonSerializer.Serialize(result));
    }
}
