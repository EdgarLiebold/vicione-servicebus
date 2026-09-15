using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Topology;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Topology;

public sealed class PublishTopologyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-ROOTS", "publish-convention-current-future-and-duplicate")]
    public void RootConvention_AppliesOnceToExistingAndFutureMessageTopologies()
    {
        var trace = new List<string>();
        var topology = new TestPublishTopology();
        IMessagePublishTopologyConfigurator<Message> existing = topology.Get<Message>();
        var convention = new RecordingRootConvention(trace);

        Assert.True(topology.TryAddConvention(convention));
        Assert.False(topology.TryAddConvention(new RecordingRootConvention(trace)));

        IMessagePublishTopologyConfigurator<SecondMessage> future = topology.Get<SecondMessage>();
        existing.Apply(new RecordingBuilder<PublishContext<Message>>());
        future.Apply(new RecordingBuilder<PublishContext<SecondMessage>>());

        Assert.Equal(["Message", "SecondMessage"], trace.OrderBy(value => value));
        Assert.Same(existing, topology.GetMessageTopology(typeof(Message)));
        Assert.Same(existing, ((IPublishTopology)topology).GetMessageTopology<Message>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-ROOTS", "publish-implemented-message-contracts")]
    public void ConcreteMessageTopology_AlsoCreatesEachImplementedMessageTopology()
    {
        var topology = new TestPublishTopology();

        IMessagePublishTopologyConfigurator<ConcreteMessage> concrete = topology.Get<ConcreteMessage>();
        var visited = new HashSet<IMessagePublishTopologyConfigurator>();
        topology.Visit(configuration => visited.Add(configuration));

        Assert.Equal(2, visited.Count);
        Assert.Contains(concrete, visited);
        Assert.Contains(topology.Get<IBaseMessage>(), visited);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-ROOTS", "publish-created-topology-enumeration")]
    public void CreatedTopologyEnumeration_VisitsEveryTopologyAndValidatesThem()
    {
        var topology = new TestPublishTopology();

        topology.Visit(_ => throw new InvalidOperationException("No topology should be visited."));
        IMessagePublishTopologyConfigurator<Message> first = topology.Get<Message>();
        IMessagePublishTopologyConfigurator<SecondMessage> second = topology.Get<SecondMessage>();

        var visited = new HashSet<IMessagePublishTopologyConfigurator>();
        topology.Visit(configuration => visited.Add(configuration));

        Assert.Equal(2, visited.Count);
        Assert.Contains(first, visited);
        Assert.Contains(second, visited);
        Assert.Empty(topology.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-ROOTS", "publish-runtime-contract-validation-and-default-address")]
    public void RuntimeLookup_RejectsInvalidContractsAndHasNoTransportIndependentAddress()
    {
        var topology = new PublishTopology();

        Assert.Equal("messageType", Assert.Throws<ArgumentException>(
            () => topology.GetMessageTopology(typeof(int))).ParamName);
        Assert.False(topology.TryGetPublishAddress(
            typeof(Message),
            new Uri("loopback://localhost"),
            out Uri? publishAddress));
        Assert.Null(publishAddress);
    }

    private sealed record Message;

    private sealed record SecondMessage;

    private interface IBaseMessage;

    private sealed record ConcreteMessage : IBaseMessage;

    private sealed class RecordingRootConvention(List<string> trace) : IPublishTopologyConvention
    {
        public bool TryGetMessagePublishTopologyConvention<T>(
            [NotNullWhen(true)] out IMessagePublishTopologyConvention<T>? convention)
            where T : class
        {
            convention = new RecordingConvention<T>(trace);
            return true;
        }
    }

    private sealed class RecordingConvention<TMessage>(List<string> trace) : IMessagePublishTopologyConvention<TMessage>
        where TMessage : class
    {
        public bool TryGetMessagePublishTopology([NotNullWhen(true)] out IMessagePublishTopology<TMessage>? messagePublishTopology)
        {
            messagePublishTopology = new RecordingTopology<TMessage>(trace);
            return true;
        }

        public bool TryGetMessagePublishTopologyConvention<T>(
            [NotNullWhen(true)] out IMessagePublishTopologyConvention<T>? convention)
            where T : class
        {
            convention = this as IMessagePublishTopologyConvention<T>;
            return convention is not null;
        }
    }

    private sealed class RecordingTopology<TMessage>(List<string> trace) : IMessagePublishTopology<TMessage>
        where TMessage : class
    {
        public bool Exclude => false;

        public void Apply(ITopologyPipeBuilder<PublishContext<TMessage>> builder)
        {
            trace.Add(typeof(TMessage).Name);
        }

        public bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress)
        {
            publishAddress = null;
            return false;
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

    private sealed class TestPublishTopology : PublishTopology
    {
        internal IMessagePublishTopologyConfigurator<T> Get<T>()
            where T : class => GetMessageTopology<T>();

        internal void Visit(Action<IMessagePublishTopologyConfigurator> callback) => ForEachMessageType(callback);
    }
}
