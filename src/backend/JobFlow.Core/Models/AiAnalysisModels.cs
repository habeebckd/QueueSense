namespace JobFlow.Core.Models;

public class AiAnalysisContext
{
    public Guid ExecutionId { get; set; }
    public string JobName { get; set; } = string.Empty;
    public string HandlerType { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public List<string> RecentLogs { get; set; } = new();
    public double DurationSeconds { get; set; }
    public int AttemptNumber { get; set; }
    public string? JobConfiguration { get; set; }
}

public class AiAnalysisOutput
{
    public string Summary { get; set; } = string.Empty;
    public List<string> ProbableCauses { get; set; } = new();
    public List<string> SuggestedSolutions { get; set; } = new();
    public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical
    public string Confidence { get; set; } = "Medium"; // Low, Medium, High
    public bool RetryRecommended { get; set; }
}
