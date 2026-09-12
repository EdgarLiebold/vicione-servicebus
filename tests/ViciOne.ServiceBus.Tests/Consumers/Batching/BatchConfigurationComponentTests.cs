using System.Reflection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Consumers;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers.Batching;

public sealed class BatchConfigurationComponentTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CONNECTOR-FACTORY", "typed-connector-and-instance-rejection")]
    public void ConnectorFactory_ReturnsOnlyItsExactBatchConnectorAndRejectsInstances()
    {
        var factory = new BatchMessageConnectorFactory<ConfigurationBatchConsumer, ConfigurationMessage>();

        IConsumerMessageConnector<ConfigurationBatchConsumer> connector =
            factory.CreateConsumerConnector<ConfigurationBatchConsumer>();

        Assert.IsType<BatchConsumerMessageConnector<ConfigurationBatchConsumer, ConfigurationMessage>>(connector);
        Assert.Equal(typeof(ConfigurationMessage), connector.MessageType);
        Assert.Throws<ArgumentException>(() => factory.CreateConsumerConnector<OtherConsumer>());
        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            factory.CreateInstanceConnector<ConfigurationBatchConsumer>());
        Assert.Contains(nameof(ConfigurationMessage), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CONNECTOR", "metadata-specification-and-input-boundaries")]
    public void MessageConnector_ExposesItsContractCreatesSpecificationsAndRejectsMissingInputs()
    {
        var connector = new BatchConsumerMessageConnector<ConfigurationBatchConsumer, ConfigurationMessage>();
        IConsumePipeConnector consumePipe = CreateProxy<IConsumePipeConnector>(out _);
        var consumerFactory = new InstanceConsumerFactory<ConfigurationBatchConsumer>(new ConfigurationBatchConsumer());
        var specification = new ConsumerSpecification<ConfigurationBatchConsumer>(
            [new BatchConsumerMessageSpecification<ConfigurationBatchConsumer, ConfigurationMessage>()]);

        Assert.Equal(typeof(ConfigurationMessage), connector.MessageType);
        Assert.IsType<BatchConsumerMessageSpecification<ConfigurationBatchConsumer, ConfigurationMessage>>(
            connector.CreateConsumerMessageSpecification());
        Assert.Equal(
            "consumePipe",
            Assert.Throws<ArgumentNullException>(() =>
                connector.ConnectConsumer(null!, consumerFactory, specification)).ParamName);
        Assert.Equal(
            "consumerFactory",
            Assert.Throws<ArgumentNullException>(() =>
                connector.ConnectConsumer(consumePipe, null!, specification)).ParamName);
        Assert.Equal(
            "specification",
            Assert.Throws<ArgumentNullException>(() =>
                connector.ConnectConsumer(consumePipe, consumerFactory, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-MESSAGE-SPECIFICATION", "matching-and-required-boundary-matrix")]
    public void MessageSpecification_ResolvesBothOwnedContractsAndRejectsEveryMissingInput()
    {
        var specification = new BatchConsumerMessageSpecification<ConfigurationBatchConsumer, ConfigurationMessage>();

        Assert.Equal(typeof(ConfigurationMessage), specification.MessageType);
        Assert.True(specification.TryGetMessageSpecification<ConfigurationBatchConsumer, IMessageBatch<ConfigurationMessage>>(
            out IConsumerMessageSpecification<ConfigurationBatchConsumer, IMessageBatch<ConfigurationMessage>>? batchSpecification));
        Assert.Same(specification, batchSpecification);
        Assert.True(specification.TryGetMessageSpecification<ConfigurationBatchConsumer, ConfigurationMessage>(
            out IConsumerMessageSpecification<ConfigurationBatchConsumer, ConfigurationMessage>? messageSpecification));
        Assert.NotNull(messageSpecification);
        Assert.False(specification.TryGetMessageSpecification<ConfigurationBatchConsumer, OtherMessage>(out _));

        Assert.Equal(
            "specification",
            Assert.Throws<ArgumentNullException>(() => specification.AddPipeSpecification(
                (IPipeSpecification<ConsumerConsumeContext<ConfigurationBatchConsumer, ConfigurationMessage>>)null!)).ParamName);
        Assert.Equal(
            "specification",
            Assert.Throws<ArgumentNullException>(() => specification.AddPipeSpecification(
                (IPipeSpecification<ConsumerConsumeContext<ConfigurationBatchConsumer, IMessageBatch<ConfigurationMessage>>>)null!)).ParamName);
        Assert.Equal(
            "specification",
            Assert.Throws<ArgumentNullException>(() => specification.AddPipeSpecification(
                (IPipeSpecification<ConsumeContext<IMessageBatch<ConfigurationMessage>>>)null!)).ParamName);
        Assert.Equal(
            "specification",
            Assert.Throws<ArgumentNullException>(() => specification.AddPipeSpecification(
                (IPipeSpecification<ConsumerConsumeContext<ConfigurationBatchConsumer>>)null!)).ParamName);
        Assert.Equal(
            "consumeFilter",
            Assert.Throws<ArgumentNullException>(() => specification.Build(null!)).ParamName);
        Assert.Equal(
            "configure",
            Assert.Throws<ArgumentNullException>(() => specification.BuildMessagePipe(null!)).ParamName);
        Assert.Equal(
            "observer",
            Assert.Throws<ArgumentNullException>(() => specification.ConnectConsumerConfigurationObserver(null!)).ParamName);
        Assert.Equal(
            "configure",
            Assert.Throws<ArgumentNullException>(() => specification.Message(
                (Action<IConsumerMessageConfigurator<ConfigurationMessage>>)null!)).ParamName);
        Assert.Equal(
            "configure",
            Assert.Throws<ArgumentNullException>(() => specification.Message(
                (Action<IConsumerMessageConfigurator<IMessageBatch<ConfigurationMessage>>>)null!)).ParamName);

        IConsumerMessageConfigurator<ConfigurationMessage>? observedMessageConfigurator = null;
        specification.Message((IConsumerMessageConfigurator<ConfigurationMessage> configurator) =>
            observedMessageConfigurator = configurator);
        Assert.NotNull(observedMessageConfigurator);

        IConsumerMessageConfigurator<IMessageBatch<ConfigurationMessage>>? observedBatchConfigurator = null;
        specification.Message((IConsumerMessageConfigurator<IMessageBatch<ConfigurationMessage>> configurator) =>
            observedBatchConfigurator = configurator);
        Assert.NotNull(observedBatchConfigurator);
        Assert.Equal(
            "specification",
            Assert.Throws<ArgumentNullException>(() => observedBatchConfigurator.AddPipeSpecification(null!)).ParamName);

        specification.AddPipeSpecification(
            new EmptyPipeSpecification<ConsumerConsumeContext<ConfigurationBatchConsumer, ConfigurationMessage>>());
        specification.AddPipeSpecification(
            new EmptyPipeSpecification<ConsumerConsumeContext<ConfigurationBatchConsumer, IMessageBatch<ConfigurationMessage>>>());
        specification.AddPipeSpecification(
            new EmptyPipeSpecification<ConsumerConsumeContext<ConfigurationBatchConsumer>>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CONSUMER-CONVENTION", "direct-interface-class-and-non-batch-discovery")]
    public void Convention_DiscoversEveryBatchShapeWithoutInventingNonBatchContracts()
    {
        IMessageInterfaceType direct = Assert.Single(
            new BatchConsumerMessageConvention<IConsumer<IMessageBatch<ConfigurationMessage>>>().GetMessageTypes());
        IMessageInterfaceType implemented = Assert.Single(
            new BatchConsumerMessageConvention<ConfigurationBatchConsumer>().GetMessageTypes());

        Assert.Equal(typeof(IMessageBatch<ConfigurationMessage>), direct.MessageType);
        Assert.Equal(typeof(IMessageBatch<ConfigurationMessage>), implemented.MessageType);
        Assert.IsType<BatchConsumerMessageConnector<ConfigurationBatchConsumer, ConfigurationMessage>>(
            implemented.GetConsumerConnector<ConfigurationBatchConsumer>());
        Assert.Throws<NotSupportedException>(() =>
            implemented.GetInstanceConnector<ConfigurationBatchConsumer>());
        Assert.Empty(new BatchConsumerMessageConvention<OtherConsumer>().GetMessageTypes());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-REGISTRATION", "constructor-boundary-and-successful-extension-forwarding")]
    public void RegistrationExtensions_ForwardExactConfigurationFactoriesAndCallbacks()
    {
        IReceiveEndpointConfigurator endpoint = CreateProxy<IReceiveEndpointConfigurator>(out RecordingProxy endpointProxy);
        Assert.Equal(
            "configurator",
            Assert.Throws<ArgumentNullException>(() => new BatchConfigurator<ConfigurationMessage>(null!)).ParamName);
        IBatchConfigurator<ConfigurationMessage>? observedBatch = null;

        endpoint.Batch<ConfigurationMessage>(configurator =>
        {
            observedBatch = configurator;
            configurator.MessageLimit = 7;
            configurator.ConcurrencyLimit = 3;
            configurator.TimeLimit = TimeSpan.FromMinutes(2);
            configurator.TimeLimitStart = BatchTimeLimitStart.FromLast;
        });

        Assert.NotNull(observedBatch);
        Assert.Empty(endpointProxy.Invocations);

        IBatchConfigurator<ConfigurationMessage> batch = CreateProxy<IBatchConfigurator<ConfigurationMessage>>(
            out RecordingProxy batchProxy);
        var consumer = new ConfigurationBatchConsumer();
        var suppliedFactory = new InstanceConsumerFactory<ConfigurationBatchConsumer>(consumer);

        BatchConsumerExtensions.Consumer<ConfigurationBatchConsumer, ConfigurationMessage>(batch, suppliedFactory);
        BatchConsumerExtensions.Consumer<ConfigurationBatchConsumer, ConfigurationMessage>(batch, () => consumer);

        Assert.Equal(2, batchProxy.Invocations.Count);
        Assert.Same(suppliedFactory, batchProxy.Invocations[0].Arguments[0]);
        Assert.IsType<DelegateConsumerFactory<ConfigurationBatchConsumer>>(batchProxy.Invocations[1].Arguments[0]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-DI-REGISTRATION", "scoped-factory-forwarding")]
    public void DependencyInjectionRegistration_ForwardsScopedFactoriesAndCallbacks()
    {
        IRegistrationContext registrationContext = CreateProxy<IRecordingRegistrationContext>(out _);
        IBatchConfigurator<ConfigurationMessage> batch = CreateProxy<IBatchConfigurator<ConfigurationMessage>>(
            out RecordingProxy batchProxy);
        Action<IConsumerMessageConfigurator<ConfigurationBatchConsumer, IMessageBatch<ConfigurationMessage>>> batchCallback = _ => { };

        DependencyInjectionReceiveEndpointExtensions.Consumer<ConfigurationBatchConsumer, ConfigurationMessage>(
            batch,
            registrationContext,
            batchCallback);

        Invocation batchInvocation = Assert.Single(batchProxy.Invocations);
        Assert.IsType<ScopeConsumerFactory<ConfigurationBatchConsumer>>(batchInvocation.Arguments[0]);
        Assert.Same(batchCallback, batchInvocation.Arguments[1]);

        IReceiveEndpointConfigurator endpoint = CreateProxy<IReceiveEndpointConfigurator>(out RecordingProxy endpointProxy);
        var callbackInvoked = false;
        DependencyInjectionReceiveEndpointExtensions.Consumer<OtherConsumer>(
            endpoint,
            registrationContext,
            _ => callbackInvoked = true);

        Assert.True(callbackInvoked);
        Invocation endpointInvocation = Assert.Single(endpointProxy.Invocations);
        Assert.Equal("AddEndpointSpecification", endpointInvocation.Method.Name);
        Assert.IsType<ConsumerConfigurator<OtherConsumer>>(endpointInvocation.Arguments[0]);

        IConsumePipeConnector consumePipe = CreateProxy<IConsumePipeConnector>(out RecordingProxy consumePipeProxy);
        ConnectHandle connection = DependencyInjectionReceiveEndpointExtensions.ConnectConsumer<OtherConsumer>(
            consumePipe,
            registrationContext,
            []);

        Assert.NotNull(connection);
        Invocation consumePipeInvocation = Assert.Single(consumePipeProxy.Invocations);
        Assert.Equal("ConnectConsumePipe", consumePipeInvocation.Method.Name);
        connection.Dispose();
    }

    private static TContract CreateProxy<TContract>(out RecordingProxy proxy)
        where TContract : class
    {
        TContract contract = DispatchProxy.Create<TContract, RecordingProxy>();
        proxy = (RecordingProxy)(object)contract;
        return contract;
    }

    private class RecordingProxy : DispatchProxy
    {
        public List<Invocation> Invocations { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            Invocations.Add(new Invocation(targetMethod, args is null ? [] : [.. args]));
            if (targetMethod.ReturnType == typeof(void))
                return null;
            if (targetMethod.ReturnType == typeof(ConnectHandle))
                return new ViciOne.ServiceBus.Util.EmptyConnectHandle();

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private sealed class EmptyPipeSpecification<TContext> : IPipeSpecification<TContext>
        where TContext : class, PipeContext
    {
        public IEnumerable<ValidationResult> Validate() => [];

        public void Apply(IPipeBuilder<TContext> builder)
        {
        }
    }

    private interface IRecordingRegistrationContext :
        IRegistrationContext,
        ISetScopedConsumeContext
    {
    }

    private sealed record Invocation(MethodInfo Method, object?[] Arguments);

    private sealed class ConfigurationBatchConsumer : IConsumer<IMessageBatch<ConfigurationMessage>>
    {
        public Task ConsumeAsync(ConsumeContext<IMessageBatch<ConfigurationMessage>> context) => Task.CompletedTask;
    }

    private sealed class OtherConsumer : IConsumer<OtherMessage>
    {
        public Task ConsumeAsync(ConsumeContext<OtherMessage> context) => Task.CompletedTask;
    }

    private sealed record ConfigurationMessage(string Value);

    private sealed record OtherMessage(string Value);
}
