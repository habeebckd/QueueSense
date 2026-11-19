using System.Text.Json;
using JobFlow.Core.Enums;
using JobFlow.Core.Interfaces;
using JobFlow.Core.Models;

namespace JobFlow.Worker.Handlers;

public class ConfigurableLongRunningJobHandler : IJobHandler
{
    public string HandlerType => "ConfigurableLongRunningJobHandler";

    public async Task<JobExecutionResult> ExecuteAsync(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        int totalDurationSeconds = 40;
        int stepIntervalMs = 1000;

        if (!string.IsNullOrWhiteSpace(context.Configuration))
        {
            try
            {
                using var doc = JsonDocument.Parse(context.Configuration);
                if (doc.RootElement.TryGetProperty("durationSeconds", out var dProp))
                    totalDurationSeconds = dProp.GetInt32();
                if (doc.RootElement.TryGetProperty("intervalSeconds", out var iProp))
                    stepIntervalMs = iProp.GetInt32() * 1000;
            }
            catch
            {
                // Fall back to defaults
            }
        }

        await context.LogAsync(LogLevelType.Information, $"Starting Configurable Long-Running Job for {totalDurationSeconds}s. Responds cooperatively to cancellation.");

        int elapsedSeconds = 0;

        while (elapsedSeconds < totalDurationSeconds)
        {
            // Crucial: check cancellation token cooperatively
            cancellationToken.ThrowIfCancellationRequested();

            await Task.Delay(stepIntervalMs, cancellationToken);
            elapsedSeconds += (stepIntervalMs / 1000);

            var pct = Math.Min(99, (int)((double)elapsedSeconds / totalDurationSeconds * 100));

            await context.LogAsync(LogLevelType.Information, $"Heartbeat tick: {elapsedSeconds}/{totalDurationSeconds}s elapsed. Worker lease refreshed.");
            await context.ReportProgressAsync(pct, $"Running step: {elapsedSeconds}/{totalDurationSeconds}s complete.");
        }

        await context.ReportProgressAsync(100, "Long-running job finished all execution cycles.");
        await context.LogAsync(LogLevelType.Information, "All processing intervals completed successfully.");

        var result = new
        {
            durationSeconds = totalDurationSeconds,
            cyclesCompleted = elapsedSeconds,
            completedAt = DateTime.UtcNow
        };

        return JobExecutionResult.SuccessResult(JsonSerializer.Serialize(result));
    }
}
