using JobFlow.Core.Interfaces;
using JobFlow.Worker.Handlers;
using Xunit;

namespace JobFlow.Tests.UnitTests;

public class JobHandlerRegistryTests
{
    [Fact]
    public void Registry_ShouldAllowlistAndRetrieve_RegisteredHandlers()
    {
        var handlers = new IJobHandler[]
        {
            new SampleDataProcessingJobHandler(),
            new BatchRecordProcessingJobHandler(),
            new ReportGenerationJobHandler(),
            new SimulatedFailureJobHandler(),
            new ConfigurableLongRunningJobHandler()
        };

        var registry = new JobHandlerRegistry(handlers);

        Assert.Equal(5, registry.GetRegisteredHandlerTypes().Count);

        var found = registry.TryGetHandler("SampleDataProcessingJobHandler", out var handler);
        Assert.True(found);
        Assert.NotNull(handler);
        Assert.Equal("SampleDataProcessingJobHandler", handler.HandlerType);
    }

    [Fact]
    public void Registry_ShouldReject_UnregisteredHandler()
    {
        var handlers = new IJobHandler[]
        {
            new SampleDataProcessingJobHandler()
        };

        var registry = new JobHandlerRegistry(handlers);

        var found = registry.TryGetHandler("ArbitraryMaliciousHandler", out var handler);
        Assert.False(found);
        Assert.Null(handler);
    }
}
