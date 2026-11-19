using JobFlow.Core.Enums;
using JobFlow.Core.Models;
using JobFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly JobFlowDbContext _dbContext;

    public DashboardController(JobFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var totalJobs = await _dbContext.JobDefinitions.CountAsync();
        var activeJobs = await _dbContext.JobDefinitions.CountAsync(j => j.IsEnabled);

        var queuedCount = await _dbContext.JobExecutions.CountAsync(e => e.Status == JobStatus.Queued);
        var runningCount = await _dbContext.JobExecutions.CountAsync(e => e.Status == JobStatus.Running || e.Status == JobStatus.CancellationRequested);
        var completedCount = await _dbContext.JobExecutions.CountAsync(e => e.Status == JobStatus.Completed);
        var failedCount = await _dbContext.JobExecutions.CountAsync(e => e.Status == JobStatus.Failed || e.Status == JobStatus.TimedOut);

        var terminalTotal = completedCount + failedCount;
        var successRate = terminalTotal > 0
            ? Math.Round((double)completedCount / terminalTotal * 100, 2)
            : 100.0;

        var avgDuration = await _dbContext.JobExecutions
            .Where(e => e.Status == JobStatus.Completed && e.Duration != null)
            .Select(e => e.Duration!.Value)
            .DefaultIfEmpty(0.0)
            .AverageAsync();

        var summary = new DashboardSummaryResponse
        {
            TotalJobs = totalJobs,
            ActiveJobs = activeJobs,
            QueuedExecutions = queuedCount,
            RunningExecutions = runningCount,
            CompletedExecutions = completedCount,
            FailedExecutions = failedCount,
            SuccessRatePercentage = successRate,
            AverageDurationSeconds = Math.Round(avgDuration, 2)
        };

        return Ok(new { success = true, data = summary });
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics([FromQuery] string timeRange = "24h")
    {
        var since = DateTime.UtcNow.AddHours(-24);

        var executions = await _dbContext.JobExecutions
            .Where(e => e.QueuedAt >= since)
            .Select(e => new { e.QueuedAt, e.Status, e.Duration })
            .ToListAsync();

        var metricPoints = new List<MetricPointResponse>();

        // Group by 4-hour intervals for smooth chart visualization
        for (int i = 24; i >= 0; i -= 4)
        {
            var bucketStart = DateTime.UtcNow.AddHours(-i);
            var bucketEnd = bucketStart.AddHours(4);

            var bucketExecutions = executions
                .Where(e => e.QueuedAt >= bucketStart && e.QueuedAt < bucketEnd)
                .ToList();

            var successCount = bucketExecutions.Count(e => e.Status == JobStatus.Completed);
            var failureCount = bucketExecutions.Count(e => e.Status == JobStatus.Failed || e.Status == JobStatus.TimedOut);
            var avgDur = bucketExecutions
                .Where(e => e.Duration != null)
                .Select(e => e.Duration!.Value)
                .DefaultIfEmpty(0.0)
                .Average();

            metricPoints.Add(new MetricPointResponse
            {
                Timestamp = bucketStart.ToString("HH:mm"),
                SuccessCount = successCount,
                FailureCount = failureCount,
                AverageDuration = Math.Round(avgDur, 2)
            });
        }

        return Ok(new { success = true, data = metricPoints });
    }
}
