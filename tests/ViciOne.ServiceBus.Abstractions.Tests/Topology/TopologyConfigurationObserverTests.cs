using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ViciOne.ServiceBus.Advanced.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Topology;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Topology;

public sealed class TopologyConfigurationObserverTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-OBSERVATION", "delegate-publish-and-send-topology")]
    public void DelegateObservers_ApplyTheirSourceTopologyAsDelegatedConfiguration()
    {
        var publishTrace = new List<bool>();
        var publishSource = new PublishTopology();
        ((IPublishTopologyConfigurator)publishSource).AddMessagePublishTopology<Message>(
            new RecordingPublishTopology(publishTrace));
        var publishTarget = new MessagePublishTopology<Message>(new PublishTopology());
        new DelegatePublishTopologyConfigurationObserver(publishSource).MessageTopologyCreated(publishTarget);
        publishTarget.Apply(new RecordingBuilder<PublishContext<Message>>());

        var sendTrace = new List<bool>();
        var sendSource = new SendTopology();
        ((ISendTopologyConfigurator)sendSource).AddMessageSendTopology<Message>(new RecordingSendTopology(sendTrace));
        var sendTarget = new MessageSendTopology<Message>();
        new DelegateSendTopologyConfigurationObserver(sendSource).MessageTopologyCreated(sendTarget);
        sendTarget.Apply(new RecordingBuilder<SendContext<Message>>());

        Assert.Equal([true], publishTrace);
        Assert.Equal([true], sendTrace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-OBSERVATION", "delegate-observer-required-collaborators")]
    public void DelegateObservers_RejectEveryMissingCollaborator()
    {
        Assert.Equal("publishTopology", Assert.Throws<ArgumentNullException>(
            () => new DelegatePublishTopologyConfigurationObserver(null!)).ParamName);
        Assert.Equal("sendTopology", Assert.Throws<ArgumentNullException>(
            () => new DelegateSendTopologyConfigurationObserver(null!)).ParamName);

        var publish = new DelegatePublishTopologyConfigurationObserver(new PublishTopology());
        var send = new DelegateSendTopologyConfigurationObserver(new SendTopology());
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(
            () => publish.MessageTopologyCreated<Message>(null!)).ParamName);
        Assert.Equal("configuration", Assert.Throws<ArgumentNullException>(
            () => send.MessageTopologyCreated<Message>(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-OBSERVATION", "topology-parent-pipe-specifications")]
    public void PipeObservers_AddTheMatchingTopologyAsAParentSpecification()
    {
        IConsumeTopology consumeTopology = CreateProxy<IConsumeTopology>(new NoOpConsumeTopology());
        IPublishTopology publishTopology = CreateProxy<IPublishTopology>(new NoOpPublishTopology());
        ISendTopology sendTopology = CreateProxy<ISendTopology>(new MessageSendTopology<Message>());
        IMessageConsumePipeSpecification<Message> consumeSpecification =
            CreateProxy<IMessageConsumePipeSpecification<Message>>(null);
        IMessagePublishPipeSpecification<Message> publishSpecification =
            CreateProxy<IMessagePublishPipeSpecification<Message>>(null);
        IMessageSendPipeSpecification<Message> sendSpecification =
            CreateProxy<IMessageSendPipeSpecification<Message>>(null);

        ((IConsumePipeSpecificationObserver)new TopologyConsumePipeSpecificationObserver(consumeTopology))
            .MessageSpecificationCreated(consumeSpecification);
        ((IPublishPipeSpecificationObserver)new TopologyPublishPipeSpecificationObserver(publishTopology))
            .MessageSpecificationCreated(publishSpecification);
        ((ISendPipeSpecificationObserver)new TopologySendPipeSpecificationObserver(sendTopology))
            .MessageSpecificationCreated(sendSpecification);

        Assert.IsType<MessageConsumeTopologyPipeSpecification<Message>>(Handler(consumeSpecification).LastArgument);
        Assert.IsType<MessagePublishTopologyPipeSpecification<Message>>(Handler(publishSpecification).LastArgument);
        Assert.IsType<MessageSendTopologyPipeSpecification<Message>>(Handler(sendSpecification).LastArgument);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-OBSERVATION", "pipe-observer-required-collaborators")]
    public void PipeObservers_RejectEveryMissingCollaborator()
    {
        Assert.Equal("topology", Assert.Throws<ArgumentNullException>(
            () => new TopologyConsumePipeSpecificationObserver(null!)).ParamName);
        Assert.Equal("topology", Assert.Throws<ArgumentNullException>(
            () => new TopologyPublishPipeSpecificationObserver(null!)).ParamName);
        Assert.Equal("topology", Assert.Throws<ArgumentNullException>(
            () => new TopologySendPipeSpecificationObserver(null!)).ParamName);

        var consume = new TopologyConsumePipeSpecificationObserver(CreateProxy<IConsumeTopology>(new NoOpConsumeTopology()));
        var publish = new TopologyPublishPipeSpecificationObserver(CreateProxy<IPublishTopology>(new NoOpPublishTopology()));
        var send = new TopologySendPipeSpecificationObserver(CreateProxy<ISendTopology>(new MessageSendTopology<Message>()));

        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() =>
            ((IConsumePipeSpecificationObserver)consume).MessageSpecificationCreated<Message>(null!)).ParamName);
        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() =>
            ((IPublishPipeSpecificationObserver)publish).MessageSpecificationCreated<Message>(null!)).ParamName);
        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() =>
            ((ISendPipeSpecificationObserver)send).MessageSpecificationCreated<Message>(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-OBSERVATION", "closed-concrete-observers")]
    public void ConcreteTopologyObservers_AreClosedForInheritance()
    {
        Type[] observerTypes =
        [
            typeof(DelegatePublishTopologyConfigurationObserver),
            typeof(DelegateSendTopologyConfigurationObserver),
            typeof(TopologyConsumePipeSpecificationObserver),
            typeof(TopologyPublishPipeSpecificationObserver),
            typeof(TopologySendPipeSpecificationObserver)
        ];

        Assert.All(observerTypes, type => Assert.True(type.IsSealed, type.FullName));
    }

    private static T CreateProxy<T>(object? returnValue)
        where T : class
    {
        T proxy = DispatchProxy.Create<T, RecordingProxy>();
        Handler(proxy).ReturnValue = returnValue;
        return proxy;
    }

    private static RecordingProxy Handler(object proxy) => (RecordingProxy)proxy;

    private sealed record Message;

    private sealed class RecordingPublishTopology(List<bool> delegated) : IMessagePublishTopology<Message>
    {
        public bool Exclude => false;

        public void Apply(ITopologyPipeBuilder<PublishContext<Message>> builder) => delegated.Add(builder.IsDelegated);

        public bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress)
        {
            publishAddress = null;
            return false;
        }
    }

    private sealed class RecordingSendTopology(List<bool> delegated) : IMessageSendTopology<Message>
    {
        public void Apply(ITopologyPipeBuilder<SendContext<Message>> builder) => delegated.Add(builder.IsDelegated);
    }

    private sealed class NoOpConsumeTopology : IMessageConsumeTopology<Message>
    {
        public void Apply(ITopologyPipeBuilder<ConsumeContext<Message>> builder)
        {
        }
    }

    private sealed class NoOpPublishTopology : IMessagePublishTopology<Message>
    {
        public bool Exclude => false;

        public void Apply(ITopologyPipeBuilder<PublishContext<Message>> builder)
        {
        }

        public bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress)
        {
            publishAddress = null;
            return false;
        }
    }

    private sealed class RecordingBuilder<TContext>(bool isDelegated = false) : ITopologyPipeBuilder<TContext>
        where TContext : class, PipeContext
    {
        public bool IsDelegated { get; } = isDelegated;

        public bool IsImplemented => false;

        public void AddFilter(IFilter<TContext> filter)
        {
        }

        public ITopologyPipeBuilder<TContext> CreateDelegatedBuilder() => new RecordingBuilder<TContext>(true);
    }

    private class RecordingProxy : DispatchProxy
    {
        internal object? LastArgument { get; private set; }

        internal object? ReturnValue { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (args is { Length: > 0 })
                LastArgument = args[0];

            return ReturnValue;
        }
    }
}
