namespace JobFlow.Core.Entities;

public class AIAnalysisResult
{
    public Guid Id { get; set; }
    public Guid ExecutionId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string ProbableCauses { get; set; } = "[]"; // Stored as JSON array
    public string SuggestedSolutions { get; set; } = "[]"; // Stored as JSON array
    public string Severity { get; set; } = "Medium";
    public string Confidence { get; set; } = "Medium";
    public bool RetryRecommended { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public JobExecution? Execution { get; set; }
}
