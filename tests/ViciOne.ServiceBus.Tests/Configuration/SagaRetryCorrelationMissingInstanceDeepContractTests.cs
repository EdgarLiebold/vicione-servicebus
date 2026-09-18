using System.Reflection;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class SagaRetryCorrelationMissingInstanceDeepContractTests
{
    const BindingFlags DeclaredPublic = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    static readonly NullabilityInfoContext Nullability = new();

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-RETRY-CORRELATION-MISSING", "exact-four-type-surfaces-constraints-members-and-nullability")]
    public void PublicSurfaces_ExposeExactInheritanceConstraintsMembersAndNullability()
    {
        AssertBehaviorContextRetryConfiguratorSurface();
        AssertCorrelationBuilderSurface(typeof(CorrelatedByEventCorrelationBuilder<,>), typeof(CorrelatedMessage));
        AssertCorrelationBuilderSurface(typeof(CorrelatedByFaultEventCorrelationBuilder<,>), typeof(Fault<CorrelatedMessage>));
        AssertMissingInstanceConfiguratorSurface();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-RETRY-CORRELATION-MISSING", "retry-policy-observer-null-ownership-and-lifetime-identity")]
    public void RetryConfigurator_OwnsPolicyAndObserverNullBoundariesAndObserverLifetimeIdentity()
    {
        var configurator = new BehaviorContextRetryConfigurator();

        AssertParameter("factory", () => configurator.SetRetryPolicy(null!));
        AssertParameter("observer", () => configurator.ConnectRetryObserver(null!));
        InvalidOperationException missing = Assert.Throws<InvalidOperationException>(() => configurator.GetRetryPolicy());
        Assert.Equal("A retry policy must be configured before it is retrieved.", missing.Message);

        RetryPolicyFactory factory = _ => Retry.None;
        configurator.SetRetryPolicy(factory);
        Assert.Same(factory, configurator.PolicyFactory);
        AssertParameter("factory", () => configurator.SetRetryPolicy(null!));
        Assert.Same(factory, configurator.PolicyFactory);

        IRetryObserver observer = CreateProxy<IRetryObserver>();
        ConnectHandle handle = configurator.ConnectRetryObserver(observer);
        object observable = ReadField(configurator, "_observers");
        PropertyInfo connectedProperty = observable.GetType().GetProperty("Connected", BindingFlags.Public | BindingFlags.Instance)!;
        Assert.Same(observer, Assert.Single(Assert.IsType<IRetryObserver[]>(connectedProperty.GetValue(observable))));

        handle.Disconnect();

        Assert.Empty(Assert.IsType<IRetryObserver[]>(connectedProperty.GetValue(observable)));
        handle.Disconnect();
        Assert.Empty(Assert.IsType<IRetryObserver[]>(connectedProperty.GetValue(observable)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-RETRY-CORRELATION-MISSING", "retry-filter-forwarding-result-null-and-exception-identity")]
    public void RetryConfigurator_ForwardsTheExactFilterAndPreservesResultNullAndExceptionOutcomes()
    {
        var configurator = new BehaviorContextRetryConfigurator();
        configurator.Handle<InvalidOperationException>();
        IExceptionFilter? observedFilter = null;
        IRetryPolicy expectedPolicy = Retry.None;
        configurator.SetRetryPolicy(filter =>
        {
            observedFilter = filter;
            return expectedPolicy;
        });

        IRetryPolicy actualPolicy = configurator.GetRetryPolicy();

        Assert.Same(expectedPolicy, actualPolicy);
        Assert.Same(ReadProperty(configurator, "Filter"), observedFilter);
        Assert.True(observedFilter!.Match(new InvalidOperationException()));
        Assert.False(observedFilter.Match(new ArgumentException()));

        var nullResult = new BehaviorContextRetryConfigurator();
        nullResult.SetRetryPolicy(_ => null!);
        InvalidOperationException nullFailure = Assert.Throws<InvalidOperationException>(() => nullResult.GetRetryPolicy());
        Assert.Equal("The retry policy factory returned null.", nullFailure.Message);

        var throwing = new BehaviorContextRetryConfigurator();
        var expectedFailure = new RetryFactoryException();
        throwing.SetRetryPolicy(_ => throw expectedFailure);
        RetryFactoryException actualFailure = Assert.Throws<RetryFactoryException>(() => throwing.GetRetryPolicy());
        Assert.Same(expectedFailure, actualFailure);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-RETRY-CORRELATION-MISSING", "event-fault-builder-machine-event-null-ownership")]
    public void CorrelationBuilders_RejectMissingMachineAndEventAtTheirOwnConstructors()
    {
        ISagaStateMachine<CorrelationSaga> machine = CreateProxy<ISagaStateMachine<CorrelationSaga>>();
        IEvent<CorrelatedMessage> @event = CreateProxy<IEvent<CorrelatedMessage>>();
        IEvent<Fault<CorrelatedMessage>> faultEvent = CreateProxy<IEvent<Fault<CorrelatedMessage>>>();

        AssertParameter("machine", () => new CorrelatedByEventCorrelationBuilder<CorrelationSaga, CorrelatedMessage>(null!, null!));
        AssertParameter("machine", () => new CorrelatedByEventCorrelationBuilder<CorrelationSaga, CorrelatedMessage>(null!, @event));
        AssertParameter("event", () => new CorrelatedByEventCorrelationBuilder<CorrelationSaga, CorrelatedMessage>(machine, null!));
        AssertParameter("machine", () => new CorrelatedByFaultEventCorrelationBuilder<CorrelationSaga, CorrelatedMessage>(null!, null!));
        AssertParameter("machine", () => new CorrelatedByFaultEventCorrelationBuilder<CorrelationSaga, CorrelatedMessage>(null!, faultEvent));
        AssertParameter("event", () => new CorrelatedByFaultEventCorrelationBuilder<CorrelationSaga, CorrelatedMessage>(machine, null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-RETRY-CORRELATION-MISSING", "event-fault-builder-owner-and-exact-correlation-path")]
    public async Task CorrelationBuilders_PreserveOwnersAndSelectTheExactMessageAndFaultPathsAsync()
    {
        ISagaStateMachine<CorrelationSaga> machine = CreateProxy<ISagaStateMachine<CorrelationSaga>>();
        IEvent<CorrelatedMessage> @event = CreateProxy<IEvent<CorrelatedMessage>>();
        IEvent<Fault<CorrelatedMessage>> faultEvent = CreateProxy<IEvent<Fault<CorrelatedMessage>>>();
        var directBuilder = new CorrelatedByEventCorrelationBuilder<CorrelationSaga, CorrelatedMessage>(machine, @event);
        var faultBuilder = new CorrelatedByFaultEventCorrelationBuilder<CorrelationSaga, CorrelatedMessage>(machine, faultEvent);

        var direct = Assert.IsType<MessageEventCorrelation<CorrelationSaga, CorrelatedMessage>>(directBuilder.Build());
        var fault = Assert.IsType<MessageEventCorrelation<CorrelationSaga, Fault<CorrelatedMessage>>>(faultBuilder.Build());

        Assert.Same(machine, ReadField(direct, "_machine"));
        Assert.Same(@event, direct.Event);
        Assert.Equal(typeof(CorrelatedMessage), direct.DataType);
        Assert.True(direct.ConfigureConsumeTopology);
        Assert.NotNull(direct.FilterFactory);
        var directFilter = Assert.IsType<CorrelationIdMessageFilter<CorrelatedMessage>>(direct.MessageFilter);

        Assert.Same(machine, ReadField(fault, "_machine"));
        Assert.Same(faultEvent, fault.Event);
        Assert.Equal(typeof(Fault<CorrelatedMessage>), fault.DataType);
        Assert.True(fault.ConfigureConsumeTopology);
        Assert.NotNull(fault.FilterFactory);
        var faultFilter = Assert.IsType<CorrelationIdMessageFilter<Fault<CorrelatedMessage>>>(fault.MessageFilter);

        Guid directEnvelopeId = NewId.NextGuid();
        Guid directMessageId = NewId.NextGuid();
        var directMessage = new CorrelatedMessage(directMessageId);
        ConsumeContext<CorrelatedMessage> directContext = InMemoryOutboxTestContextFactory.Create(
            directMessage,
            TestContext.Current.CancellationToken,
            correlationId: directEnvelopeId);
        var directCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var directNext = new CapturingPipe<ConsumeContext<CorrelatedMessage>>(directCompletion.Task);

        Task directResult = directFilter.SendAsync(directContext, directNext);

        Assert.Same(directCompletion.Task, directResult);
        ConsumeContext<CorrelatedMessage> forwardedDirect = Assert.IsAssignableFrom<ConsumeContext<CorrelatedMessage>>(directNext.Context);
        Assert.NotSame(directContext, forwardedDirect);
        Assert.Same(directMessage, forwardedDirect.Message);
        Assert.Equal(directMessageId, forwardedDirect.CorrelationId);
        Assert.NotEqual(directEnvelopeId, forwardedDirect.CorrelationId);

        Guid faultEnvelopeId = NewId.NextGuid();
        Guid enclosedMessageId = NewId.NextGuid();
        var enclosedMessage = new CorrelatedMessage(enclosedMessageId);
        var faultMessage = new TestFault<CorrelatedMessage>(enclosedMessage);
        ConsumeContext<Fault<CorrelatedMessage>> faultContext = InMemoryOutboxTestContextFactory.Create<Fault<CorrelatedMessage>>(
            faultMessage,
            TestContext.Current.CancellationToken,
            correlationId: faultEnvelopeId);
        var faultCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var faultNext = new CapturingPipe<ConsumeContext<Fault<CorrelatedMessage>>>(faultCompletion.Task);

        Task faultResult = faultFilter.SendAsync(faultContext, faultNext);

        Assert.Same(faultCompletion.Task, faultResult);
        ConsumeContext<Fault<CorrelatedMessage>> forwardedFault =
            Assert.IsAssignableFrom<ConsumeContext<Fault<CorrelatedMessage>>>(faultNext.Context);
        Assert.NotSame(faultContext, forwardedFault);
        Assert.Same(faultMessage, forwardedFault.Message);
        Assert.Same(enclosedMessage, forwardedFault.Message.Message);
        Assert.Equal(enclosedMessageId, forwardedFault.CorrelationId);
        Assert.NotEqual(faultEnvelopeId, forwardedFault.CorrelationId);

        directCompletion.SetResult(true);
        faultCompletion.SetResult(true);
        await Task.WhenAll(directResult, faultResult);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-RETRY-CORRELATION-MISSING", "missing-discard-and-fault-envelope-correlation")]
    public void MissingInstance_DiscardCompletesAndFaultUsesTheEnvelopeCorrelation()
    {
        var configurator = new EventMissingInstanceConfigurator<CorrelationSaga, MissingMessage>();
        ConsumeContext<MissingMessage> discardContext = CreateMissingContext(NewId.NextGuid());

        Task discarded = configurator.Discard().SendAsync(discardContext);

        Assert.Same(Task.CompletedTask, discarded);
        Assert.Same(configurator.Discard(), configurator.Discard());

        Guid envelopeCorrelationId = NewId.NextGuid();
        ConsumeContext<MissingMessage> correlatedContext = CreateMissingContext(envelopeCorrelationId);
        SagaException correlated = Assert.Throws<SagaException>(() =>
        {
            _ = configurator.Fault().SendAsync(correlatedContext);
        });
        Assert.Equal(typeof(CorrelationSaga), correlated.SagaType);
        Assert.Equal(typeof(MissingMessage), correlated.MessageType);
        Assert.Equal(envelopeCorrelationId, correlated.CorrelationId);
        Assert.NotEqual(correlatedContext.Message.MessageCorrelationId, correlated.CorrelationId);
        Assert.Contains("An existing saga instance was not found", correlated.Message, StringComparison.Ordinal);

        ConsumeContext<MissingMessage> uncorrelatedContext = InMemoryOutboxTestContextFactory.Create(
            new MissingMessage(NewId.NextGuid()),
            TestContext.Current.CancellationToken);
        SagaException uncorrelated = Assert.Throws<SagaException>(() =>
        {
            _ = configurator.Fault().SendAsync(uncorrelatedContext);
        });
        Assert.Equal(Guid.Empty, uncorrelated.CorrelationId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-RETRY-CORRELATION-MISSING", "missing-sync-async-callback-guard-ownership")]
    public void MissingInstance_RejectsBothMissingCallbacksAtItsOwnBoundary()
    {
        var configurator = new EventMissingInstanceConfigurator<CorrelationSaga, MissingMessage>();

        AssertParameter("callback", () => configurator.Execute(null!));
        AssertParameter("callback", () => configurator.ExecuteAwaited(null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-RETRY-CORRELATION-MISSING", "missing-sync-async-context-task-exception-identity")]
    public async Task MissingInstance_SyncAndAsyncPipesPreserveContextTaskAndExceptionIdentityAsync()
    {
        var configurator = new EventMissingInstanceConfigurator<CorrelationSaga, MissingMessage>();
        ConsumeContext<MissingMessage> context = CreateMissingContext(NewId.NextGuid());
        ConsumeContext<MissingMessage>? observedSync = null;
        IPipe<ConsumeContext<MissingMessage>> syncPipe = configurator.Execute(value => observedSync = value);

        Task syncResult = syncPipe.SendAsync(context);

        Assert.Same(context, observedSync);
        Assert.Same(Task.CompletedTask, syncResult);

        var expectedSyncFailure = new MissingCallbackException("sync");
        IPipe<ConsumeContext<MissingMessage>> throwingSync = configurator.Execute(_ => throw expectedSyncFailure);
        MissingCallbackException actualSyncFailure = Assert.Throws<MissingCallbackException>(() =>
        {
            _ = throwingSync.SendAsync(context);
        });
        Assert.Same(expectedSyncFailure, actualSyncFailure);

        ConsumeContext<MissingMessage>? observedAsync = null;
        var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        IPipe<ConsumeContext<MissingMessage>> asyncPipe = configurator.ExecuteAwaited(value =>
        {
            observedAsync = value;
            return pending.Task;
        });

        Task asyncResult = asyncPipe.SendAsync(context);

        Assert.Same(context, observedAsync);
        Assert.Same(pending.Task, asyncResult);
        pending.SetResult(true);
        await asyncResult;

        var expectedSynchronousAsyncFailure = new MissingCallbackException("async-synchronous");
        IPipe<ConsumeContext<MissingMessage>> synchronouslyThrowingAsync =
            configurator.ExecuteAwaited(_ => throw expectedSynchronousAsyncFailure);
        MissingCallbackException actualSynchronousAsyncFailure = Assert.Throws<MissingCallbackException>(() =>
        {
            _ = synchronouslyThrowingAsync.SendAsync(context);
        });
        Assert.Same(expectedSynchronousAsyncFailure, actualSynchronousAsyncFailure);

        var expectedTaskFailure = new MissingCallbackException("async-task");
        Task faultedTask = Task.FromException(expectedTaskFailure);
        IPipe<ConsumeContext<MissingMessage>> faultedAsync = configurator.ExecuteAwaited(_ => faultedTask);

        Task faultedResult = faultedAsync.SendAsync(context);

        Assert.Same(faultedTask, faultedResult);
        MissingCallbackException actualTaskFailure =
            await Assert.ThrowsAsync<MissingCallbackException>(() => faultedResult);
        Assert.Same(expectedTaskFailure, actualTaskFailure);
    }

    static void AssertBehaviorContextRetryConfiguratorSurface()
    {
        Type type = typeof(BehaviorContextRetryConfigurator);
        AssertPublicConcreteClass(type, typeof(ExceptionSpecification), typeof(IRetryConfigurator));
        Assert.Empty(type.GetGenericArguments());
        Assert.Empty(type.GetFields(DeclaredPublic));
        Assert.Empty(type.GetEvents(DeclaredPublic));
        Assert.Empty(type.GetNestedTypes(BindingFlags.Public));
        Assert.Empty(Assert.Single(type.GetConstructors(DeclaredPublic)).GetParameters());

        PropertyInfo property = Assert.Single(type.GetProperties(DeclaredPublic));
        Assert.Equal("PolicyFactory", property.Name);
        Assert.Equal(typeof(RetryPolicyFactory), property.PropertyType);
        Assert.True(property.GetMethod is { IsPublic: true, IsStatic: false });
        Assert.True(property.SetMethod is { IsPrivate: true, IsStatic: false });
        Assert.Equal(NullabilityState.NotNull, Nullability.Create(property).ReadState);

        MethodInfo[] methods = DeclaredOrdinaryMethods(type);
        Assert.Equal(["ConnectRetryObserver", "GetRetryPolicy", "SetRetryPolicy"],
            methods.Select(method => method.Name).OrderBy(name => name, StringComparer.Ordinal));
        AssertMethod(methods, "SetRetryPolicy", typeof(void), ("factory", typeof(RetryPolicyFactory)));
        AssertMethod(methods, "ConnectRetryObserver", typeof(ConnectHandle), ("observer", typeof(IRetryObserver)));
        AssertMethod(methods, "GetRetryPolicy", typeof(IRetryPolicy));
    }

    static void AssertCorrelationBuilderSurface(Type type, Type eventDataType)
    {
        AssertPublicConcreteClass(type, typeof(object), typeof(IEventCorrelationBuilder));
        Assert.Empty(type.GetFields(DeclaredPublic));
        Assert.Empty(type.GetProperties(DeclaredPublic));
        Assert.Empty(type.GetEvents(DeclaredPublic));
        Assert.Empty(type.GetNestedTypes(BindingFlags.Public));

        Type[] genericArguments = type.GetGenericArguments();
        Assert.Equal(2, genericArguments.Length);
        AssertGenericParameter(genericArguments[0], "TInstance", true, typeof(ISagaStateMachineInstance));
        AssertGenericParameter(genericArguments[1], "TData", true, typeof(ICorrelatedBy<Guid>));

        Type machineType = typeof(ISagaStateMachine<>).MakeGenericType(genericArguments[0]);
        Type declaredEventDataType = eventDataType.IsGenericType
            ? eventDataType.GetGenericTypeDefinition().MakeGenericType(genericArguments[1])
            : genericArguments[1];
        Type eventType = typeof(IEvent<>).MakeGenericType(declaredEventDataType);
        ConstructorInfo constructor = Assert.Single(type.GetConstructors(DeclaredPublic));
        AssertParameters(constructor.GetParameters(), ("machine", machineType), ("event", eventType));

        MethodInfo build = Assert.Single(DeclaredOrdinaryMethods(type));
        Assert.Equal("Build", build.Name);
        Assert.Equal(typeof(IEventCorrelation), build.ReturnType);
        Assert.Empty(build.GetParameters());
        Assert.Equal(NullabilityState.NotNull, Nullability.Create(build.ReturnParameter).ReadState);
    }

    static void AssertMissingInstanceConfiguratorSurface()
    {
        Type type = typeof(EventMissingInstanceConfigurator<,>);
        Type[] genericArguments = type.GetGenericArguments();
        Type expectedInterface = typeof(IMissingInstanceConfigurator<,>).MakeGenericType(genericArguments);
        AssertPublicConcreteClass(type, typeof(object), expectedInterface);
        Assert.Empty(type.GetFields(DeclaredPublic));
        Assert.Empty(type.GetProperties(DeclaredPublic));
        Assert.Empty(type.GetEvents(DeclaredPublic));
        Assert.Empty(type.GetNestedTypes(BindingFlags.Public));
        Assert.Empty(Assert.Single(type.GetConstructors(DeclaredPublic)).GetParameters());
        AssertGenericParameter(genericArguments[0], "TSaga", false, typeof(ISagaStateMachineInstance));
        AssertGenericParameter(genericArguments[1], "TMessage", true);

        Type contextType = typeof(ConsumeContext<>).MakeGenericType(genericArguments[1]);
        Type pipeType = typeof(IPipe<>).MakeGenericType(contextType);
        MethodInfo[] methods = DeclaredOrdinaryMethods(type);
        Assert.Equal(["Discard", "Execute", "ExecuteAwaited", "Fault"],
            methods.Select(method => method.Name).OrderBy(name => name, StringComparer.Ordinal));
        AssertMethod(methods, "Discard", pipeType);
        AssertMethod(methods, "Fault", pipeType);
        AssertMethod(methods, "Execute", pipeType,
            ("callback", typeof(Action<>).MakeGenericType(contextType)));
        AssertMethod(methods, "ExecuteAwaited", pipeType,
            ("callback", typeof(Func<,>).MakeGenericType(contextType, typeof(Task))));
    }

    static void AssertPublicConcreteClass(Type type, Type baseType, params Type[] directInterfaces)
    {
        Assert.True(type.IsPublic && type.IsClass && !type.IsAbstract && !type.IsSealed);
        Assert.Equal(baseType, type.BaseType);
        AssertDirectInterfaces(type, directInterfaces);
    }

    static void AssertGenericParameter(Type parameter, string name, bool referenceType, params Type[] constraints)
    {
        Assert.Equal(name, parameter.Name);
        GenericParameterAttributes expected = referenceType
            ? GenericParameterAttributes.ReferenceTypeConstraint
            : GenericParameterAttributes.None;
        Assert.Equal(expected, parameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Equal(constraints, parameter.GetGenericParameterConstraints());
    }

    static void AssertMethod(MethodInfo[] methods, string name, Type returnType,
        params (string Name, Type Type)[] parameters)
    {
        MethodInfo method = Assert.Single(methods, candidate => candidate.Name == name);
        Assert.True(method.IsPublic && !method.IsStatic && !method.IsGenericMethod);
        Assert.Equal(returnType, method.ReturnType);
        AssertParameters(method.GetParameters(), parameters);
        if (returnType != typeof(void))
            Assert.Equal(NullabilityState.NotNull, Nullability.Create(method.ReturnParameter).ReadState);
    }

    static void AssertParameters(ParameterInfo[] actual, params (string Name, Type Type)[] expected)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (var index = 0; index < expected.Length; index++)
        {
            Assert.Equal(expected[index].Name, actual[index].Name);
            Assert.Equal(expected[index].Type, actual[index].ParameterType);
            Assert.Equal(NullabilityState.NotNull, Nullability.Create(actual[index]).ReadState);
        }
    }

    static void AssertDirectInterfaces(Type type, params Type[] expected)
    {
        Type[] inherited = type.GetInterfaces().SelectMany(@interface => @interface.GetInterfaces()).Distinct().ToArray();
        Type[] actual = type.GetInterfaces()
            .Except(inherited)
            .Except(type.BaseType?.GetInterfaces() ?? [])
            .OrderBy(TypeIdentity, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expected.OrderBy(TypeIdentity, StringComparer.Ordinal), actual);
    }

    static MethodInfo[] DeclaredOrdinaryMethods(Type type) =>
        type.GetMethods(DeclaredPublic).Where(method => !method.IsSpecialName).ToArray();

    static string TypeIdentity(Type type) => type.AssemblyQualifiedName ?? type.FullName ?? type.Name;

    static void AssertParameter(string parameterName, Action action) =>
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(action).ParamName);

    static T CreateProxy<T>()
        where T : class =>
        DispatchProxy.Create<T, PassiveProxy>();

    static object ReadField(object owner, string name)
    {
        for (Type? type = owner.GetType(); type != null; type = type.BaseType)
        {
            FieldInfo? field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null)
                return field.GetValue(owner)!;
        }

        throw new MissingFieldException(owner.GetType().FullName, name);
    }

    static object ReadProperty(object owner, string name)
    {
        for (Type? type = owner.GetType(); type != null; type = type.BaseType)
        {
            PropertyInfo? property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (property != null)
                return property.GetValue(owner)!;
        }

        throw new MissingMemberException(owner.GetType().FullName, name);
    }

    static ConsumeContext<MissingMessage> CreateMissingContext(Guid correlationId) =>
        InMemoryOutboxTestContextFactory.Create(
            new MissingMessage(NewId.NextGuid()),
            TestContext.Current.CancellationToken,
            correlationId: correlationId);

    public sealed record CorrelatedMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record MissingMessage(Guid MessageCorrelationId);

    public sealed class CorrelationSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    sealed class TestFault<T>(T message) : Fault<T>
    {
        public T Message { get; } = message;

        public Guid FaultId { get; } = NewId.NextGuid();

        public Guid? FaultedMessageId => null;

        public DateTimeOffset Timestamp => DateTimeOffset.UnixEpoch;

        public ExceptionInfo[] Exceptions { get; } = [];

        public HostInfo Host => null!;

        public string[] FaultMessageTypes { get; } = [];
    }

    sealed class CapturingPipe<TContext>(Task completion) : IPipe<TContext>
        where TContext : class, PipeContext
    {
        public TContext? Context { get; private set; }

        public Task SendAsync(TContext context)
        {
            Context = context;
            return completion;
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    sealed class RetryFactoryException : Exception
    {
    }

    sealed class MissingCallbackException(string message) : Exception(message)
    {
    }

    class PassiveProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"Unexpected proxy call: {targetMethod?.Name}");
    }
}
