using System.Text.Json;
using JobFlow.Infrastructure.Data;
using JobFlow.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.Api.Controllers;

[Authorize]
[ApiController]
public class AiAnalysisController : ControllerBase
{
    private readonly JobFlowDbContext _dbContext;
    private readonly AiAnalysisService _aiAnalysisService;

    public AiAnalysisController(JobFlowDbContext dbContext, AiAnalysisService aiAnalysisService)
    {
        _dbContext = dbContext;
        _aiAnalysisService = aiAnalysisService;
    }

    [HttpGet("api/executions/{executionId:guid}/ai-analysis")]
    public async Task<IActionResult> GetAnalysis(Guid executionId)
    {
        var result = await _dbContext.AIAnalysisResults
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.ExecutionId == executionId);

        if (result == null)
        {
            return NotFound(new { success = false, message = "No AI analysis found for this execution." });
        }

        var causes = new List<string>();
        var solutions = new List<string>();

        try
        {
            causes = JsonSerializer.Deserialize<List<string>>(result.ProbableCauses) ?? new();
            solutions = JsonSerializer.Deserialize<List<string>>(result.SuggestedSolutions) ?? new();
        }
        catch
        {
            // fallback
        }

        return Ok(new
        {
            success = true,
            data = new
            {
                id = result.Id,
                executionId = result.ExecutionId,
                summary = result.Summary,
                probableCauses = causes,
                suggestedSolutions = solutions,
                severity = result.Severity,
                confidence = result.Confidence,
                retryRecommended = result.RetryRecommended,
                createdAt = result.CreatedAt
            }
        });
    }

    [HttpPost("api/executions/{executionId:guid}/analyze")]
    public async Task<IActionResult> TriggerAnalysis(Guid executionId)
    {
        var execution = await _dbContext.JobExecutions.FindAsync(executionId);
        if (execution == null)
        {
            return NotFound(new { success = false, message = "Execution not found." });
        }

        var result = await _aiAnalysisService.AnalyzeExecutionFailureAsync(executionId);
        if (result == null)
        {
            return BadRequest(new { success = false, message = "Could not trigger AI analysis." });
        }

        return Accepted(new { success = true, message = "AI analysis triggered successfully." });
    }
}
