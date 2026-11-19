using JobFlow.Core.Interfaces;

namespace JobFlow.Worker.Handlers;

public class JobHandlerRegistry
{
    private readonly Dictionary<string, IJobHandler> _handlers;

    public JobHandlerRegistry(IEnumerable<IJobHandler> handlers)
    {
        _handlers = handlers.ToDictionary(h => h.HandlerType, StringComparer.OrdinalIgnoreCase);
    }

    public bool TryGetHandler(string handlerType, out IJobHandler handler)
    {
        return _handlers.TryGetValue(handlerType, out handler!);
    }

    public IReadOnlyCollection<string> GetRegisteredHandlerTypes()
    {
        return _handlers.Keys;
    }
}
