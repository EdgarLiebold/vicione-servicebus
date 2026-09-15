using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Advanced.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Topology;

public sealed class ConsumeTopologyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-CONSUME", "root-convention-current-future-and-duplicate")]
    public void RootConvention_AppliesOnceToExistingAndFutureMessageTopologies()
    {
        var trace = new List<string>();
        var topology = new TestConsumeTopology();
        IMessageConsumeTopologyConfigurator<Message> existing = topology.Get<Message>();
        var convention = new RecordingRootConvention(trace);

        Assert.True(topology.TryAddConvention(convention));
        Assert.False(topology.TryAddConvention(new RecordingRootConvention(trace)));

        IMessageConsumeTopologyConfigurator<SecondMessage> future = topology.Get<SecondMessage>();
        existing.Apply(new RecordingBuilder<ConsumeContext<Message>>());
        future.Apply(new RecordingBuilder<ConsumeContext<SecondMessage>>());

        Assert.Equal(["Message", "SecondMessage"], trace.OrderBy(value => value));
        Assert.Same(existing, topology.GetMessageTopology(typeof(Message)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-CONSUME", "read-only-and-configurator-projections-share-one-topology")]
    public void GenericInterfaceProjections_ReturnTheSameMessageTopologyInstance()
    {
        var topology = new TestConsumeTopology();

        IMessageConsumeTopology<Message> readOnly = ((IConsumeTopology)topology).GetMessageTopology<Message>();
        IMessageConsumeTopologyConfigurator<Message> configurable =
            ((IConsumeTopologyConfigurator)topology).GetMessageTopology<Message>();

        Assert.Same(configurable, readOnly);
        Assert.Same(configurable, topology.GetMessageTopology(typeof(Message)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-CONSUME", "created-topology-projection-branches")]
    public void CreatedTopologyProjection_HandlesEmptySingleAndMultipleCollections()
    {
        var topology = new TestConsumeTopology();

        Assert.True(topology.InvokeAll(_ => false));
        Assert.Empty(topology.InvokeSelectMany(_ => [1]));
        topology.InvokeForEach(_ => throw new InvalidOperationException("No topology should be visited."));

        IMessageConsumeTopologyConfigurator<Message> first = topology.Get<Message>();
        Assert.True(topology.InvokeAll(configuration => ReferenceEquals(configuration, first)));
        Assert.Equal([1], topology.InvokeSelectMany(_ => [1]));

        IMessageConsumeTopologyConfigurator<SecondMessage> second = topology.Get<SecondMessage>();
        Assert.False(topology.InvokeAll(configuration => !ReferenceEquals(configuration, second)));
        Assert.Equal([1, 2], topology.InvokeSelectMany(configuration =>
            ReferenceEquals(configuration, first) ? [1] : [2]).OrderBy(value => value));

        var visited = new HashSet<IMessageConsumeTopologyConfigurator>();
        topology.InvokeForEach(configuration => visited.Add(configuration));
        Assert.Equal(2, visited.Count);
        Assert.Contains(first, visited);
        Assert.Contains(second, visited);
        Assert.Empty(topology.Validate());
    }

    private sealed record Message;

    private sealed record SecondMessage;

    private sealed class RecordingRootConvention(List<string> trace) : IConsumeTopologyConvention
    {
        public bool TryGetMessageConsumeTopologyConvention<T>(
            [NotNullWhen(true)] out IMessageConsumeTopologyConvention<T>? convention)
            where T : class
        {
            convention = new RecordingConvention<T>(trace);
            return true;
        }
    }

    private sealed class RecordingConvention<TMessage>(List<string> trace) : IMessageConsumeTopologyConvention<TMessage>
        where TMessage : class
    {
        public bool TryGetMessageConsumeTopology([NotNullWhen(true)] out IMessageConsumeTopology<TMessage>? messageConsumeTopology)
        {
            messageConsumeTopology = new RecordingTopology<TMessage>(trace);
            return true;
        }

        public bool TryGetMessageConsumeTopologyConvention<T>(
            [NotNullWhen(true)] out IMessageConsumeTopologyConvention<T>? convention)
            where T : class
        {
            convention = this as IMessageConsumeTopologyConvention<T>;
            return convention is not null;
        }
    }

    private sealed class RecordingTopology<TMessage>(List<string> trace) : IMessageConsumeTopology<TMessage>
        where TMessage : class
    {
        public void Apply(ITopologyPipeBuilder<ConsumeContext<TMessage>> builder)
        {
            trace.Add(typeof(TMessage).Name);
        }
    }

    private sealed class RecordingBuilder<TContext> : ITopologyPipeBuilder<TContext>
        where TContext : class, PipeContext
    {
        public bool IsDelegated => false;

        public bool IsImplemented => false;

        public void AddFilter(IFilter<TContext> filter)
        {
        }

        public ITopologyPipeBuilder<TContext> CreateDelegatedBuilder() => new RecordingBuilder<TContext>();
    }

    private sealed class TestConsumeTopology : ConsumeTopology
    {
        internal IMessageConsumeTopologyConfigurator<T> Get<T>()
            where T : class => GetMessageTopology<T>();

        internal bool InvokeAll(Func<IMessageConsumeTopologyConfigurator, bool> callback) => All(callback);

        internal IEnumerable<int> InvokeSelectMany(Func<IMessageConsumeTopologyConfigurator, IEnumerable<int>> selector) =>
            SelectMany(selector);

        internal void InvokeForEach(Action<IMessageConsumeTopologyConfigurator> callback) => ForEach(callback);
    }
}
