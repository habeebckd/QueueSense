using JobFlow.Core.Models;

namespace JobFlow.Core.Interfaces;

public interface IAiAnalysisProvider
{
    string ProviderName { get; }
    Task<AiAnalysisOutput> AnalyzeFailureAsync(AiAnalysisContext context, CancellationToken cancellationToken = default);
}
