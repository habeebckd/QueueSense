using System.Collections.Concurrent;

namespace JobFlow.Worker.Services;

public class JobCancellationManager
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _activeCts = new();

    public CancellationTokenSource Register(Guid executionId, CancellationToken parentToken)
    {
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken);
        _activeCts[executionId] = linkedCts;
        return linkedCts;
    }

    public bool RequestCancellation(Guid executionId)
    {
        if (_activeCts.TryGetValue(executionId, out var cts))
        {
            try
            {
                cts.Cancel();
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }
        return false;
    }

    public void Unregister(Guid executionId)
    {
        if (_activeCts.TryRemove(executionId, out var cts))
        {
            cts.Dispose();
        }
    }
}
