using System.ComponentModel.DataAnnotations;
using JobFlow.Core.Enums;

namespace JobFlow.Core.Models;

public class CreateJobRequest
{
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required, StringLength(150)]
    public string HandlerType { get; set; } = string.Empty;

    public string? Configuration { get; set; }

    public bool IsEnabled { get; set; } = true;
}

public class UpdateJobRequest
{
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required, StringLength(150)]
    public string HandlerType { get; set; } = string.Empty;

    public string? Configuration { get; set; }

    public bool IsEnabled { get; set; } = true;
}

public class JobResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string HandlerType { get; set; } = string.Empty;
    public string? Configuration { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public JobStatus? LastExecutionStatus { get; set; }
    public DateTime? LastRunAt { get; set; }
    public List<ScheduleResponse> Schedules { get; set; } = new();
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize > 0 ? PageSize : 10));
}
