using System.Reflection;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineRuntimeMetadataContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "runtime-metadata-required-arguments")]
    public void RequestMetadata_RejectsEveryMissingRequiredArgument()
    {
        IRequestSettings<MetadataSaga, RequestMessage, ResponseMessage> settings =
            CreateProxy<IRequestSettings<MetadataSaga, RequestMessage, ResponseMessage>>();

        Assert.Equal("name", Assert.Throws<ArgumentNullException>(() =>
            new ViciOneServiceBusStateMachine<MetadataSaga>.StateMachineRequest<RequestMessage, ResponseMessage>(null!, settings)).ParamName);
        Assert.Equal("settings", Assert.Throws<ArgumentNullException>(() =>
            new ViciOneServiceBusStateMachine<MetadataSaga>.StateMachineRequest<RequestMessage, ResponseMessage>("Request", null!)).ParamName);

        var correlationRequest =
            new ViciOneServiceBusStateMachine<MetadataSaga>.StateMachineRequest<RequestMessage, ResponseMessage>("Correlation", settings);
        var storedRequest =
            new ViciOneServiceBusStateMachine<MetadataSaga>.StateMachineRequest<RequestMessage, ResponseMessage>(
                "Stored",
                settings,
                instance => instance.RequestId);

        Assert.Equal("instance", Assert.Throws<ArgumentNullException>(() => correlationRequest.GenerateRequestId(null!)).ParamName);
        Assert.Equal("instance", Assert.Throws<ArgumentNullException>(() => storedRequest.GenerateRequestId(null!)).ParamName);
        Assert.Equal("instance", Assert.Throws<ArgumentNullException>(() => storedRequest.SetRequestId(null!, null)).ParamName);
        Assert.Equal("instance", Assert.Throws<ArgumentNullException>(() => storedRequest.GetRequestId(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => correlationRequest.SetSendContextHeaders(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => correlationRequest.EventFilter(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "runtime-metadata-storage-headers-and-filtering")]
    public void RequestMetadata_PreservesStorageHeadersAcceptedResponsesAndTimeoutFiltering()
    {
        TimeSpan lifetime = TimeSpan.FromMinutes(3);
        IRequestSettings<MetadataSaga, RequestMessage, ResponseMessage, ResponseMessage2, ResponseMessage3> settings =
            CreateProxy<IRequestSettings<MetadataSaga, RequestMessage, ResponseMessage, ResponseMessage2, ResponseMessage3>>(
                (method, _) => method.Name == "get_TimeToLive" ? lifetime : DefaultValue(method.ReturnType));
        var request = new ViciOneServiceBusStateMachine<MetadataSaga>
            .StateMachineRequest<RequestMessage, ResponseMessage, ResponseMessage2, ResponseMessage3>(
                "Stored",
                settings,
                instance => instance.RequestId);
        var saga = new MetadataSaga();
        Guid storedId = NewId.NextGuid();

        request.SetRequestId(saga, storedId);

        Assert.Equal("Stored", request.Name);
        Assert.Same(settings, request.Settings);
        Assert.Same(settings, ((IRequest<MetadataSaga, RequestMessage, ResponseMessage>)request).Settings);
        Assert.Same(settings, ((IRequest<MetadataSaga, RequestMessage, ResponseMessage, ResponseMessage2>)request).Settings);
        Assert.Equal(storedId, request.GetRequestId(saga));
        Assert.NotEqual(storedId, request.GenerateRequestId(saga));

        var headers = new DictionarySendHeaders();
        TimeSpan? assignedLifetime = null;
        SendContext<RequestMessage> sendContext = CreateProxy<SendContext<RequestMessage>>((method, arguments) =>
        {
            if (method.Name == "get_Headers")
                return headers;
            if (method.Name == "set_TimeToLive")
            {
                assignedLifetime = Assert.IsType<TimeSpan>(arguments![0]);
                return null;
            }

            return DefaultValue(method.ReturnType);
        });

        request.SetSendContextHeaders(sendContext);

        Assert.Equal(lifetime, assignedLifetime);
        Assert.True(headers.TryGetHeader(MessageHeaders.Request.Accept, out object? accepted));
        Assert.Equal(
            [
                MessageUrn.ForTypeString<ResponseMessage>(),
                MessageUrn.ForTypeString<ResponseMessage2>(),
                MessageUrn.ForTypeString<ResponseMessage3>(),
            ],
            Assert.IsAssignableFrom<IEnumerable<string>>(accepted));

        Assert.False(request.EventFilter(CreateTimeoutContext(saga, null)));
        Assert.True(request.EventFilter(CreateTimeoutContext(saga, storedId)));
        Assert.False(request.EventFilter(CreateTimeoutContext(saga, NewId.NextGuid())));

        var correlationRequest =
            new ViciOneServiceBusStateMachine<MetadataSaga>.StateMachineRequest<RequestMessage, ResponseMessage>(
                "Correlation",
                settings);
        correlationRequest.SetRequestId(saga, NewId.NextGuid());
        Assert.Equal(saga.CorrelationId, correlationRequest.GetRequestId(saga));
        Assert.Equal(saga.CorrelationId, correlationRequest.GenerateRequestId(saga));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-SCHEDULE", "runtime-metadata-required-arguments")]
    public void ScheduleMetadata_RejectsEveryMissingRequiredArgument()
    {
        IScheduleSettings<MetadataSaga, ScheduledMessage> settings =
            CreateProxy<IScheduleSettings<MetadataSaga, ScheduledMessage>>();

        Assert.Equal("name", Assert.Throws<ArgumentNullException>(() =>
            new ViciOneServiceBusStateMachine<MetadataSaga>.StateMachineSchedule<ScheduledMessage>(
                null!,
                instance => instance.ScheduleTokenId,
                settings)).ParamName);
        Assert.Equal("tokenIdExpression", Assert.Throws<ArgumentNullException>(() =>
            new ViciOneServiceBusStateMachine<MetadataSaga>.StateMachineSchedule<ScheduledMessage>(
                "Schedule",
                null!,
                settings)).ParamName);
        Assert.Equal("settings", Assert.Throws<ArgumentNullException>(() =>
            new ViciOneServiceBusStateMachine<MetadataSaga>.StateMachineSchedule<ScheduledMessage>(
                "Schedule",
                instance => instance.ScheduleTokenId,
                null!)).ParamName);

        var schedule = new ViciOneServiceBusStateMachine<MetadataSaga>.StateMachineSchedule<ScheduledMessage>(
            "Schedule",
            instance => instance.ScheduleTokenId,
            settings);

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => schedule.GetDelay(null!)).ParamName);
        Assert.Equal("instance", Assert.Throws<ArgumentNullException>(() => schedule.GetTokenId(null!)).ParamName);
        Assert.Equal("instance", Assert.Throws<ArgumentNullException>(() => schedule.SetTokenId(null!, NewId.NextGuid())).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-SCHEDULE", "runtime-metadata-delay-name-and-token-storage")]
    public void ScheduleMetadata_PreservesDelayNameEventsAndTokenStorage()
    {
        TimeSpan expectedDelay = TimeSpan.FromSeconds(17);
        IScheduleSettings<MetadataSaga, ScheduledMessage> settings =
            CreateProxy<IScheduleSettings<MetadataSaga, ScheduledMessage>>((method, _) =>
                method.Name == "get_DelayProvider"
                    ? (ScheduleDelayProvider<MetadataSaga>)(_ => expectedDelay)
                    : DefaultValue(method.ReturnType));
        var schedule = new ViciOneServiceBusStateMachine<MetadataSaga>.StateMachineSchedule<ScheduledMessage>(
            "Reminder",
            instance => instance.ScheduleTokenId,
            settings);
        var saga = new MetadataSaga();
        var received = new MessageEvent<ScheduledMessage>("Received");
        var anyReceived = new MessageEvent<ScheduledMessage>("AnyReceived");
        Guid tokenId = NewId.NextGuid();
        schedule.Received = received;
        schedule.AnyReceived = anyReceived;

        schedule.SetTokenId(saga, tokenId);

        Assert.Equal("Reminder", ((ISchedule<MetadataSaga>)schedule).Name);
        Assert.Same(received, schedule.Received);
        Assert.Same(anyReceived, schedule.AnyReceived);
        Assert.Equal(tokenId, schedule.GetTokenId(saga));
        Assert.Equal(expectedDelay, schedule.GetDelay(CreateProxy<IBehaviorContext<MetadataSaga>>()));

        schedule.SetTokenId(saga, null);
        Assert.Null(schedule.GetTokenId(saga));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "state-runtime-metadata-required-arguments")]
    public void StateMetadata_RejectsEveryMissingRequiredCollaborator()
    {
        IEventObserver<MetadataSaga> observer = CreateProxy<IEventObserver<MetadataSaga>>();

        Assert.Equal("unhandledEventCallback", Assert.Throws<ArgumentNullException>(() =>
            new ViciOneServiceBusStateMachine<MetadataSaga>.StateMachineState(null!, "Running", observer)).ParamName);
        Assert.Equal("name", Assert.Throws<ArgumentNullException>(() =>
            new ViciOneServiceBusStateMachine<MetadataSaga>.StateMachineState((_, _) => Task.CompletedTask, null!, observer)).ParamName);
        Assert.Equal("observer", Assert.Throws<ArgumentNullException>(() =>
            new ViciOneServiceBusStateMachine<MetadataSaga>.StateMachineState((_, _) => Task.CompletedTask, "Running", null!)).ParamName);

        ViciOneServiceBusStateMachine<MetadataSaga>.StateMachineState state = CreateState("Running");
        var trigger = new TriggerEvent("Trigger");
        var messageEvent = new MessageEvent<ScheduledMessage>("Message");

        Assert.Equal("visitor", Assert.Throws<ArgumentNullException>(() => state.Accept(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => state.Probe(null!)).ParamName);
        Assert.Equal("event", Assert.Throws<ArgumentNullException>(() => state.Bind(null!, null!)).ParamName);
        Assert.Equal("activity", Assert.Throws<ArgumentNullException>(() => state.Bind(trigger, null!)).ParamName);
        Assert.Equal("event", Assert.Throws<ArgumentNullException>(() => state.Ignore((IEvent)null!)).ParamName);
        Assert.Equal("event", Assert.Throws<ArgumentNullException>(() => state.Ignore<ScheduledMessage>(null!, null!)).ParamName);
        Assert.Equal("filter", Assert.Throws<ArgumentNullException>(() => state.Ignore(messageEvent, null!)).ParamName);
        Assert.Equal("subState", Assert.Throws<ArgumentNullException>(() => state.AddSubstate(null!)).ParamName);
        Assert.Equal("state", Assert.Throws<ArgumentNullException>(() => state.HasState(null!)).ParamName);
        Assert.Equal("state", Assert.Throws<ArgumentNullException>(() => state.IsStateOf(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "state-name-equality-hash-and-order")]
    public void StateMetadata_UsesOrdinalNameEqualityHashingAndOrdering()
    {
        ViciOneServiceBusStateMachine<MetadataSaga>.StateMachineState left = CreateState("Running");
        ViciOneServiceBusStateMachine<MetadataSaga>.StateMachineState equivalent = CreateState("Running");
        ViciOneServiceBusStateMachine<MetadataSaga>.StateMachineState differentCase = CreateState("running");
        ViciOneServiceBusStateMachine<MetadataSaga>.StateMachineState empty = CreateState(string.Empty);

        Assert.True(left.Equals((IState)equivalent));
        Assert.True(left.Equals((object)equivalent));
        Assert.True(left == equivalent);
        Assert.False(left != equivalent);
        Assert.Equal(left.GetHashCode(), equivalent.GetHashCode());
        Assert.Equal(0, left.CompareTo(equivalent));
        Assert.False(left.Equals((IState)differentCase));
        Assert.Equal(string.CompareOrdinal(left.Name, differentCase.Name), left.CompareTo(differentCase));
        Assert.False(left.Equals((IState?)null));
        Assert.False(empty.Equals((IState?)null));
        Assert.Equal(1, left.CompareTo(null));
        Assert.Equal("Running (State)", left.ToString());

        IState<MetadataSaga> leftInterface = left;
        IState<MetadataSaga> equivalentInterface = equivalent;
        Assert.True(leftInterface == equivalent);
        Assert.False(leftInterface != equivalent);
        Assert.True(left == equivalentInterface);
        Assert.False(left != equivalentInterface);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "state-probe-hierarchy-and-parent-unhandled-fallback")]
    public async Task StateMetadata_ProbesItsGraphAndOwnsUnhandledFallbackAcrossBothContextShapesAsync()
    {
        IEventObserver<MetadataSaga> observer = CreateProxy<IEventObserver<MetadataSaga>>();
        int parentUnhandled = 0;
        int childUnhandled = 0;
        var parent = new ViciOneServiceBusStateMachine<MetadataSaga>.StateMachineState(
            (context, state) =>
            {
                parentUnhandled++;
                return Task.FromException(new UnhandledEventException("Machine", context.Event.Name, state.Name));
            },
            "Parent",
            observer);
        var child = new ViciOneServiceBusStateMachine<MetadataSaga>.StateMachineState(
            (_, _) =>
            {
                childUnhandled++;
                return Task.CompletedTask;
            },
            "Child",
            observer,
            parent);
        var bound = new TriggerEvent("Bound");
        var ignored = new MessageEvent<ScheduledMessage>("Ignored");
        parent.Bind(bound, new ProbeActivity());
        parent.Ignore(ignored);

        var root = new RecordingProbeContext();
        parent.Probe(root);

        RecordingProbeContext stateScope = Assert.Single(root.Children, scope => scope.Key == "state");
        Assert.Equal("Parent", stateScope.Values["name"]);
        Assert.Contains(stateScope.Children, scope => scope.Key == "substates");
        Assert.Contains(stateScope.Children, scope => scope.Key == "event");
        Assert.Contains(stateScope.Children, scope => scope.Key == "event-ignored");
        Assert.True(parent.HasState(child));
        Assert.True(child.IsStateOf(parent));
        Assert.False(parent.IsStateOf(child));
        Assert.Contains(bound, parent.DeclaredEvents);
        Assert.Contains(ignored, parent.Events);
        Assert.Equal("subState", Assert.Throws<ArgumentException>(() => parent.AddSubstate(CreateState("Parent"))).ParamName);

        var untypedEvent = new TriggerEvent("UntypedMissing");
        IBehaviorContext<MetadataSaga> untypedContext = CreateProxy<IBehaviorContext<MetadataSaga>>(
            (method, _) => method.Name == "get_Event" ? untypedEvent : DefaultValue(method.ReturnType));
        await ((IState<MetadataSaga>)child).RaiseAsync(untypedContext, TestContext.Current.CancellationToken);

        var typedEvent = new MessageEvent<ScheduledMessage>("TypedMissing");
        IBehaviorContext<MetadataSaga, ScheduledMessage> typedContext =
            CreateProxy<IBehaviorContext<MetadataSaga, ScheduledMessage>>(
                (method, _) => method.Name == "get_Event" ? typedEvent : DefaultValue(method.ReturnType));
        await ((IState<MetadataSaga>)child).RaiseAsync(typedContext, TestContext.Current.CancellationToken);

        Assert.Equal(2, parentUnhandled);
        Assert.Equal(2, childUnhandled);

        var inheritedEvent = new MessageEvent<ScheduledMessage>("Inherited");
        parent.Bind(inheritedEvent, new ProbeActivity());
        IBehaviorContext<MetadataSaga, ScheduledMessage> inheritedContext =
            CreateProxy<IBehaviorContext<MetadataSaga, ScheduledMessage>>(
                (method, _) => method.Name == "get_Event" ? inheritedEvent : DefaultValue(method.ReturnType));
        await ((IState<MetadataSaga>)child).RaiseAsync(inheritedContext, TestContext.Current.CancellationToken);

        using var cancellationSource = new CancellationTokenSource();
        var cancelingEvent = new MessageEvent<ScheduledMessage>("Canceling");
        parent.Bind(cancelingEvent, new ProbeActivity(cancellationSource));
        IBehaviorContext<MetadataSaga, ScheduledMessage> cancelingContext =
            CreateProxy<IBehaviorContext<MetadataSaga, ScheduledMessage>>(
                (method, _) => method.Name == "get_Event" ? cancelingEvent : DefaultValue(method.ReturnType));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ((IState<MetadataSaga>)child).RaiseAsync(cancelingContext, cancellationSource.Token));
        Assert.True(cancellationSource.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-COMPOSITE", "status-bit-layout-and-mask-contract")]
    public void CompositeEventStatus_PreservesTheStableBitLayoutAndMaskContract()
    {
        var status = new CompositeEventStatus();

        Assert.Equal(0, status.Bits);
        Assert.Equal(new string('0', 32), status.Status);
        Assert.True(status.IsSet(0));

        status.Set(1 | int.MinValue);

        Assert.Equal(1 | int.MinValue, status.Bits);
        Assert.Equal($"1{new string('0', 30)}1", status.Status);
        Assert.True(status.IsSet(1));
        Assert.True(status.IsSet(int.MinValue));
        Assert.True(status.IsSet(1 | int.MinValue));
        Assert.False(status.IsSet(2));
        Assert.False(status.Equals((object?)null));
        Assert.False(status.Equals("not-a-status"));
        Assert.True(status.Equals((object)new CompositeEventStatus(1 | int.MinValue)));
        Assert.Equal(1 | int.MinValue, status.GetHashCode());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "saga-concurrency-mode-stable-values")]
    public void ConcurrencyMode_PreservesTheStableNumericContract()
    {
        Assert.Equal(0, (int)ConcurrencyMode.Optimistic);
        Assert.Equal(1, (int)ConcurrencyMode.Pessimistic);
        Assert.Equal(nameof(ConcurrencyMode.Optimistic), ConcurrencyMode.Optimistic.ToString());
        Assert.Equal(nameof(ConcurrencyMode.Pessimistic), ConcurrencyMode.Pessimistic.ToString());
        Assert.False(Enum.IsDefined((ConcurrencyMode)2));
    }

    private static ViciOneServiceBusStateMachine<MetadataSaga>.StateMachineState CreateState(string name) =>
        new((_, _) => Task.CompletedTask, name, CreateProxy<IEventObserver<MetadataSaga>>());

    private static IBehaviorContext<MetadataSaga, IRequestTimeoutExpired<RequestMessage>> CreateTimeoutContext(
        MetadataSaga saga,
        Guid? requestId) =>
        CreateProxy<IBehaviorContext<MetadataSaga, IRequestTimeoutExpired<RequestMessage>>>((method, _) => method.Name switch
        {
            "get_Saga" => saga,
            "get_RequestId" => requestId,
            _ => DefaultValue(method.ReturnType),
        });

    private static T CreateProxy<T>(Func<MethodInfo, object?[]?, object?>? handler = null)
        where T : class
    {
        T proxy = DispatchProxy.Create<T, DefaultDispatchProxy>();
        ((DefaultDispatchProxy)(object)proxy).Handler = handler;
        return proxy;
    }

    private static object? DefaultValue(Type type)
    {
        if (type == typeof(void))
            return null;
        if (type == typeof(Task))
            return Task.CompletedTask;

        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    private sealed class MetadataSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
        public Guid? RequestId { get; set; }
        public Guid? ScheduleTokenId { get; set; }
    }

    private sealed record RequestMessage;
    private sealed record ResponseMessage;
    private sealed record ResponseMessage2;
    private sealed record ResponseMessage3;
    private sealed record ScheduledMessage;

    private class DefaultDispatchProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?>? Handler { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            return Handler is null
                ? DefaultValue(targetMethod.ReturnType)
                : Handler(targetMethod, args);
        }
    }

    private sealed class RecordingProbeContext(string? key = null) : ProbeContext
    {
        public CancellationToken CancellationToken => default;
        public List<RecordingProbeContext> Children { get; } = [];
        public string? Key { get; } = key;
        public Dictionary<string, object?> Values { get; } = [];

        public void Add(string key, string? value) => Values[key] = value;
        public void Add(string key, object? value) => Values[key] = value;
        public void Set(object values) => throw new NotSupportedException();
        public void Set(IEnumerable<KeyValuePair<string, object?>> values) => throw new NotSupportedException();

        public ProbeContext CreateScope(string scopeKey)
        {
            var child = new RecordingProbeContext(scopeKey);
            Children.Add(child);
            return child;
        }
    }

    private sealed class ProbeActivity(CancellationTokenSource? cancellationSource = null) : IStateMachineActivity<MetadataSaga>
    {
        public void Accept(IStateMachineVisitor visitor) => visitor.Visit(this);
        public void Probe(ProbeContext context) => context.CreateScope("probe-activity");
        public Task ExecuteAsync(IBehaviorContext<MetadataSaga> context, IBehavior<MetadataSaga> next) =>
            ExecuteAsyncCore(context, next);
        public Task ExecuteAsync<T>(IBehaviorContext<MetadataSaga, T> context, IBehavior<MetadataSaga, T> next)
            where T : class => ExecuteAsyncCore(context, next);
        public Task FaultedAsync<TException>(IBehaviorExceptionContext<MetadataSaga, TException> context, IBehavior<MetadataSaga> next)
            where TException : Exception => throw new NotSupportedException();
        public Task FaultedAsync<T, TException>(
            IBehaviorExceptionContext<MetadataSaga, T, TException> context,
            IBehavior<MetadataSaga, T> next)
            where T : class
            where TException : Exception => throw new NotSupportedException();

        private Task ExecuteAsyncCore(IBehaviorContext<MetadataSaga> context, IBehavior<MetadataSaga> next)
        {
            if (cancellationSource is null)
                return next.ExecuteAsync(context);

            cancellationSource.Cancel();
            return Task.FromCanceled(cancellationSource.Token);
        }

        private Task ExecuteAsyncCore<T>(IBehaviorContext<MetadataSaga, T> context, IBehavior<MetadataSaga, T> next)
            where T : class
        {
            if (cancellationSource is null)
                return next.ExecuteAsync(context);

            cancellationSource.Cancel();
            return Task.FromCanceled(cancellationSource.Token);
        }
    }
}
