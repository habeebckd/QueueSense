using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace JobFlow.Infrastructure.Hubs;

[Authorize]
public class JobFlowHub : Hub<IJobFlowHubClient>
{
    public async Task SubscribeToExecution(string executionId)
    {
        if (Guid.TryParse(executionId, out _))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"execution-{executionId}");
        }
    }

    public async Task UnsubscribeFromExecution(string executionId)
    {
        if (Guid.TryParse(executionId, out _))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"execution-{executionId}");
        }
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
