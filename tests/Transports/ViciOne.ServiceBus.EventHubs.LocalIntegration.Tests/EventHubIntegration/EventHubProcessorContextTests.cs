using System.Reflection;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Primitives;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

public sealed class EventHubProcessorContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-PROCESSOR-LEASE", "callbacks-detach-before-client-is-leased-again")]
    public async Task ReleaseClient_DetachesCallbacksBeforeTheClientIsLeasedAgainAsync()
    {
        var calls = new List<string>();
        var client = new TestEventProcessorClient();
        var context = new EventHubProcessorContext(CreateHostConfiguration(), client,
            _ => RecordAsync(calls, "application:initialize"),
            _ => RecordAsync(calls, "application:close"),
            TestContext.Current.CancellationToken);

        var firstLease = new RecordingBuilderContext(calls, "first");
        Assert.Same(client, context.GetClient(firstLease));
        await client.RaisePartitionInitializingAsync("0");
        await client.RaisePartitionClosingAsync("0");

        context.ReleaseClient();
        context.ReleaseClient();

        var secondLease = new RecordingBuilderContext(calls, "second");
        Assert.Same(client, context.GetClient(secondLease));
        await client.RaisePartitionInitializingAsync("1");
        await client.RaisePartitionClosingAsync("1");

        Assert.Equal([
            "first:initialize",
            "application:initialize",
            "application:close",
            "first:close",
            "second:initialize",
            "application:initialize",
            "application:close",
            "second:close",
        ], calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-PROCESSOR-LEASE", "null-builder-and-overlapping-lease-are-rejected")]
    public void GetClient_RejectsNullAndAnOverlappingLease()
    {
        var context = new EventHubProcessorContext(CreateHostConfiguration(), new TestEventProcessorClient(), null, null,
            TestContext.Current.CancellationToken);

        ArgumentNullException nullContext = Assert.Throws<ArgumentNullException>(() => context.GetClient(null!));
        Assert.Equal("context", nullContext.ParamName);

        var firstLease = new RecordingBuilderContext([], "first");
        context.GetClient(firstLease);

        InvalidOperationException overlappingLease = Assert.Throws<InvalidOperationException>(() =>
            context.GetClient(new RecordingBuilderContext([], "second")));
        Assert.Equal("The processor client already has an active lease.", overlappingLease.Message);

        context.ReleaseClient();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-PROCESSOR-LEASE", "constructor-rejects-missing-required-dependencies")]
    public void Constructor_RejectsMissingRequiredDependencies()
    {
        IHostConfiguration hostConfiguration = CreateHostConfiguration();
        var client = new TestEventProcessorClient();

        ArgumentNullException missingHost = Assert.Throws<ArgumentNullException>(() =>
            new EventHubProcessorContext(null!, client, null, null, CancellationToken.None));
        ArgumentNullException missingClient = Assert.Throws<ArgumentNullException>(() =>
            new EventHubProcessorContext(hostConfiguration, null!, null, null, CancellationToken.None));

        Assert.Equal("hostConfiguration", missingHost.ParamName);
        Assert.Equal("client", missingClient.ParamName);
    }

    private static IHostConfiguration CreateHostConfiguration() =>
        DispatchProxy.Create<IHostConfiguration, UnexpectedInvocationProxy>();

    private static Task RecordAsync(ICollection<string> calls, string value)
    {
        calls.Add(value);
        return Task.CompletedTask;
    }

    private sealed class RecordingBuilderContext(ICollection<string> calls, string name) : ProcessorClientBuilderContext
    {
        public Task OnPartitionInitializingAsync(PartitionInitializingEventArgs eventArgs, CancellationToken cancellationToken = default) =>
            RecordAsync(calls, $"{name}:initialize");

        public Task OnPartitionClosingAsync(PartitionClosingEventArgs eventArgs, CancellationToken cancellationToken = default) =>
            RecordAsync(calls, $"{name}:close");
    }

    private sealed class TestEventProcessorClient : EventProcessorClient
    {
        public Task RaisePartitionInitializingAsync(string partitionId) =>
            OnInitializingPartitionAsync(new TestPartition(partitionId), CancellationToken.None);

        public Task RaisePartitionClosingAsync(string partitionId) =>
            OnPartitionProcessingStoppedAsync(new TestPartition(partitionId), ProcessingStoppedReason.Shutdown, CancellationToken.None);
    }

    private sealed class TestPartition : EventProcessorPartition
    {
        public TestPartition(string partitionId)
        {
            PartitionId = partitionId;
        }
    }

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }
}
