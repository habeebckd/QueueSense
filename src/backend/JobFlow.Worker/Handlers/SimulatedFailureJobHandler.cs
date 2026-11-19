using System.Net.Sockets;
using System.Text.Json;
using JobFlow.Core.Enums;
using JobFlow.Core.Interfaces;
using JobFlow.Core.Models;

namespace JobFlow.Worker.Handlers;

public class SimulatedFailureJobHandler : IJobHandler
{
    public string HandlerType => "SimulatedFailureJobHandler";

    public async Task<JobExecutionResult> ExecuteAsync(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        string scenario = "DatabaseConnectionTimeout";
        int failAfterSeconds = 2;

        if (!string.IsNullOrWhiteSpace(context.Configuration))
        {
            try
            {
                using var doc = JsonDocument.Parse(context.Configuration);
                if (doc.RootElement.TryGetProperty("failureScenario", out var scProp))
                    scenario = scProp.GetString() ?? scenario;
                if (doc.RootElement.TryGetProperty("failAfterSeconds", out var secProp))
                    failAfterSeconds = secProp.GetInt32();
            }
            catch
            {
                // Fall back to defaults
            }
        }

        await context.LogAsync(LogLevelType.Information, $"Starting Simulated Failure Job [Scenario: {scenario}]. Running preliminary steps...");
        await context.ReportProgressAsync(15, "Connecting to external resource...");

        await Task.Delay(Math.Max(500, failAfterSeconds * 500), cancellationToken);

        await context.ReportProgressAsync(45, "Executing critical transactional step...");
        await context.LogAsync(LogLevelType.Warning, "Resource response latency exceeding nominal threshold (4,500ms > 500ms)...");

        await Task.Delay(500, cancellationToken);

        switch (scenario.ToLowerInvariant())
        {
            case "databaseconnectiontimeout":
            case "timeout":
                await context.LogAsync(LogLevelType.Error, "Database command timed out waiting for connection lease from pool. Target pool size: 50.");
                throw new TimeoutException("Database command execution timed out after 15,000ms. Unable to acquire lock on table 'financial_transactions'.");

            case "networksocketrefused":
            case "connection":
                await context.LogAsync(LogLevelType.Error, "Socket connection to billing.internal:8443 was actively refused by target host.");
                throw new SocketException((int)SocketError.ConnectionRefused);

            case "outofmemory":
                await context.LogAsync(LogLevelType.Critical, "System memory pressure exceeded 95%. Memory allocation failure during record batching.");
                throw new OutOfMemoryException("Insufficient memory to continue the execution of the program.");

            default:
                await context.LogAsync(LogLevelType.Error, $"Simulated error triggered: {scenario}");
                throw new InvalidOperationException($"Simulated fatal execution failure encountered: {scenario}.");
        }
    }
}
