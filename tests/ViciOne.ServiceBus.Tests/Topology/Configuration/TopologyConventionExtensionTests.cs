using System.Net.Mime;
using ViciOne.ServiceBus.Advanced.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Topology;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Topology.Configuration;

public sealed class TopologyConventionExtensionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-CONVENTION-EXTENSIONS", "every-required-receiver-and-value")]
    public void ConventionExtensions_RejectEveryMissingRequiredInputAtThePublicBoundary()
    {
        var messageTopology = new MessageSendTopology<Message>();
        var sendTopology = new SendTopology();
        var partitionFormatter = new PartitionFormatter<Message>();
        var routingFormatter = new RoutingFormatter<Message>();
        Func<Message, Guid> requiredCorrelation = message => message.CorrelationId;
        Func<Message, Guid?> optionalCorrelation = message => message.OptionalCorrelationId;
        Func<SendContext<Message>, string> partitionSelector = _ => "partition";
        Func<SendContext<Message>, string> routingSelector = _ => "routing";

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            CorrelationIdConventionExtensions.UseCorrelationId((IMessageSendTopologyConfigurator<Message>)null!, requiredCorrelation)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            CorrelationIdConventionExtensions.UseCorrelationId((IMessageSendTopologyConfigurator<Message>)null!, optionalCorrelation)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            CorrelationIdConventionExtensions.UseCorrelationId((ISendTopology)null!, requiredCorrelation)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            CorrelationIdConventionExtensions.UseCorrelationId((ISendTopology)null!, optionalCorrelation)).ParamName);
        Assert.Equal("correlationIdSelector", Assert.Throws<ArgumentNullException>(() =>
            messageTopology.UseCorrelationId((Func<Message, Guid>)null!)).ParamName);
        Assert.Equal("correlationIdSelector", Assert.Throws<ArgumentNullException>(() =>
            messageTopology.UseCorrelationId((Func<Message, Guid?>)null!)).ParamName);
        Assert.Equal("correlationIdSelector", Assert.Throws<ArgumentNullException>(() =>
            ((ISendTopology)sendTopology).UseCorrelationId<Message>((Func<Message, Guid>)null!)).ParamName);
        Assert.Equal("correlationIdSelector", Assert.Throws<ArgumentNullException>(() =>
            ((ISendTopology)sendTopology).UseCorrelationId<Message>((Func<Message, Guid?>)null!)).ParamName);

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            PartitionKeyConventionExtensions.UsePartitionKeyFormatter(
                (IMessageSendTopologyConfigurator<Message>)null!, partitionFormatter)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            PartitionKeyConventionExtensions.UsePartitionKeyFormatter(
                (IMessageSendTopologyConfigurator<Message>)null!, partitionSelector)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            PartitionKeyConventionExtensions.UsePartitionKeyFormatter(
                (ISendTopologyConfigurator)null!, partitionFormatter)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            PartitionKeyConventionExtensions.UsePartitionKeyFormatter(
                (ISendTopologyConfigurator)null!, partitionSelector)).ParamName);
        Assert.Equal("formatter", Assert.Throws<ArgumentNullException>(() =>
            messageTopology.UsePartitionKeyFormatter((IMessagePartitionKeyFormatter<Message>)null!)).ParamName);
        Assert.Equal("formatter", Assert.Throws<ArgumentNullException>(() =>
            messageTopology.UsePartitionKeyFormatter((Func<SendContext<Message>, string>)null!)).ParamName);
        Assert.Equal("formatter", Assert.Throws<ArgumentNullException>(() =>
            sendTopology.UsePartitionKeyFormatter<Message>((IMessagePartitionKeyFormatter<Message>)null!)).ParamName);
        Assert.Equal("formatter", Assert.Throws<ArgumentNullException>(() =>
            sendTopology.UsePartitionKeyFormatter<Message>((Func<SendContext<Message>, string>)null!)).ParamName);

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            RoutingKeyConventionExtensions.UseRoutingKeyFormatter(
                (IMessageSendTopologyConfigurator<Message>)null!, routingFormatter)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            RoutingKeyConventionExtensions.UseRoutingKeyFormatter(
                (IMessageSendTopologyConfigurator<Message>)null!, routingSelector)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            RoutingKeyConventionExtensions.UseRoutingKeyFormatter(
                (ISendTopologyConfigurator)null!, routingFormatter)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            RoutingKeyConventionExtensions.UseRoutingKeyFormatter(
                (ISendTopologyConfigurator)null!, routingSelector)).ParamName);
        Assert.Equal("formatter", Assert.Throws<ArgumentNullException>(() =>
            messageTopology.UseRoutingKeyFormatter((IMessageRoutingKeyFormatter<Message>)null!)).ParamName);
        Assert.Equal("formatter", Assert.Throws<ArgumentNullException>(() =>
            messageTopology.UseRoutingKeyFormatter((Func<SendContext<Message>, string>)null!)).ParamName);
        Assert.Equal("formatter", Assert.Throws<ArgumentNullException>(() =>
            sendTopology.UseRoutingKeyFormatter<Message>((IMessageRoutingKeyFormatter<Message>)null!)).ParamName);
        Assert.Equal("formatter", Assert.Throws<ArgumentNullException>(() =>
            sendTopology.UseRoutingKeyFormatter<Message>((Func<SendContext<Message>, string>)null!)).ParamName);

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            SerializerConventionExtensions.UseSerializer<Message>(null!, new ContentType("application/json"))).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            SerializerConventionExtensions.UseSerializer<Message>(null!, "application/json")).ParamName);
        Assert.Equal("contentType", Assert.Throws<ArgumentNullException>(() =>
            messageTopology.UseSerializer((ContentType)null!)).ParamName);
        Assert.Equal("contentType", Assert.Throws<ArgumentNullException>(() =>
            messageTopology.UseSerializer((string)null!)).ParamName);
        Assert.Equal("contentType", Assert.Throws<ArgumentException>(() =>
            messageTopology.UseSerializer("  ")).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-CONVENTION-EXTENSIONS", "correlation-overload-projection")]
    public void CorrelationExtensions_PreserveRequiredOptionalMessageAndRootSelectors()
    {
        var messageTopology = new MessageSendTopology<Message>();
        var required = new Message(Guid.Parse("117580c7-4fc4-48a6-b165-58c2c0be0318"), null);
        messageTopology.UseCorrelationId(message => message.CorrelationId);

        Assert.True(messageTopology.TryGetConvention(
            out ICorrelationIdMessageSendTopologyConvention<Message>? requiredConvention));
        Assert.True(requiredConvention.TryGetCorrelationIdResolver(out IMessageCorrelationId<Message>? requiredResolver));
        Assert.True(requiredResolver.TryGetCorrelationId(required, out Guid requiredActual));
        Assert.Equal(required.CorrelationId, requiredActual);

        messageTopology.UseCorrelationId(message => message.OptionalCorrelationId);
        Assert.True(requiredConvention.TryGetCorrelationIdResolver(out IMessageCorrelationId<Message>? optionalResolver));
        Assert.False(optionalResolver.TryGetCorrelationId(required, out Guid absent));
        Assert.Equal(Guid.Empty, absent);

        var root = new SendTopology();
        ((ISendTopology)root).UseCorrelationId<SecondMessage>(message => message.CorrelationId);
        IMessageSendTopologyConfigurator<SecondMessage> secondTopology = root.GetMessageTopology<SecondMessage>();
        Assert.True(secondTopology.TryGetConvention(
            out ICorrelationIdMessageSendTopologyConvention<SecondMessage>? rootRequiredConvention));
        Assert.True(rootRequiredConvention.TryGetCorrelationIdResolver(
            out IMessageCorrelationId<SecondMessage>? rootRequiredResolver));
        var second = new SecondMessage(Guid.Parse("5cb86e16-6efd-4c15-b88b-f156419cd8cc"), null);
        Assert.True(rootRequiredResolver.TryGetCorrelationId(second, out Guid secondActual));
        Assert.Equal(second.CorrelationId, secondActual);

        ((ISendTopology)root).UseCorrelationId<ThirdMessage>(message => message.OptionalCorrelationId);
        IMessageSendTopologyConfigurator<ThirdMessage> thirdTopology = root.GetMessageTopology<ThirdMessage>();
        Assert.True(thirdTopology.TryGetConvention(
            out ICorrelationIdMessageSendTopologyConvention<ThirdMessage>? rootOptionalConvention));
        Assert.True(rootOptionalConvention.TryGetCorrelationIdResolver(
            out IMessageCorrelationId<ThirdMessage>? rootOptionalResolver));
        Assert.False(rootOptionalResolver.TryGetCorrelationId(new ThirdMessage(null), out Guid rootAbsent));
        Assert.Equal(Guid.Empty, rootAbsent);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-CONVENTION-EXTENSIONS", "partition-and-routing-overload-projection")]
    public void PartitionAndRoutingExtensions_ConfigureMessageAndRootTopologyOverloads()
    {
        var partitionMessage = new MessageSendTopology<PartitionMessage>();
        Assert.True(partitionMessage.TryAddConvention(new PartitionKeyMessageSendTopologyConvention<PartitionMessage>()));
        partitionMessage.UsePartitionKeyFormatter(new PartitionFormatter<PartitionMessage>());
        AssertSingleFilter<SetPartitionKeyFilter<PartitionMessage>, PartitionMessage>(partitionMessage);

        var partitionRoot = new SendTopology();
        Assert.True(partitionRoot.TryAddConvention(new PartitionKeySendTopologyConvention()));
        partitionRoot.UsePartitionKeyFormatter<SecondPartitionMessage>(_ => "partition");
        AssertSingleFilter<SetPartitionKeyFilter<SecondPartitionMessage>, SecondPartitionMessage>(
            partitionRoot.GetMessageTopology<SecondPartitionMessage>());

        var routingMessage = new MessageSendTopology<RoutingMessage>();
        Assert.True(routingMessage.TryAddConvention(new RoutingKeyMessageSendTopologyConvention<RoutingMessage>()));
        routingMessage.UseRoutingKeyFormatter(new RoutingFormatter<RoutingMessage>());
        AssertSingleFilter<SetRoutingKeyFilter<RoutingMessage>, RoutingMessage>(routingMessage);

        var routingRoot = new SendTopology();
        Assert.True(routingRoot.TryAddConvention(new RoutingKeySendTopologyConvention()));
        routingRoot.UseRoutingKeyFormatter<SecondRoutingMessage>(_ => "routing");
        AssertSingleFilter<SetRoutingKeyFilter<SecondRoutingMessage>, SecondRoutingMessage>(
            routingRoot.GetMessageTopology<SecondRoutingMessage>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-CONVENTION-EXTENSIONS", "serializer-content-type-overload-projection")]
    public void SerializerExtensions_ConfigureAndUpdateBothContentTypeShapes()
    {
        var topology = new MessageSendTopology<Message>();

        topology.UseSerializer(new ContentType("application/json"));
        AssertSingleFilter<SetSerializerFilter<Message>, Message>(topology);

        topology.UseSerializer("application/vnd.vicione.message+json");
        AssertSingleFilter<SetSerializerFilter<Message>, Message>(topology);
    }

    static void AssertSingleFilter<TFilter, TMessage>(IMessageSendTopology<TMessage> topology)
        where TFilter : class, IFilter<SendContext<TMessage>>
        where TMessage : class
    {
        var builder = new CapturingBuilder<SendContext<TMessage>>();

        topology.Apply(builder);

        Assert.IsType<TFilter>(Assert.Single(builder.Filters));
    }

    private sealed record Message(Guid CorrelationId, Guid? OptionalCorrelationId);

    private sealed record SecondMessage(Guid CorrelationId, Guid? OptionalCorrelationId);

    private sealed record ThirdMessage(Guid? OptionalCorrelationId);

    private sealed record PartitionMessage;

    private sealed record SecondPartitionMessage;

    private sealed record RoutingMessage;

    private sealed record SecondRoutingMessage;

    private sealed class PartitionFormatter<TMessage> : IMessagePartitionKeyFormatter<TMessage>
        where TMessage : class
    {
        public string FormatPartitionKey(SendContext<TMessage> context) => "partition";
    }

    private sealed class RoutingFormatter<TMessage> : IMessageRoutingKeyFormatter<TMessage>
        where TMessage : class
    {
        public string FormatRoutingKey(SendContext<TMessage> context) => "routing";
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
