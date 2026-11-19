using JobFlow.Core.Entities;
using JobFlow.Core.Models;
using JobFlow.Infrastructure.Data;
using JobFlow.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SchedulesController : ControllerBase
{
    private readonly JobFlowDbContext _dbContext;

    public SchedulesController(JobFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetSchedules([FromQuery] Guid? jobId = null, [FromQuery] bool? isEnabled = null)
    {
        var query = _dbContext.JobSchedules
            .Include(s => s.JobDefinition)
            .AsNoTracking()
            .AsQueryable();

        if (jobId.HasValue)
        {
            query = query.Where(s => s.JobDefinitionId == jobId.Value);
        }

        if (isEnabled.HasValue)
        {
            query = query.Where(s => s.IsEnabled == isEnabled.Value);
        }

        var schedules = await query
            .OrderBy(s => s.NextRunAt)
            .Select(s => new ScheduleResponse
            {
                Id = s.Id,
                JobDefinitionId = s.JobDefinitionId,
                JobName = s.JobDefinition != null ? s.JobDefinition.Name : string.Empty,
                ScheduleType = s.ScheduleType,
                CronExpression = s.CronExpression,
                TimeZone = s.TimeZone,
                NextRunAt = s.NextRunAt,
                IsEnabled = s.IsEnabled
            })
            .ToListAsync();

        return Ok(new { success = true, data = schedules });
    }

    [HttpPost]
    public async Task<IActionResult> CreateSchedule([FromBody] CreateScheduleRequest request)
    {
        var job = await _dbContext.JobDefinitions.FindAsync(request.JobDefinitionId);
        if (job == null)
        {
            return NotFound(new { success = false, message = "Job definition not found." });
        }

        var schedule = new JobSchedule
        {
            Id = Guid.NewGuid(),
            JobDefinitionId = request.JobDefinitionId,
            ScheduleType = request.ScheduleType,
            CronExpression = request.CronExpression?.Trim(),
            TimeZone = request.TimeZone.Trim(),
            IsEnabled = request.IsEnabled
        };

        schedule.NextRunAt = JobScheduleCalculator.CalculateNextRun(schedule, DateTime.UtcNow);

        _dbContext.JobSchedules.Add(schedule);
        await _dbContext.SaveChangesAsync();

        return StatusCode(201, new
        {
            success = true,
            data = new ScheduleResponse
            {
                Id = schedule.Id,
                JobDefinitionId = schedule.JobDefinitionId,
                JobName = job.Name,
                ScheduleType = schedule.ScheduleType,
                CronExpression = schedule.CronExpression,
                TimeZone = schedule.TimeZone,
                NextRunAt = schedule.NextRunAt,
                IsEnabled = schedule.IsEnabled
            },
            message = "Schedule created successfully."
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateSchedule(Guid id, [FromBody] UpdateScheduleRequest request)
    {
        var schedule = await _dbContext.JobSchedules
            .Include(s => s.JobDefinition)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
        {
            return NotFound(new { success = false, message = "Schedule not found." });
        }

        schedule.ScheduleType = request.ScheduleType;
        schedule.CronExpression = request.CronExpression?.Trim();
        schedule.TimeZone = request.TimeZone.Trim();
        schedule.IsEnabled = request.IsEnabled;
        schedule.NextRunAt = JobScheduleCalculator.CalculateNextRun(schedule, DateTime.UtcNow);

        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            data = new ScheduleResponse
            {
                Id = schedule.Id,
                JobDefinitionId = schedule.JobDefinitionId,
                JobName = schedule.JobDefinition?.Name ?? string.Empty,
                ScheduleType = schedule.ScheduleType,
                CronExpression = schedule.CronExpression,
                TimeZone = schedule.TimeZone,
                NextRunAt = schedule.NextRunAt,
                IsEnabled = schedule.IsEnabled
            },
            message = "Schedule updated successfully."
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSchedule(Guid id)
    {
        var schedule = await _dbContext.JobSchedules.FindAsync(id);
        if (schedule == null)
        {
            return NotFound(new { success = false, message = "Schedule not found." });
        }

        _dbContext.JobSchedules.Remove(schedule);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
