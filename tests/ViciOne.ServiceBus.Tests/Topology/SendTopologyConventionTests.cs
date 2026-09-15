using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using ViciOne.ServiceBus.Advanced.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Topology;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Topology;

public sealed class SendTopologyConventionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-CORRELATION", "interface-resolver-and-send-topology")]
    public void CorrelationConvention_InfersInterfaceResolverAndCreatesItsSendFilter()
    {
        var convention = new CorrelationIdMessageSendTopologyConvention<CorrelatedMessage>();
        var typed = (IMessageSendTopologyConvention<CorrelatedMessage>)convention;
        var untyped = (IMessageSendTopologyConvention)convention;

        Assert.True(convention.TryGetCorrelationIdResolver(out IMessageCorrelationId<CorrelatedMessage>? resolver));
        Guid expected = Guid.Parse("ef30a4d4-aa5a-49cc-92ac-1b839ea0a4ce");
        Assert.False(resolver.TryGetCorrelationId(new CorrelatedMessage(Guid.Empty), out Guid empty));
        Assert.Equal(Guid.Empty, empty);
        Assert.True(resolver.TryGetCorrelationId(new CorrelatedMessage(expected), out Guid actual));
        Assert.Equal(expected, actual);
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(
            () => resolver.TryGetCorrelationId(null!, out _)).ParamName);

        Assert.True(typed.TryGetMessageSendTopology(out IMessageSendTopology<CorrelatedMessage>? topology));
        var builder = new CapturingBuilder<SendContext<CorrelatedMessage>>();
        topology.Apply(builder);
        Assert.IsType<SetCorrelationIdFilter<CorrelatedMessage>>(Assert.Single(builder.Filters));

        Assert.True(untyped.TryGetMessageSendTopologyConvention(
            out IMessageSendTopologyConvention<CorrelatedMessage>? projected));
        Assert.Same(convention, projected);
        Assert.False(untyped.TryGetMessageSendTopologyConvention(
            out IMessageSendTopologyConvention<PlainMessage>? incompatible));
        Assert.Null(incompatible);

        var missing = new CorrelationIdMessageSendTopologyConvention<PlainMessage>();
        Assert.False(((IMessageSendTopologyConvention<PlainMessage>)missing)
            .TryGetMessageSendTopology(out IMessageSendTopology<PlainMessage>? missingTopology));
        Assert.Null(missingTopology);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-TOPOLOGY-CONVENTION", "partition-convention-lifecycle")]
    public void PartitionConvention_ProjectsCachesAndCreatesTopologyOnlyAfterConfiguration()
    {
        var root = new PartitionKeySendTopologyConvention();
        var rootConvention = (IMessageSendTopologyConvention)root;
        Assert.True(rootConvention.TryGetMessageSendTopologyConvention(
            out IMessageSendTopologyConvention<Message>? first));
        Assert.True(rootConvention.TryGetMessageSendTopologyConvention(
            out IMessageSendTopologyConvention<Message>? second));
        Assert.Same(first, second);
        var configurable = Assert.IsAssignableFrom<IPartitionKeyMessageSendTopologyConvention<Message>>(first);

        Assert.False(first.TryGetMessageSendTopology(out IMessageSendTopology<Message>? absent));
        Assert.Null(absent);
        configurable.SetFormatter(new MessagePartitionFormatter());
        Assert.True(first.TryGetMessageSendTopology(out IMessageSendTopology<Message>? topology));
        var builder = new CapturingBuilder<SendContext<Message>>();
        topology.Apply(builder);
        Assert.IsType<SetPartitionKeyFilter<Message>>(Assert.Single(builder.Filters));

        var universal = new PartitionKeyMessageSendTopologyConvention<Message>(new UniversalPartitionFormatter());
        Assert.True(universal.TryGetMessageSendTopology(out IMessageSendTopology<Message>? universalTopology));
        Assert.NotNull(universalTopology);
        Assert.True(universal.TryGetMessageSendTopologyConvention(out IMessageSendTopologyConvention<Message>? compatible));
        Assert.Same(universal, compatible);
        Assert.False(universal.TryGetMessageSendTopologyConvention(out IMessageSendTopologyConvention<PlainMessage>? incompatible));
        Assert.Null(incompatible);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-TOPOLOGY-CONVENTION", "routing-convention-lifecycle")]
    public void RoutingConvention_ProjectsCachesAndCreatesTopologyOnlyAfterConfiguration()
    {
        var root = new RoutingKeySendTopologyConvention();
        var rootConvention = (IMessageSendTopologyConvention)root;
        Assert.True(rootConvention.TryGetMessageSendTopologyConvention(
            out IMessageSendTopologyConvention<Message>? first));
        Assert.True(rootConvention.TryGetMessageSendTopologyConvention(
            out IMessageSendTopologyConvention<Message>? second));
        Assert.Same(first, second);
        var configurable = Assert.IsAssignableFrom<IRoutingKeyMessageSendTopologyConvention<Message>>(first);

        Assert.False(first.TryGetMessageSendTopology(out IMessageSendTopology<Message>? absent));
        Assert.Null(absent);
        configurable.SetFormatter(new MessageRoutingFormatter());
        Assert.True(first.TryGetMessageSendTopology(out IMessageSendTopology<Message>? topology));
        var builder = new CapturingBuilder<SendContext<Message>>();
        topology.Apply(builder);
        Assert.IsType<SetRoutingKeyFilter<Message>>(Assert.Single(builder.Filters));

        var universal = new RoutingKeyMessageSendTopologyConvention<Message>(new UniversalRoutingFormatter());
        Assert.True(((IMessageSendTopologyConvention<Message>)universal)
            .TryGetMessageSendTopology(out IMessageSendTopology<Message>? universalTopology));
        Assert.NotNull(universalTopology);
        Assert.True(((IMessageSendTopologyConvention)universal).TryGetMessageSendTopologyConvention(
            out IMessageSendTopologyConvention<Message>? compatible));
        Assert.Same(universal, compatible);
        Assert.False(((IMessageSendTopologyConvention)universal).TryGetMessageSendTopologyConvention(
            out IMessageSendTopologyConvention<PlainMessage>? incompatible));
        Assert.Null(incompatible);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-TOPOLOGY-CONVENTION", "serializer-convention-lifecycle")]
    public void SerializerConvention_ProjectsAndCreatesTopologyOnlyAfterConfiguration()
    {
        var convention = new SetSerializerMessageSendTopologyConvention<Message>();
        var typed = (IMessageSendTopologyConvention<Message>)convention;
        var untyped = (IMessageSendTopologyConvention)convention;
        Assert.False(typed.TryGetMessageSendTopology(out IMessageSendTopology<Message>? absent));
        Assert.Null(absent);

        var contentType = new ContentType("application/vnd.vicione.message+json");
        convention.SetSerializer(contentType);
        Assert.True(typed.TryGetMessageSendTopology(out IMessageSendTopology<Message>? topology));
        var builder = new CapturingBuilder<SendContext<Message>>();
        topology.Apply(builder);
        Assert.IsType<SetSerializerFilter<Message>>(Assert.Single(builder.Filters));

        Assert.True(untyped.TryGetMessageSendTopologyConvention(
            out IMessageSendTopologyConvention<Message>? compatible));
        Assert.Same(convention, compatible);
        Assert.False(untyped.TryGetMessageSendTopologyConvention(
            out IMessageSendTopologyConvention<PlainMessage>? incompatible));
        Assert.Null(incompatible);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-TOPOLOGY-CONVENTION", "set-topology-required-builders-and-values")]
    public void SetTopologies_RejectMissingValuesAndBuildersAtTheirOwningBoundary()
    {
        var correlation = new DelegateMessageCorrelationId<Message>(_ => Guid.Empty);
        var partition = new MessagePartitionFormatter();
        var routing = new MessageRoutingFormatter();
        var contentType = new ContentType("application/json");

        Assert.Equal("messageCorrelationId", Assert.Throws<ArgumentNullException>(
            () => new SetCorrelationIdMessageSendTopology<Message>(null!)).ParamName);
        Assert.Equal("partitionKeyFormatter", Assert.Throws<ArgumentNullException>(
            () => new SetPartitionKeyMessageSendTopology<Message>(null!)).ParamName);
        Assert.Equal("routingKeyFormatter", Assert.Throws<ArgumentNullException>(
            () => new SetRoutingKeyMessageSendTopology<Message>(null!)).ParamName);
        Assert.Equal("contentType", Assert.Throws<ArgumentNullException>(
            () => new SetSerializerMessageSendTopology<Message>(null!)).ParamName);

        Assert.Equal("builder", Assert.Throws<ArgumentNullException>(
            () => new SetCorrelationIdMessageSendTopology<Message>(correlation).Apply(null!)).ParamName);
        Assert.Equal("builder", Assert.Throws<ArgumentNullException>(
            () => new SetPartitionKeyMessageSendTopology<Message>(partition).Apply(null!)).ParamName);
        Assert.Equal("builder", Assert.Throws<ArgumentNullException>(
            () => new SetRoutingKeyMessageSendTopology<Message>(routing).Apply(null!)).ParamName);
        Assert.Equal("builder", Assert.Throws<ArgumentNullException>(
            () => new SetSerializerMessageSendTopology<Message>(contentType).Apply(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-PUBLIC-API", "closed-root-send-conventions")]
    public void ConcreteRootSendConventions_AreClosedForInheritance()
    {
        Assert.True(typeof(PartitionKeySendTopologyConvention).IsSealed);
        Assert.True(typeof(RoutingKeySendTopologyConvention).IsSealed);
    }

    private sealed record Message;

    private sealed record PlainMessage;

    private sealed record CorrelatedMessage(Guid CorrelationId) : IMessageCorrelation<Guid>;

    private sealed class MessagePartitionFormatter : IMessagePartitionKeyFormatter<Message>
    {
        public string FormatPartitionKey(SendContext<Message> context) => "partition";
    }

    private sealed class UniversalPartitionFormatter : IPartitionKeyFormatter
    {
        public string FormatPartitionKey<T>(SendContext<T> context)
            where T : class => "partition";
    }

    private sealed class MessageRoutingFormatter : IMessageRoutingKeyFormatter<Message>
    {
        public string FormatRoutingKey(SendContext<Message> context) => "routing";
    }

    private sealed class UniversalRoutingFormatter : IRoutingKeyFormatter
    {
        public string FormatRoutingKey<T>(SendContext<T> context)
            where T : class => "routing";
    }

    private sealed class CapturingBuilder<TContext> : ITopologyPipeBuilder<TContext>
        where TContext : class, PipeContext
    {
        readonly List<IFilter<TContext>> _filters = new();

        internal IReadOnlyList<IFilter<TContext>> Filters => _filters;

        public bool IsDelegated => false;

        public bool IsImplemented => false;

        public void AddFilter(IFilter<TContext> filter) => _filters.Add(filter);

        public ITopologyPipeBuilder<TContext> CreateDelegatedBuilder() => new CapturingBuilder<TContext>();
    }
}
