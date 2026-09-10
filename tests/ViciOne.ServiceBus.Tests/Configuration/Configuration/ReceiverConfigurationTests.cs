using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration.Configuration;

public sealed class ReceiverConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVER-CONFIGURATION", "shared-settings-topology-and-lifecycle-links-forward-to-endpoint-owner")]
    public async Task Wrapper_ForwardsEverySharedSettingAndLifecycleLinkToTheEndpointOwnerAsync()
    {
        var topology = new InMemoryTopologyConfiguration(InMemoryBus.CreateMessageTopology());
        var bus = new InMemoryBusConfiguration(topology, new Uri("loopback://localhost/"));
        IInMemoryReceiveEndpointConfiguration endpoint =
            bus.HostConfiguration.CreateReceiveEndpointConfiguration("receiver-wrapper");
        var wrapper = new ReceivePipeDispatcherConfiguration(bus.HostConfiguration, endpoint);
        var dependency = new ControlledDependency();
        var dependent = new ControlledDependent();

        wrapper.ConfigureConsumeTopology = false;
        wrapper.PublishFaults = false;
        wrapper.ConfigureMessageTopology<GenericMessage>(false);
        wrapper.ConfigureMessageTopology(typeof(RuntimeMessage), false);
        wrapper.AddDependency(dependency);
        wrapper.AddDependent(dependent);

        Assert.False(endpoint.ConfigureConsumeTopology);
        Assert.False(endpoint.PublishFaults);
        Assert.False(endpoint.Topology.Consume.GetMessageTopology<GenericMessage>().ConfigureConsumeTopology);
        Assert.False(endpoint.Topology.Consume.GetMessageTopology(typeof(RuntimeMessage)).ConfigureConsumeTopology);
        Assert.False(endpoint.DependenciesReady.IsCompleted);
        Assert.False(endpoint.DependentsCompleted.IsCompleted);

        dependency.MarkReady();
        dependent.MarkCompleted();

        await endpoint.DependenciesReady.WaitAsync(TestContext.Current.CancellationToken);
        await endpoint.DependentsCompleted.WaitAsync(TestContext.Current.CancellationToken);
    }

    private sealed record GenericMessage;

    private sealed record RuntimeMessage;

    private sealed class ControlledDependency : IReceiveEndpointDependency
    {
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Ready => _ready.Task;

        public void MarkReady() => _ready.SetResult();
    }

    private sealed class ControlledDependent : IReceiveEndpointDependent
    {
        private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Completed => _completed.Task;

        public void MarkCompleted() => _completed.SetResult();
    }
}
