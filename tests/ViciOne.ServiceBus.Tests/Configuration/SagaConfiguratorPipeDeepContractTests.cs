using System.Reflection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Rescue;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class SagaConfiguratorPipeDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-CONFIGURATION", "configurator-required-owners-and-owned-identity")]
    public void ConstructorAndConfigure_RejectMissingOwnersBeforeAnyObserverOrBuilderEffect()
    {
        var repository = new StubSagaRepository();
        var observer = new RecordingSagaObserver();

        ArgumentNullException repositoryException = Assert.Throws<ArgumentNullException>(() =>
            new SagaConfigurator<ConfiguredSaga>(null!, observer));
        ArgumentNullException observerException = Assert.Throws<ArgumentNullException>(() =>
            new SagaConfigurator<ConfiguredSaga>(repository, null!));

        Assert.Equal("sagaRepository", repositoryException.ParamName);
        Assert.Equal("observer", observerException.ParamName);
        Assert.Equal(0, observer.TotalCalls);

        var configurator = new SagaConfigurator<ConfiguredSaga>(repository, observer);
        ArgumentNullException builderException = Assert.Throws<ArgumentNullException>(() => configurator.Configure(null!));
        ArgumentNullException messageException = Assert.Throws<ArgumentNullException>(() =>
            configurator.Message<ConfiguredMessage>(null!));
        ArgumentNullException sagaMessageException = Assert.Throws<ArgumentNullException>(() =>
            configurator.SagaMessage<ConfiguredMessage>(null!));
        var specification = Assert.IsType<SagaSpecification<ConfiguredSaga>>(ReadField(configurator, "_specification"));
        ArgumentException undeclaredMessageException = Assert.Throws<ArgumentException>(() =>
            specification.GetMessageSpecification<UndeclaredMessage>());

        Assert.Equal("builder", builderException.ParamName);
        Assert.Equal("configure", messageException.ParamName);
        Assert.Equal("configure", sagaMessageException.ParamName);
        Assert.Contains(nameof(UndeclaredMessage), undeclaredMessageException.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(ConfiguredSaga), undeclaredMessageException.Message, StringComparison.Ordinal);
        Assert.Same(repository, ReadField(configurator, "_sagaRepository"));
        Assert.Same(specification, ReadField(configurator, "_specification"));
        Assert.Equal(0, observer.TotalCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-CONFIGURATION", "configurator-message-options-and-observer-forwarding")]
    public void ConfigurationSurfaces_ForwardExactCallbacksOptionsSpecificationsAndObserverHandles()
    {
        var initialObserver = new RecordingSagaObserver();
        var configurator = new SagaConfigurator<ConfiguredSaga>(new StubSagaRepository(), initialObserver);
        ISagaMessageConfigurator<ConfiguredMessage>? messageConfigurator = null;
        ISagaMessageConfigurator<ConfiguredSaga, ConfiguredMessage>? sagaMessageConfigurator = null;
        var messageCalls = 0;
        var sagaMessageCalls = 0;

        configurator.Message<ConfiguredMessage>(configured =>
        {
            messageCalls++;
            messageConfigurator = configured;
        });
        configurator.SagaMessage<ConfiguredMessage>(configured =>
        {
            sagaMessageCalls++;
            sagaMessageConfigurator = configured;
        });

        Assert.Equal(1, messageCalls);
        Assert.Equal(1, sagaMessageCalls);
        Assert.Same(messageConfigurator, sagaMessageConfigurator);

        var sharedSpecification = new RecordingSagaPipeSpecification("shared");
        configurator.AddPipeSpecification(sharedSpecification);

        CreatedOptions? createdCallbackArgument = null;
        CreatedOptions created = configurator.Options<CreatedOptions>(options =>
        {
            createdCallbackArgument = options;
            options.Value = 17;
        });
        var supplied = new SuppliedOptions { Value = 19 };
        SuppliedOptions? suppliedCallbackArgument = null;
        SuppliedOptions suppliedResult = configurator.Options(supplied, options =>
        {
            suppliedCallbackArgument = options;
            options.Value = 23;
        });

        Assert.Same(created, createdCallbackArgument);
        Assert.Equal(17, created.Value);
        Assert.Same(supplied, suppliedCallbackArgument);
        Assert.Same(supplied, suppliedResult);
        Assert.Equal(23, supplied.Value);
        Assert.True(configurator.TryGetOptions(out CreatedOptions selectedCreated));
        Assert.Same(created, selectedCreated);
        Assert.True(configurator.TryGetOptions(out SuppliedOptions selectedSupplied));
        Assert.Same(supplied, selectedSupplied);
        Assert.Collection(
            configurator.SelectOptions<IConfiguredOption>(),
            selected => Assert.Same(created, selected),
            selected => Assert.Same(supplied, selected));

        var disconnectedObserver = new RecordingSagaObserver();
        ConnectHandle handle = configurator.ConnectSagaConfigurationObserver(disconnectedObserver);
        handle.Dispose();

        ValidationResult[] validation = configurator.Validate().ToArray();

        Assert.Collection(validation, failure => Assert.Equal("shared", failure.Key));
        Assert.Equal(1, sharedSpecification.ValidateCalls);
        Assert.Equal(1, initialObserver.SagaCalls);
        Assert.Equal(1, initialObserver.MessageCalls);
        Assert.Equal(0, disconnectedObserver.TotalCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONFIGURATION-OBSERVER-LIFECYCLE", "saga-notify-once-before-repeatable-validation")]
    public void Validate_NotifiesObserversOnceBeforeTheFirstSnapshotAndValidatesTheirMutationEveryTime()
    {
        var injectedSpecification = new RecordingSagaPipeSpecification("observer-injected");
        var observer = new RecordingSagaObserver(injectedSpecification);
        var configurator = new SagaConfigurator<ConfiguredSaga>(new StubSagaRepository(), observer);
        var options = new ValidatingOptions("options");
        configurator.Options(options, null);

        ValidationResult[] first = configurator.Validate().ToArray();
        ValidationResult[] second = configurator.Validate().ToArray();

        Assert.Collection(
            first,
            failure => Assert.Equal("observer-injected", failure.Key),
            failure => Assert.Equal("options", failure.Key));
        Assert.Collection(
            second,
            failure => Assert.Equal("observer-injected", failure.Key),
            failure => Assert.Equal("options", failure.Key));
        Assert.Equal(1, observer.SagaCalls);
        Assert.Equal(1, observer.MessageCalls);
        Assert.Equal(0, observer.StateMachineCalls);
        Assert.Same(ReadField(configurator, "_specification"), observer.SagaConfigurator);
        Assert.Equal(2, injectedSpecification.ValidateCalls);
        Assert.Equal(2, options.ValidateCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DIRECT-CONNECT", "configurator-builds-one-declared-message-pipe")]
    public void Configure_ConnectsTheDeclaredMessagePipeExactlyOnceUsingTheSuppliedBuilder()
    {
        var repository = new StubSagaRepository();
        var configurator = new SagaConfigurator<ConfiguredSaga>(repository, new RecordingSagaObserver());
        var builder = new RecordingReceiveEndpointBuilder();

        configurator.Configure(builder);

        PipeConnection connection = Assert.Single(builder.Connections);
        Assert.Equal(typeof(ConfiguredMessage), connection.MessageType);
        Assert.NotNull(connection.Pipe);
        Assert.Equal(ConnectPipeOptions.ConfigureConsumeTopology, connection.Options);
        Assert.Same(repository, ReadField(configurator, "_sagaRepository"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-CONFIGURATION", "deferred-null-collaborator-validation")]
    public void PipeSpecifications_DeferMissingCollaboratorsToPreciseValidationFailures()
    {
        var rescue = new SagaConsumeContextRescuePipeSpecification<PipeSaga>(null!);
        var filter = new SagaFilterSpecification<PipeSaga, PipeMessage>(null!);

        ValidationResult rescueFailure = Assert.Single(rescue.Validate());
        ValidationResult filterFailure = Assert.Single(filter.Validate());

        Assert.Equal(ValidationResultDisposition.Failure, rescueFailure.Disposition);
        Assert.Equal("RescuePipe", rescueFailure.Key);
        Assert.Equal("must not be null", rescueFailure.Message);
        Assert.Equal(ValidationResultDisposition.Failure, filterFailure.Disposition);
        Assert.Equal("Filter", filterFailure.Key);
        Assert.Equal("must not be null", filterFailure.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-CONFIGURATION", "single-wrapper-builder-mutation-and-failure-identity")]
    public void PipeSpecifications_ApplyOneIdentityPreservingWrapperAndPropagateTheBuilderFailure()
    {
        IPipe<ExceptionSagaConsumeContext<PipeSaga>> rescuePipe = StrictStub<IPipe<ExceptionSagaConsumeContext<PipeSaga>>>();
        IFilter<SagaConsumeContext<PipeSaga>> sagaFilter = StrictStub<IFilter<SagaConsumeContext<PipeSaga>>>();
        var rescue = new SagaConsumeContextRescuePipeSpecification<PipeSaga>(rescuePipe);
        var filter = new SagaFilterSpecification<PipeSaga, PipeMessage>(sagaFilter);
        var rescueBuilder = new RecordingPipeBuilder<SagaConsumeContext<PipeSaga>>();
        var filterBuilder = new RecordingPipeBuilder<SagaConsumeContext<PipeSaga, PipeMessage>>();

        rescue.Apply(rescueBuilder);
        filter.Apply(filterBuilder);

        object rescueWrapper = Assert.Single(rescueBuilder.Filters);
        var filterWrapper = Assert.IsType<SagaSplitFilter<PipeSaga, PipeMessage>>(Assert.Single(filterBuilder.Filters));
        Assert.Equal("RescueFilter`2", rescueWrapper.GetType().Name);
        Assert.Same(rescuePipe, ReadField(rescueWrapper, "_rescuePipe"));
        Assert.Same(sagaFilter, ReadField(filterWrapper, "_next"));
        var publishProvider = StrictStub<IPublishEndpointProvider>();
        var receiveContext = ConfigurableStub<ReceiveContext>(new Dictionary<string, object?>
        {
            ["get_PublishEndpointProvider"] = publishProvider
        });
        var serializerContext = StrictStub<SerializerContext>();
        var rescueContext = ConfigurableStub<SagaConsumeContext<PipeSaga>>(new Dictionary<string, object?>
        {
            ["get_ReceiveContext"] = receiveContext,
            ["get_SerializerContext"] = serializerContext
        });
        var rescueException = new InvalidOperationException("projected");
        var contextFactory = Assert.IsAssignableFrom<Delegate>(ReadField(rescueWrapper, "_rescueContextFactory"));
        var projected = Assert.IsType<RescueExceptionSagaConsumeContext<PipeSaga>>(
            contextFactory.DynamicInvoke(rescueContext, rescueException));
        Assert.Same(rescueContext, ReadField(projected, "_context"));
        Assert.Same(rescueException, projected.Exception);
        Assert.Empty(rescue.Validate());
        Assert.Empty(filter.Validate());

        var rescueFailure = new InvalidOperationException("rescue builder failed");
        var filterFailure = new InvalidOperationException("filter builder failed");
        var failingRescueBuilder = new RecordingPipeBuilder<SagaConsumeContext<PipeSaga>>(rescueFailure);
        var failingFilterBuilder = new RecordingPipeBuilder<SagaConsumeContext<PipeSaga, PipeMessage>>(filterFailure);

        Assert.Same(rescueFailure, Assert.Throws<InvalidOperationException>(() => rescue.Apply(failingRescueBuilder)));
        Assert.Same(filterFailure, Assert.Throws<InvalidOperationException>(() => filter.Apply(failingFilterBuilder)));
        Assert.Single(failingRescueBuilder.Filters);
        Assert.Single(failingFilterBuilder.Filters);
    }

    static object? ReadField(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"The field '{fieldName}' was not found on {target.GetType()}.");
        return field.GetValue(target);
    }

    static T StrictStub<T>()
        where T : class => DispatchProxy.Create<T, StrictDispatchProxy>();

    static T ConfigurableStub<T>(Dictionary<string, object?> results)
        where T : class
    {
        T stub = DispatchProxy.Create<T, ConfigurableDispatchProxy>();
        ((ConfigurableDispatchProxy)(object)stub).Results = results;
        return stub;
    }

    class StrictDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"Unexpected collaborator call: {targetMethod?.Name}.");
    }

    class ConfigurableDispatchProxy : DispatchProxy
    {
        public required Dictionary<string, object?> Results { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (Results.TryGetValue(targetMethod.Name, out object? result))
                return result;

            throw new InvalidOperationException($"Unexpected collaborator call: {targetMethod.Name}.");
        }
    }

    sealed class RecordingSagaObserver(RecordingSagaPipeSpecification? injectedSpecification = null) :
        ISagaConfigurationObserver
    {
        public int SagaCalls { get; private set; }

        public int StateMachineCalls { get; private set; }

        public int MessageCalls { get; private set; }

        public int TotalCalls => SagaCalls + StateMachineCalls + MessageCalls;

        public object? SagaConfigurator { get; private set; }

        public void SagaConfigured<TSaga>(ISagaConfigurator<TSaga> configurator)
            where TSaga : class
        {
            SagaCalls++;
            SagaConfigurator = configurator;

            if (injectedSpecification != null && configurator is ISagaConfigurator<ConfiguredSaga> configured)
                configured.AddPipeSpecification(injectedSpecification);
        }

        public void StateMachineSagaConfigured<TInstance>(ISagaConfigurator<TInstance> configurator, object stateMachine)
            where TInstance : class => StateMachineCalls++;

        public void SagaMessageConfigured<TSaga, TMessage>(ISagaMessageConfigurator<TSaga, TMessage> configurator)
            where TSaga : class
            where TMessage : class => MessageCalls++;
    }

    sealed class RecordingSagaPipeSpecification(string key) :
        IPipeSpecification<SagaConsumeContext<ConfiguredSaga>>
    {
        public int ValidateCalls { get; private set; }

        public void Apply(IPipeBuilder<SagaConsumeContext<ConfiguredSaga>> builder)
        {
        }

        public IEnumerable<ValidationResult> Validate()
        {
            ValidateCalls++;
            yield return this.Failure(key, "synthetic validation failure");
        }
    }

    sealed class RecordingReceiveEndpointBuilder : IReceiveEndpointBuilder
    {
        public List<PipeConnection> Connections { get; } = [];

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
            where T : class
        {
            Connections.Add(new PipeConnection(typeof(T), pipe, ConnectPipeOptions.ConfigureConsumeTopology));
            return new RecordingConnectHandle();
        }

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
            where T : class
        {
            Connections.Add(new PipeConnection(typeof(T), pipe, options));
            return new RecordingConnectHandle();
        }
    }

    sealed class RecordingPipeBuilder<TContext>(Exception? failure = null) : IPipeBuilder<TContext>
        where TContext : class, PipeContext
    {
        public List<IFilter<TContext>> Filters { get; } = [];

        public void AddFilter(IFilter<TContext> filter)
        {
            Filters.Add(filter);

            if (failure != null)
                throw failure;
        }
    }

    sealed class RecordingConnectHandle : ConnectHandle
    {
        public void Disconnect()
        {
        }

        public void Dispose()
        {
        }
    }

    sealed class StubSagaRepository : ISagaRepository<ConfiguredSaga>
    {
        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync<T>(
            ConsumeContext<T> context,
            ISagaPolicy<ConfiguredSaga, T> policy,
            IPipe<SagaConsumeContext<ConfiguredSaga, T>> next)
            where T : class => throw new NotSupportedException();

        public Task SendQueryAsync<T>(
            ConsumeContext<T> context,
            ISagaQuery<ConfiguredSaga> query,
            ISagaPolicy<ConfiguredSaga, T> policy,
            IPipe<SagaConsumeContext<ConfiguredSaga, T>> next)
            where T : class => throw new NotSupportedException();
    }

    interface IConfiguredOption
    {
    }

    sealed class CreatedOptions : IOptions, IConfiguredOption
    {
        public int Value { get; set; }
    }

    sealed class SuppliedOptions : IOptions, IConfiguredOption
    {
        public int Value { get; set; }
    }

    sealed class ValidatingOptions(string key) : IOptions, ISpecification
    {
        public int ValidateCalls { get; private set; }

        public IEnumerable<ValidationResult> Validate()
        {
            ValidateCalls++;
            yield return this.Failure(key, "synthetic options failure");
        }
    }

    sealed class ConfiguredSaga : ISaga, IInitiatedBy<ConfiguredMessage>
    {
        public Guid CorrelationId { get; set; }

        public Task ConsumeAsync(ConsumeContext<ConfiguredMessage> context) => Task.CompletedTask;
    }

    sealed record ConfiguredMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    sealed record UndeclaredMessage;

    sealed class PipeSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    sealed record PipeMessage;

    sealed record PipeConnection(Type MessageType, object Pipe, ConnectPipeOptions Options);
}
