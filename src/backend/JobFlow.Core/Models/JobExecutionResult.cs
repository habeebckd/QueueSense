namespace JobFlow.Core.Models;

public class JobExecutionResult
{
    public bool Success { get; set; }
    public string? OutputJson { get; set; }
    public string? ErrorMessage { get; set; }

    public static JobExecutionResult SuccessResult(string? outputJson = null) => new()
    {
        Success = true,
        OutputJson = outputJson
    };

    public static JobExecutionResult FailureResult(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}
