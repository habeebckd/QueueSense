using JobFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly JobFlowDbContext _dbContext;

    public NotificationsController(JobFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly = false, [FromQuery] int limit = 20)
    {
        limit = Math.Clamp(limit, 1, 100);

        var query = _dbContext.Notifications.AsNoTracking().AsQueryable();

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .Select(n => new
            {
                id = n.Id,
                userId = n.UserId,
                executionId = n.ExecutionId,
                type = n.Type.ToString(),
                message = n.Message,
                isRead = n.IsRead,
                createdAt = n.CreatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, data = notifications });
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var notification = await _dbContext.Notifications.FindAsync(id);
        if (notification == null)
        {
            return NotFound(new { success = false, message = "Notification not found." });
        }

        notification.IsRead = true;
        await _dbContext.SaveChangesAsync();

        return Ok(new { success = true, message = "Notification marked as read." });
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _dbContext.Notifications
            .Where(n => !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

        return Ok(new { success = true, message = "All notifications marked as read." });
    }
}
