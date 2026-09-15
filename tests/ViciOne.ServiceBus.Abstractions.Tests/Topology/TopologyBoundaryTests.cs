using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Advanced.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Topology;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Topology;

public sealed class TopologyBoundaryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-PIPE", "consume-builder-state-forwarding")]
    public void ConsumeTopologySpecification_PreservesTheParentBuilderState()
    {
        var topology = new CapturingConsumeTopology();
        var specification = new MessageConsumeTopologyPipeSpecification<Message>(topology);
        var builder = new SpecificationBuilder<ConsumeContext<Message>>(isDelegated: true, isImplemented: true);

        specification.Apply(builder);

        Assert.NotNull(topology.Builder);
        Assert.True(topology.Builder.IsDelegated);
        Assert.True(topology.Builder.IsImplemented);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-PIPE", "required-specification-collaborators")]
    public void TopologySpecifications_RejectEveryMissingRequiredCollaborator()
    {
        Assert.Equal("messageConsumeTopology", Assert.Throws<ArgumentNullException>(
            () => new MessageConsumeTopologyPipeSpecification<Message>(null!)).ParamName);
        Assert.Equal("messageSendTopology", Assert.Throws<ArgumentNullException>(
            () => new MessageSendTopologyPipeSpecification<Message>(null!)).ParamName);
        Assert.Equal("messagePublishTopology", Assert.Throws<ArgumentNullException>(
            () => new MessagePublishTopologyPipeSpecification<Message>(null!)).ParamName);

        var consume = new MessageConsumeTopologyPipeSpecification<Message>(new CapturingConsumeTopology());
        var send = new MessageSendTopologyPipeSpecification<Message>(new NoOpSendTopology());
        var publish = new MessagePublishTopologyPipeSpecification<Message>(new NoOpPublishTopology());

        Assert.Equal("builder", Assert.Throws<ArgumentNullException>(() => consume.Apply(null!)).ParamName);
        Assert.Equal("builder", Assert.Throws<ArgumentNullException>(() => send.Apply(null!)).ParamName);
        Assert.Equal("builder", Assert.Throws<ArgumentNullException>(() => publish.Apply(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-SEND-TOPOLOGY", "required-configuration-inputs")]
    public void MessageSendTopology_RejectsEveryMissingConfigurationInput()
    {
        var topology = new MessageSendTopology<Message>();

        Assert.Equal("sendTopology", Assert.Throws<ArgumentNullException>(() => topology.Add(null!)).ParamName);
        Assert.Equal("configuration", Assert.Throws<ArgumentNullException>(() => topology.AddDelegate(null!)).ParamName);
        Assert.Equal("builder", Assert.Throws<ArgumentNullException>(() => topology.Apply(null!)).ParamName);
        Assert.Equal("convention", Assert.Throws<ArgumentNullException>(
            () => topology.TryAddConvention((IMessageSendTopologyConvention<Message>)null!)).ParamName);
        Assert.Equal("convention", Assert.Throws<ArgumentNullException>(
            () => topology.TryAddConvention((ISendTopologyConvention)null!)).ParamName);
        Assert.Equal("update", Assert.Throws<ArgumentNullException>(
            () => topology.UpdateConvention<TestSendConvention>(null!)).ParamName);
        Assert.Equal("add", Assert.Throws<ArgumentNullException>(
            () => topology.AddOrUpdateConvention<TestSendConvention>(null!, convention => convention)).ParamName);
        Assert.Equal("update", Assert.Throws<ArgumentNullException>(
            () => topology.AddOrUpdateConvention(() => new TestSendConvention(), null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-SEND-TOPOLOGY", "invalid-convention-results")]
    public void MessageSendTopology_RejectsNullConventionFactoryAndUpdateResults()
    {
        var topology = new MessageSendTopology<Message>();
        topology.TryAddConvention(new TestSendConvention());

        Assert.Throws<InvalidOperationException>(
            () => topology.UpdateConvention<TestSendConvention>(_ => null!));

        var empty = new MessageSendTopology<Message>();
        Assert.Throws<InvalidOperationException>(
            () => empty.AddOrUpdateConvention<TestSendConvention>(() => null!, convention => convention));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-PUBLISH-TOPOLOGY", "required-configuration-inputs")]
    public void MessagePublishTopology_RejectsEveryMissingConfigurationInput()
    {
        Assert.Equal("publishTopology", Assert.Throws<ArgumentNullException>(
            () => new MessagePublishTopology<Message>(null!)).ParamName);

        var topology = new MessagePublishTopology<Message>(new PublishTopology());

        Assert.Equal("publishTopology", Assert.Throws<ArgumentNullException>(() => topology.Add(null!)).ParamName);
        Assert.Equal("configuration", Assert.Throws<ArgumentNullException>(() => topology.AddDelegate(null!)).ParamName);
        Assert.Equal("builder", Assert.Throws<ArgumentNullException>(() => topology.Apply(null!)).ParamName);
        Assert.Equal("baseAddress", Assert.Throws<ArgumentNullException>(
            () => topology.TryGetPublishAddress(null!, out _)).ParamName);
        Assert.Equal("convention", Assert.Throws<ArgumentNullException>(
            () => topology.TryAddConvention((IMessagePublishTopologyConvention<Message>)null!)).ParamName);
        Assert.Equal("convention", Assert.Throws<ArgumentNullException>(
            () => topology.TryAddConvention((IPublishTopologyConvention)null!)).ParamName);
        Assert.Equal("add", Assert.Throws<ArgumentNullException>(
            () => topology.AddOrUpdateConvention<TestPublishConvention>(null!, convention => convention)).ParamName);
        Assert.Equal("update", Assert.Throws<ArgumentNullException>(
            () => topology.AddOrUpdateConvention(() => new TestPublishConvention(), null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-PUBLISH-TOPOLOGY", "invalid-convention-results")]
    public void MessagePublishTopology_RejectsNullConventionFactoryAndUpdateResults()
    {
        var topology = new MessagePublishTopology<Message>(new PublishTopology());
        topology.TryAddConvention(new TestPublishConvention());

        Assert.Throws<InvalidOperationException>(
            () => topology.AddOrUpdateConvention(() => new TestPublishConvention(), _ => null!));

        var empty = new MessagePublishTopology<Message>(new PublishTopology());
        Assert.Throws<InvalidOperationException>(
            () => empty.AddOrUpdateConvention<TestPublishConvention>(() => null!, convention => convention));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-ROOTS", "required-runtime-collaborators")]
    public void RootTopologies_RejectEveryMissingRuntimeCollaborator()
    {
        Assert.Equal("entityNameFormatter", Assert.Throws<ArgumentNullException>(
            () => new MessageTopology(null!)).ParamName);

        var message = new MessageTopology(new MessageUrnEntityNameFormatter());
        var send = new SendTopology();
        var publish = new PublishTopology();

        Assert.Equal("observer", Assert.Throws<ArgumentNullException>(
            () => message.ConnectMessageTopologyConfigurationObserver(null!)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentNullException>(
            () => send.DeadLetterQueueNameFormatter = null!).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentNullException>(
            () => send.ErrorQueueNameFormatter = null!).ParamName);
        Assert.Equal("observer", Assert.Throws<ArgumentNullException>(
            () => send.ConnectSendTopologyConfigurationObserver(null!)).ParamName);
        Assert.Equal("convention", Assert.Throws<ArgumentNullException>(
            () => send.TryAddConvention(null!)).ParamName);
        Assert.Equal("observer", Assert.Throws<ArgumentNullException>(
            () => publish.ConnectPublishTopologyConfigurationObserver(null!)).ParamName);
        Assert.Equal("convention", Assert.Throws<ArgumentNullException>(
            () => publish.TryAddConvention(null!)).ParamName);
        Assert.Equal("messageType", Assert.Throws<ArgumentNullException>(
            () => publish.GetMessageTopology(null!)).ParamName);
        Assert.Equal("messageType", Assert.Throws<ArgumentNullException>(
            () => publish.TryGetPublishAddress(null!, new Uri("loopback://localhost"), out _)).ParamName);
        Assert.Equal("baseAddress", Assert.Throws<ArgumentNullException>(
            () => publish.TryGetPublishAddress(typeof(Message), null!, out _)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-OBSERVATION", "direct-owner-notification")]
    public void RootTopologies_NotifyObserversWithTheExactCreatedConfigurator()
    {
        string[] removedAdapterTypes =
        [
            "ViciOne.ServiceBus.Configuration.ConsumeTopologyConfigurationObservable",
            "ViciOne.ServiceBus.Configuration.MessageTopologyConfigurationObservable",
            "ViciOne.ServiceBus.Configuration.PublishTopologyConfigurationObservable",
            "ViciOne.ServiceBus.Configuration.SendTopologyConfigurationObservable"
        ];
        Assert.All(removedAdapterTypes, typeName => Assert.Null(typeof(IMessageTopology).Assembly.GetType(typeName)));

        var message = new MessageTopology(new MessageUrnEntityNameFormatter());
        var send = new SendTopology();
        var publish = new PublishTopology();
        var messageObserver = new CapturingMessageTopologyObserver();
        var sendObserver = new CapturingSendTopologyObserver();
        var publishObserver = new CapturingPublishTopologyObserver();
        message.ConnectMessageTopologyConfigurationObserver(messageObserver);
        send.ConnectSendTopologyConfigurationObserver(sendObserver);
        publish.ConnectPublishTopologyConfigurationObserver(publishObserver);

        IMessageTopologyConfigurator<Message> messageConfiguration =
            ((IMessageTopologyConfigurator)message).GetMessageTopology<Message>();
        IMessageSendTopologyConfigurator<Message> sendConfiguration = send.GetMessageTopology<Message>();
        IMessagePublishTopologyConfigurator<Message> publishConfiguration =
            ((IPublishTopologyConfigurator)publish).GetMessageTopology<Message>();

        Assert.Same(messageConfiguration, messageObserver.Configuration);
        Assert.Same(sendConfiguration, sendObserver.Configuration);
        Assert.Same(publishConfiguration, publishObserver.Configuration);
        Assert.Equal(typeof(Message), messageObserver.MessageType);
        Assert.Equal(typeof(Message), sendObserver.MessageType);
        Assert.Equal(typeof(Message), publishObserver.MessageType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-ENTITY-NAME", "required-formatting-inputs")]
    public void EntityNameFormatters_RejectMissingRequiredInputs()
    {
        Assert.Equal("entityNameFormatter", Assert.Throws<ArgumentNullException>(
            () => new PrefixEntityNameFormatter(null!, "prefix-")).ParamName);
        Assert.Equal("prefix", Assert.Throws<ArgumentNullException>(
            () => new PrefixEntityNameFormatter(new ConstantEntityNameFormatter("message"), null!)).ParamName);
        Assert.Equal("formatter", Assert.Throws<ArgumentNullException>(
            () => new MessageNameFormatterEntityNameFormatter(null!)).ParamName);
        Assert.Equal("entityNameFormatter", Assert.Throws<ArgumentNullException>(
            () => new MessageEntityNameFormatter<Message>(null!)).ParamName);
        Assert.Equal("entityNameFormatter", Assert.Throws<ArgumentNullException>(
            () => new MessageTopology<Message>(null!)).ParamName);
        Assert.Equal("entityName", Assert.ThrowsAny<ArgumentException>(
            () => new StaticEntityNameFormatter<Message>(" ")).ParamName);
        Assert.Equal("queueName", Assert.ThrowsAny<ArgumentException>(
            () => DefaultErrorQueueNameFormatter.Instance.FormatErrorQueueName(" ")).ParamName);
        Assert.Equal("queueName", Assert.ThrowsAny<ArgumentException>(
            () => DefaultDeadLetterQueueNameFormatter.Instance.FormatDeadLetterQueueName(" ")).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAILURE-QUEUE-NAMES", "closed-default-singletons-and-exact-suffixes")]
    public void DefaultFailureQueueNameFormatters_AreClosedSingletonsWithExactSuffixes()
    {
        Assert.Same(DefaultErrorQueueNameFormatter.Instance, DefaultErrorQueueNameFormatter.Instance);
        Assert.Same(DefaultDeadLetterQueueNameFormatter.Instance, DefaultDeadLetterQueueNameFormatter.Instance);
        Assert.True(typeof(DefaultErrorQueueNameFormatter).IsSealed);
        Assert.True(typeof(DefaultDeadLetterQueueNameFormatter).IsSealed);
        Assert.Empty(typeof(DefaultErrorQueueNameFormatter).GetConstructors());
        Assert.Empty(typeof(DefaultDeadLetterQueueNameFormatter).GetConstructors());

        Assert.Equal("orders_error", DefaultErrorQueueNameFormatter.Instance.FormatErrorQueueName("orders"));
        Assert.Equal("orders_skipped", DefaultDeadLetterQueueNameFormatter.Instance.FormatDeadLetterQueueName("orders"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-ENTITY-NAME", "concurrent-single-evaluation")]
    public async Task MessageEntityNameFormatter_EvaluatesItsSourceExactlyOnceAsync()
    {
        var source = new CoordinatedEntityNameFormatter();
        var formatter = new MessageEntityNameFormatter<Message>(source);

        Task<string> first = Task.Run(formatter.FormatEntityName);
        Assert.True(source.FirstInvocation.Wait(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
        Task<string> second = Task.Run(formatter.FormatEntityName);
        source.SecondInvocation.Wait(TimeSpan.FromMilliseconds(250), TestContext.Current.CancellationToken);
        source.Release.Set();

        string[] values = await Task.WhenAll(first, second);

        Assert.Equal(["message", "message"], values);
        Assert.Equal(1, source.InvocationCount);
    }

    private sealed record Message;

    private sealed class CapturingMessageTopologyObserver : IMessageTopologyConfigurationObserver
    {
        internal object? Configuration { get; private set; }

        internal Type? MessageType { get; private set; }

        public void MessageTopologyCreated<T>(IMessageTopologyConfigurator<T> configuration)
            where T : class
        {
            Configuration = configuration;
            MessageType = typeof(T);
        }
    }

    private sealed class CapturingPublishTopologyObserver : IPublishTopologyConfigurationObserver
    {
        internal object? Configuration { get; private set; }

        internal Type? MessageType { get; private set; }

        public void MessageTopologyCreated<T>(IMessagePublishTopologyConfigurator<T> configurator)
            where T : class
        {
            Configuration = configurator;
            MessageType = typeof(T);
        }
    }

    private sealed class CapturingSendTopologyObserver : ISendTopologyConfigurationObserver
    {
        internal object? Configuration { get; private set; }

        internal Type? MessageType { get; private set; }

        public void MessageTopologyCreated<T>(IMessageSendTopologyConfigurator<T> configuration)
            where T : class
        {
            Configuration = configuration;
            MessageType = typeof(T);
        }
    }

    private sealed class CapturingConsumeTopology : IMessageConsumeTopology<Message>
    {
        internal ITopologyPipeBuilder<ConsumeContext<Message>>? Builder { get; private set; }

        public void Apply(ITopologyPipeBuilder<ConsumeContext<Message>> builder) => Builder = builder;
    }

    private sealed class NoOpSendTopology : IMessageSendTopology<Message>
    {
        public void Apply(ITopologyPipeBuilder<SendContext<Message>> builder)
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

    private sealed class TestSendConvention : IMessageSendTopologyConvention<Message>
    {
        public bool TryGetMessageSendTopology([NotNullWhen(true)] out IMessageSendTopology<Message>? messageSendTopology)
        {
            messageSendTopology = null;
            return false;
        }

        public bool TryGetMessageSendTopologyConvention<T>([NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
            where T : class
        {
            convention = null;
            return false;
        }
    }

    private sealed class TestPublishConvention : IMessagePublishTopologyConvention<Message>
    {
        public bool TryGetMessagePublishTopology([NotNullWhen(true)] out IMessagePublishTopology<Message>? messagePublishTopology)
        {
            messagePublishTopology = null!;
            return false;
        }

        public bool TryGetMessagePublishTopologyConvention<T>([NotNullWhen(true)] out IMessagePublishTopologyConvention<T>? convention)
            where T : class
        {
            convention = null!;
            return false;
        }
    }

    private sealed class ConstantEntityNameFormatter(string value) : IEntityNameFormatter
    {
        public string FormatEntityName<T>() => value;
    }

    private sealed class CoordinatedEntityNameFormatter : IEntityNameFormatter
    {
        int _invocationCount;

        internal ManualResetEventSlim FirstInvocation { get; } = new();

        internal int InvocationCount => Volatile.Read(ref _invocationCount);

        internal ManualResetEventSlim Release { get; } = new();

        internal ManualResetEventSlim SecondInvocation { get; } = new();

        public string FormatEntityName<T>()
        {
            int invocation = Interlocked.Increment(ref _invocationCount);
            if (invocation == 1)
                FirstInvocation.Set();
            else
                SecondInvocation.Set();

            Assert.True(Release.Wait(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
            return "message";
        }
    }

    private sealed class SpecificationBuilder<TContext>(bool isDelegated, bool isImplemented) : ISpecificationPipeBuilder<TContext>
        where TContext : class, PipeContext
    {
        public bool IsDelegated { get; } = isDelegated;

        public bool IsImplemented { get; } = isImplemented;

        public void AddFilter(IFilter<TContext> filter)
        {
        }

        public ISpecificationPipeBuilder<TContext> CreateDelegatedBuilder() =>
            new SpecificationBuilder<TContext>(true, IsImplemented);

        public ISpecificationPipeBuilder<TContext> CreateImplementedBuilder() =>
            new SpecificationBuilder<TContext>(IsDelegated, true);
    }
}
