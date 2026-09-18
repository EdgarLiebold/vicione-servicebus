using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class SagaMessageObserverAdapterDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-MESSAGE-OBSERVER-ADAPTER", "required-constructor-input-guard-ownership")]
    public void Constructors_RejectEveryMissingRequiredInputAtTheOwningBoundary()
    {
        var configurator = new RecordingSagaConfigurator<ObserverSaga>();
        Action<IOutboxConfigurator> outbox = static _ => { };
        Action<IRedeliveryConfigurator> redelivery = static _ => { };
        Action<IRetryConfigurator> retry = static _ => { };

        ArgumentNullException concurrency = AssertConstructorThrows<ArgumentNullException>(
            GetConcurrencyConstructor(), null, 1, null);
        Assert.Equal("configurator", concurrency.ParamName);
        AssertArgument("context", () =>
            new InMemoryOutboxSagaConfigurationObserver<ObserverSaga>((IRegistrationContext)null!, configurator, outbox));
        AssertArgument("configurator", () =>
            new InMemoryOutboxSagaConfigurationObserver<ObserverSaga>((ISetScopedConsumeContext?)null, null!, outbox));
        AssertArgument("configurator", () => new DelayedRedeliverySagaConfigurationObserver<ObserverSaga>(null!, redelivery));
        AssertArgument("configure", () => new DelayedRedeliverySagaConfigurationObserver<ObserverSaga>(configurator, null!));
        AssertArgument("configurator", () => new ScheduledRedeliverySagaConfigurationObserver<ObserverSaga>(null!, retry));
        AssertArgument("configure", () => new ScheduledRedeliverySagaConfigurationObserver<ObserverSaga>(configurator, null!));
        AssertArgument("configurator", () => new MessageRetrySagaConfigurationObserver<ObserverSaga>(null!, default, retry));
        AssertArgument("configure", () => new MessageRetrySagaConfigurationObserver<ObserverSaga>(configurator, default, null!));

        IRegistrationContext unsupported = DispatchProxy.Create<IRegistrationContext, EmptyProxy>();
        ArgumentException mismatch = Assert.Throws<ArgumentException>(() =>
            new InMemoryOutboxSagaConfigurationObserver<ObserverSaga>(unsupported, configurator, outbox));
        Assert.Equal("context", mismatch.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-MESSAGE-OBSERVER-ADAPTER", "saga-and-state-machine-notifications-are-noops")]
    public void NonMessageNotifications_AreSideEffectFreeForEveryAdapter()
    {
        var configurator = new RecordingSagaConfigurator<ObserverSaga>();
        ISagaConfigurationObserver[] observers =
        [
            CreateConcurrencyObserver(configurator, 1),
            new InMemoryOutboxSagaConfigurationObserver<ObserverSaga>((ISetScopedConsumeContext?)null, configurator, null),
            new DelayedRedeliverySagaConfigurationObserver<ObserverSaga>(configurator, static _ => { }),
            new ScheduledRedeliverySagaConfigurationObserver<ObserverSaga>(configurator, static _ => { }),
            new MessageRetrySagaConfigurationObserver<ObserverSaga>(configurator, default, static _ => { })
        ];

        foreach (ISagaConfigurationObserver observer in observers)
        {
            observer.SagaConfigured<OtherSaga>(null!);
            observer.StateMachineSagaConfigured<OtherSaga>(null!, null!);
        }

        Assert.Equal(0, configurator.MessageCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-MESSAGE-OBSERVER-ADAPTER", "concurrency-shared-limiter-single-specification")]
    public void ConcurrencyObserver_ReusesOneLimiterAndAddsOneSpecificationPerMessage()
    {
        var configurator = new RecordingSagaConfigurator<ObserverSaga>();
        ISagaConfigurationObserver observer = CreateConcurrencyObserver(configurator, 3, "saga-limit");
        object limiter = Assert.IsAssignableFrom<object>(
            observer.GetType().GetProperty("Limiter", BindingFlags.Instance | BindingFlags.Public)!.GetValue(observer));

        NotifyMessage(observer);
        object first = Assert.Single(configurator.LastMessageConfigurator!.Specifications);
        NotifyMessage(observer);
        object second = Assert.Single(configurator.LastMessageConfigurator!.Specifications);

        Assert.Equal("ConcurrencyLimitConsumePipeSpecification`1", first.GetType().Name);
        Assert.Equal(first.GetType(), second.GetType());
        Assert.Same(limiter, ReadField(first, "_limiter"));
        Assert.Same(limiter, ReadField(second, "_limiter"));
        Assert.Equal(2, configurator.MessageCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-MESSAGE-OBSERVER-ADAPTER", "outbox-scoped-context-callback-and-single-specification")]
    public void OutboxObserver_PreservesSetterInvokesCallbackAndAddsOneSpecification()
    {
        var configurator = new RecordingSagaConfigurator<ObserverSaga>();
        ISetScopedConsumeContext setter = DispatchProxy.Create<ISetScopedConsumeContext, EmptyProxy>();
        IOutboxConfigurator? callbackArgument = null;
        var observer = new InMemoryOutboxSagaConfigurationObserver<ObserverSaga>(setter, configurator, value => callbackArgument = value);

        NotifyMessage(observer);

        object specification = Assert.Single(configurator.LastMessageConfigurator!.Specifications);
        Assert.Equal("InMemoryOutboxSpecification`1", specification.GetType().Name);
        Assert.Same(specification, callbackArgument);
        Assert.Same(setter, ReadField(specification, "_setter"));
        Assert.Equal(1, configurator.MessageCalls);

        var withoutCallback = new RecordingSagaConfigurator<ObserverSaga>();
        NotifyMessage(new InMemoryOutboxSagaConfigurationObserver<ObserverSaga>(setter, withoutCallback, null));
        Assert.Single(withoutCallback.LastMessageConfigurator!.Specifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-MESSAGE-OBSERVER-ADAPTER", "delayed-and-scheduled-redelivery-ordered-pair")]
    public void RedeliveryObservers_ConfigureRetryAndAddRedeliveryBeforeRetry()
    {
        var delayedConfigurator = new RecordingSagaConfigurator<ObserverSaga>();
        IRedeliveryConfigurator? delayedCallback = null;
        var delayed = new DelayedRedeliverySagaConfigurationObserver<ObserverSaga>(delayedConfigurator, value => delayedCallback = value);
        NotifyMessage(delayed);
        AssertRedeliveryPair(delayedConfigurator, "DelayedRedeliveryPipeSpecification`1", delayedCallback);

        var scheduledConfigurator = new RecordingSagaConfigurator<ObserverSaga>();
        IRetryConfigurator? scheduledCallback = null;
        var scheduled = new ScheduledRedeliverySagaConfigurationObserver<ObserverSaga>(scheduledConfigurator, value => scheduledCallback = value);
        NotifyMessage(scheduled);
        AssertRedeliveryPair(scheduledConfigurator, "ScheduledRedeliveryPipeSpecification`1", scheduledCallback);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-MESSAGE-OBSERVER-ADAPTER", "message-retry-token-callback-and-context-factory")]
    public void RetryObserver_PreservesTokenConfiguresAndAddsOneContextRetrySpecification()
    {
        var configurator = new RecordingSagaConfigurator<ObserverSaga>();
        using var source = new CancellationTokenSource();
        IRetryConfigurator? callbackArgument = null;
        var observer = new MessageRetrySagaConfigurationObserver<ObserverSaga>(configurator, source.Token, value => callbackArgument = value);

        NotifyMessage(observer);

        object specification = Assert.Single(configurator.LastMessageConfigurator!.Specifications);
        Assert.Equal("ConsumeContextRetryPipeSpecification`2", specification.GetType().Name);
        Assert.Same(specification, callbackArgument);
        Assert.Equal(source.Token, Assert.IsType<CancellationToken>(ReadField(specification, "_cancellationToken")));
        Delegate factory = Assert.IsAssignableFrom<Delegate>(ReadField(specification, "_contextFactory"));
        ICombinedObserverContext context = DispatchProxy.Create<ICombinedObserverContext, EmptyProxy>();
        var contextProxy = (EmptyProxy)(object)context;
        ReceiveContext receiveContext = DispatchProxy.Create<ReceiveContext, EmptyProxy>();
        ((EmptyProxy)(object)receiveContext).Results["get_PublishEndpointProvider"] =
            DispatchProxy.Create<IPublishEndpointProvider, EmptyProxy>();
        contextProxy.Results["get_ReceiveContext"] = receiveContext;
        contextProxy.Results["get_SerializerContext"] = DispatchProxy.Create<SerializerContext, EmptyProxy>();
        object? retryContext = factory.DynamicInvoke(context, Retry.None, null);
        Assert.NotNull(retryContext);
        Assert.Same(context, ReadField(retryContext, "_context"));
        Assert.Equal(1, configurator.MessageCalls);
    }

    private static void NotifyMessage(ISagaConfigurationObserver observer) =>
        observer.SagaMessageConfigured<ObserverSaga, ObserverMessage>(null!);

    private static ISagaConfigurationObserver CreateConcurrencyObserver(
        ISagaConfigurator<ObserverSaga> configurator,
        int limit,
        string? limiterId = null)
    {
        object? instance = GetConcurrencyConstructor().Invoke([configurator, limit, limiterId]);
        return Assert.IsAssignableFrom<ISagaConfigurationObserver>(instance);
    }

    private static ConstructorInfo GetConcurrencyConstructor()
    {
        Type? openType = typeof(InMemoryOutboxSagaConfigurationObserver<>).Assembly.GetType(
            "ViciOne.ServiceBus.Configuration.ConcurrencyLimitSagaConfigurationObserver`1");
        Assert.NotNull(openType);
        Type closedType = openType.MakeGenericType(typeof(ObserverSaga));
        return Assert.Single(closedType.GetConstructors(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
    }

    private static TException AssertConstructorThrows<TException>(ConstructorInfo constructor, params object?[] arguments)
        where TException : Exception
    {
        TargetInvocationException invocation = Assert.Throws<TargetInvocationException>(() => constructor.Invoke(arguments));
        return Assert.IsType<TException>(invocation.InnerException);
    }

    private static void AssertRedeliveryPair(
        RecordingSagaConfigurator<ObserverSaga> configurator,
        string firstTypeName,
        object? callbackArgument)
    {
        Assert.Equal(1, configurator.MessageCalls);
        object[] specifications = configurator.LastMessageConfigurator!.Specifications.ToArray();
        Assert.Equal(2, specifications.Length);
        Assert.Equal(firstTypeName, specifications[0].GetType().Name);
        Assert.Equal("RedeliveryRetryPipeSpecification`1", specifications[1].GetType().Name);
        Assert.Same(specifications[1], callbackArgument);
        Assert.Same(specifications[0], ReadField(specifications[1], "_redeliveryPipeSpecification"));
    }

    private static object? ReadField(object owner, string name)
    {
        FieldInfo? field = owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return field.GetValue(owner);
    }

    private static void AssertArgument(string parameterName, Action action)
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(parameterName, exception.ParamName);
    }

    private sealed class RecordingSagaConfigurator<TSaga> : ISagaConfigurator<TSaga>
        where TSaga : class
    {
        public int MessageCalls { get; private set; }
        public RecordingMessageConfigurator<ObserverMessage>? LastMessageConfigurator { get; private set; }

        public int? ConcurrentMessageLimit { set { } }

        public void Message<T>(Action<ISagaMessageConfigurator<T>> configure)
            where T : class
        {
            Assert.Equal(typeof(ObserverMessage), typeof(T));
            var messageConfigurator = new RecordingMessageConfigurator<ObserverMessage>();
            LastMessageConfigurator = messageConfigurator;
            MessageCalls++;
            ((Action<ISagaMessageConfigurator<ObserverMessage>>)(object)configure)(messageConfigurator);
        }

        public void SagaMessage<T>(Action<ISagaMessageConfigurator<TSaga, T>> configure)
            where T : class => throw new NotSupportedException();

        public void AddPipeSpecification(IPipeSpecification<SagaConsumeContext<TSaga>> specification) =>
            throw new NotSupportedException();

        public ConnectHandle ConnectSagaConfigurationObserver(ISagaConfigurationObserver observer) =>
            throw new NotSupportedException();

        public T Options<T>(Action<T>? configure = null)
            where T : IOptions, new() => throw new NotSupportedException();

        public T Options<T>(T options, Action<T>? configure = null)
            where T : IOptions => throw new NotSupportedException();

        public bool TryGetOptions<T>(out T options)
            where T : IOptions => throw new NotSupportedException();

        public IEnumerable<T> SelectOptions<T>()
            where T : class => throw new NotSupportedException();
    }

    private sealed class RecordingMessageConfigurator<TMessage> : ISagaMessageConfigurator<TMessage>
        where TMessage : class
    {
        public List<object> Specifications { get; } = [];

        public void AddPipeSpecification(IPipeSpecification<ConsumeContext<TMessage>> specification) =>
            Specifications.Add(specification);
    }

    private class EmptyProxy : DispatchProxy
    {
        public Dictionary<string, object?> Results { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod is not null && Results.TryGetValue(targetMethod.Name, out object? result)
                ? result
                : targetMethod?.ReturnType.IsValueType == true
                    ? Activator.CreateInstance(targetMethod.ReturnType)
                    : null;
    }

    private sealed class ObserverSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class OtherSaga;

    private sealed class ObserverMessage;

    private interface ICombinedObserverContext : ConsumeContext, ConsumeContext<ObserverMessage>;
}
