using System.Security.Claims;
using JobFlow.Core.Entities;
using JobFlow.Core.Enums;
using JobFlow.Core.Models;
using JobFlow.Infrastructure.Data;
using JobFlow.Worker.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly JobFlowDbContext _dbContext;
    private readonly JobHandlerRegistry _handlerRegistry;

    public JobsController(JobFlowDbContext dbContext, JobHandlerRegistry handlerRegistry)
    {
        _dbContext = dbContext;
        _handlerRegistry = handlerRegistry;
    }

    [HttpGet]
    public async Task<IActionResult> GetJobs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool? isEnabled = null,
        [FromQuery] string? handlerType = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.JobDefinitions
            .Include(j => j.Schedules)
            .Include(j => j.Executions.OrderByDescending(e => e.QueuedAt).Take(1))
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(j => j.Name.ToLower().Contains(searchLower) ||
                                     (j.Description != null && j.Description.ToLower().Contains(searchLower)));
        }

        if (isEnabled.HasValue)
        {
            query = query.Where(j => j.IsEnabled == isEnabled.Value);
        }

        if (!string.IsNullOrWhiteSpace(handlerType))
        {
            query = query.Where(j => j.HandlerType == handlerType);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new JobResponse
            {
                Id = j.Id,
                Name = j.Name,
                Description = j.Description,
                HandlerType = j.HandlerType,
                Configuration = j.Configuration,
                IsEnabled = j.IsEnabled,
                CreatedAt = j.CreatedAt,
                UpdatedAt = j.UpdatedAt,
                LastExecutionStatus = j.Executions.Select(e => (JobStatus?)e.Status).FirstOrDefault(),
                LastRunAt = j.Executions.Select(e => (DateTime?)e.QueuedAt).FirstOrDefault(),
                Schedules = j.Schedules.Select(s => new ScheduleResponse
                {
                    Id = s.Id,
                    JobDefinitionId = s.JobDefinitionId,
                    JobName = j.Name,
                    ScheduleType = s.ScheduleType,
                    CronExpression = s.CronExpression,
                    TimeZone = s.TimeZone,
                    NextRunAt = s.NextRunAt,
                    IsEnabled = s.IsEnabled
                }).ToList()
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = new PagedResult<JobResponse>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            }
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetJobById(Guid id)
    {
        var job = await _dbContext.JobDefinitions
            .Include(j => j.Schedules)
            .Include(j => j.Executions.OrderByDescending(e => e.QueuedAt).Take(5))
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null)
        {
            return NotFound(new { success = false, message = "Job definition not found." });
        }

        var response = new JobResponse
        {
            Id = job.Id,
            Name = job.Name,
            Description = job.Description,
            HandlerType = job.HandlerType,
            Configuration = job.Configuration,
            IsEnabled = job.IsEnabled,
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt,
            LastExecutionStatus = job.Executions.Select(e => (JobStatus?)e.Status).FirstOrDefault(),
            LastRunAt = job.Executions.Select(e => (DateTime?)e.QueuedAt).FirstOrDefault(),
            Schedules = job.Schedules.Select(s => new ScheduleResponse
            {
                Id = s.Id,
                JobDefinitionId = s.JobDefinitionId,
                JobName = job.Name,
                ScheduleType = s.ScheduleType,
                CronExpression = s.CronExpression,
                TimeZone = s.TimeZone,
                NextRunAt = s.NextRunAt,
                IsEnabled = s.IsEnabled
            }).ToList()
        };

        return Ok(new { success = true, data = response });
    }

    [HttpPost]
    public async Task<IActionResult> CreateJob([FromBody] CreateJobRequest request)
    {
        // Enforce allowlist check
        if (!_handlerRegistry.TryGetHandler(request.HandlerType, out _))
        {
            var allowed = string.Join(", ", _handlerRegistry.GetRegisteredHandlerTypes());
            return BadRequest(new
            {
                success = false,
                message = $"HandlerType '{request.HandlerType}' is not allowed. Allowlisted handlers: {allowed}"
            });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;
        Guid.TryParse(userIdClaim, out var userId);

        var job = new JobDefinition
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            HandlerType = request.HandlerType.Trim(),
            Configuration = request.Configuration,
            IsEnabled = request.IsEnabled,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.JobDefinitions.Add(job);
        await _dbContext.SaveChangesAsync();

        return StatusCode(201, new
        {
            success = true,
            data = new JobResponse
            {
                Id = job.Id,
                Name = job.Name,
                Description = job.Description,
                HandlerType = job.HandlerType,
                Configuration = job.Configuration,
                IsEnabled = job.IsEnabled,
                CreatedAt = job.CreatedAt,
                UpdatedAt = job.UpdatedAt
            },
            message = "Job created successfully."
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateJob(Guid id, [FromBody] UpdateJobRequest request)
    {
        var job = await _dbContext.JobDefinitions.FindAsync(id);
        if (job == null)
        {
            return NotFound(new { success = false, message = "Job definition not found." });
        }

        if (!_handlerRegistry.TryGetHandler(request.HandlerType, out _))
        {
            return BadRequest(new { success = false, message = $"HandlerType '{request.HandlerType}' is not registered." });
        }

        job.Name = request.Name.Trim();
        job.Description = request.Description?.Trim();
        job.HandlerType = request.HandlerType.Trim();
        job.Configuration = request.Configuration;
        job.IsEnabled = request.IsEnabled;
        job.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            data = new JobResponse
            {
                Id = job.Id,
                Name = job.Name,
                Description = job.Description,
                HandlerType = job.HandlerType,
                Configuration = job.Configuration,
                IsEnabled = job.IsEnabled,
                CreatedAt = job.CreatedAt,
                UpdatedAt = job.UpdatedAt
            },
            message = "Job updated successfully."
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteJob(Guid id)
    {
        var job = await _dbContext.JobDefinitions.FindAsync(id);
        if (job == null)
        {
            return NotFound(new { success = false, message = "Job definition not found." });
        }

        _dbContext.JobDefinitions.Remove(job);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
