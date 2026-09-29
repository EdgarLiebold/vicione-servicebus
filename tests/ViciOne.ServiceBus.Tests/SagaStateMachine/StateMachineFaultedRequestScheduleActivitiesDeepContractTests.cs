using System.Reflection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineFaultedRequestScheduleActivitiesDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "t76-normal-request-invalid-declaration-fails-before-dispatch")]
    public void NormalRequest_RejectsMissingDeclarationDependenciesBeforeTheMachineRuns()
    {
        var request = NewRequest(TimeSpan.Zero, ConfiguredAddress, out _);
        var untypedFactory = new ContextMessageFactory<IBehaviorContext<Saga>, Request>(
            _ => Task.FromResult(new InitializedMessage<Request>(new Request())));
        var typedFactory = new ContextMessageFactory<IBehaviorContext<Saga, Data>, Request>(
            _ => Task.FromResult(new InitializedMessage<Request>(new Request())));

        AssertArgument("request", () => new RequestActivity<Saga, Request, Response>(null!, untypedFactory));
        AssertArgument("messageFactory", () => new RequestActivity<Saga, Request, Response>(request, null!));
        AssertArgument("request", () => new RequestActivity<Saga, Request, Response>(
            null!, _ => OverrideAddress, untypedFactory));
        AssertArgument("serviceAddressProvider", () => new RequestActivity<Saga, Request, Response>(
            request, null!, untypedFactory));
        AssertArgument("messageFactory", () => new RequestActivity<Saga, Request, Response>(
            request, _ => OverrideAddress, null!));

        AssertArgument("request", () => new RequestActivity<Saga, Data, Request, Response>(null!, typedFactory));
        AssertArgument("messageFactory", () => new RequestActivity<Saga, Data, Request, Response>(request, null!));
        AssertArgument("request", () => new RequestActivity<Saga, Data, Request, Response>(
            null!, _ => OverrideAddress, typedFactory));
        AssertArgument("serviceAddressProvider", () => new RequestActivity<Saga, Data, Request, Response>(
            request, null!, typedFactory));
        AssertArgument("messageFactory", () => new RequestActivity<Saga, Data, Request, Response>(
            request, _ => OverrideAddress, null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "t76-normal-request-awaits-send-before-persist-and-next")]
    public async Task NormalRequest_AwaitsTheExactSendBeforePersistingTheRequestIdAndContinuingAsync()
    {
        var trace = new List<string>();
        var sent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var message = new Request();
        using var cancellation = new CancellationTokenSource();
        IAdvancedSendEndpoint endpoint = Proxy<IAdvancedSendEndpoint>((method, args) =>
        {
            Assert.Equal("SendAsync", method.Name);
            Assert.Same(message, args[0]);
            Assert.Equal(cancellation.Token, args[2]);
            trace.Add("send");
            return sent.Task;
        });
        var saga = new Saga();
        var request = NewRequest(TimeSpan.Zero, ConfiguredAddress, out Guid requestId, trace);
        IBehaviorContext<Saga> context = NewContext<IBehaviorContext<Saga>>(saga,
            endpoint: endpoint,
            endpointAddress: address =>
            {
                trace.Add("endpoint");
                Assert.Equal(OverrideAddress, address);
            },
            cancellationToken: cancellation.Token,
            endpointCancellation: token => Assert.Equal(CancellationToken.None, token));
        var factory = new ContextMessageFactory<IBehaviorContext<Saga>, Request>(observed =>
        {
            Assert.Same(context, observed);
            trace.Add("factory");
            return Task.FromResult(new InitializedMessage<Request>(message));
        });
        var next = new NextBehavior(trace);
        var activity = new RequestActivity<Saga, Request, Response>(request, observed =>
        {
            Assert.Same(context, observed);
            trace.Add("provider");
            return OverrideAddress;
        }, factory);

        Task running = activity.ExecuteAsync(context, next);
        Assert.False(running.IsCompleted);
        Assert.Null(saga.RequestId);
        Assert.Empty(next.Seen);
        Assert.Equal(["provider", "factory", "generate", "endpoint", "send"], trace);

        sent.SetResult();
        await running;
        Assert.Equal(requestId, saga.RequestId);
        Assert.Same(context, Assert.Single(next.Seen));
        Assert.Equal(["provider", "factory", "generate", "endpoint", "send", "persist", "next"], trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "t76-typed-normal-request-send-failure-preserves-owner")]
    public async Task TypedNormalRequest_SendFailurePreservesThePreviousRequestAndSkipsContinuationAsync()
    {
        var failure = new InvalidOperationException("send rejected");
        var previousId = Guid.NewGuid();
        var saga = new Saga { RequestId = previousId };
        var trace = new List<string>();
        var message = new Request();
        IAdvancedSendEndpoint endpoint = Proxy<IAdvancedSendEndpoint>((method, args) =>
        {
            Assert.Equal("SendAsync", method.Name);
            Assert.Same(message, args[0]);
            trace.Add("send");
            return Task.FromException(failure);
        });
        var request = NewRequest(TimeSpan.Zero, ConfiguredAddress, out _, trace);
        IBehaviorContext<Saga, Data> context = NewContext<IBehaviorContext<Saga, Data>>(saga,
            endpoint: endpoint, endpointAddress: address => Assert.Equal(ConfiguredAddress, address));
        var factory = new ContextMessageFactory<IBehaviorContext<Saga, Data>, Request>(observed =>
        {
            Assert.Same(context, observed);
            trace.Add("factory");
            return Task.FromResult(new InitializedMessage<Request>(message));
        });
        var next = new TypedNextBehavior(trace: trace);
        var activity = new RequestActivity<Saga, Data, Request, Response>(request,
            _ => null!, factory);

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() =>
            activity.ExecuteAsync(context, next)));
        Assert.Equal(previousId, saga.RequestId);
        Assert.Empty(next.Seen);
        Assert.Equal(["factory", "generate", "send"], trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "t76-normal-request-pre-cancellation-has-no-effects")]
    public async Task NormalRequest_PreCanceledContextDoesNotResolveAddressBuildMessageOrSendAsync()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var trace = new List<string>();
        var previousId = Guid.NewGuid();
        var saga = new Saga { RequestId = previousId };
        var request = NewRequest(TimeSpan.Zero, ConfiguredAddress, out _, trace);
        IBehaviorContext<Saga> context = NewContext<IBehaviorContext<Saga>>(saga,
            cancellationToken: cancellation.Token,
            endpointAddress: _ => trace.Add("endpoint"));
        IBehaviorContext<Saga, Data> typedContext = NewContext<IBehaviorContext<Saga, Data>>(saga,
            cancellationToken: cancellation.Token,
            endpointAddress: _ => trace.Add("typed-endpoint"));
        var factory = new ContextMessageFactory<IBehaviorContext<Saga>, Request>(_ =>
        {
            trace.Add("factory");
            return Task.FromResult(new InitializedMessage<Request>(new Request()));
        });
        var typedFactory = new ContextMessageFactory<IBehaviorContext<Saga, Data>, Request>(_ =>
        {
            trace.Add("typed-factory");
            return Task.FromResult(new InitializedMessage<Request>(new Request()));
        });
        var untyped = new RequestActivity<Saga, Request, Response>(request, _ =>
        {
            trace.Add("provider");
            return OverrideAddress;
        }, factory);
        var typed = new RequestActivity<Saga, Data, Request, Response>(request, _ =>
        {
            trace.Add("typed-provider");
            return OverrideAddress;
        }, typedFactory);
        var next = new NextBehavior(trace);
        var typedNext = new TypedNextBehavior(trace: trace);

        OperationCanceledException untypedCanceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            untyped.ExecuteAsync(context, next));
        OperationCanceledException typedCanceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            typed.ExecuteAsync(typedContext, typedNext));
        Assert.Equal(cancellation.Token, untypedCanceled.CancellationToken);
        Assert.Equal(cancellation.Token, typedCanceled.CancellationToken);
        Assert.Equal(previousId, saga.RequestId);
        Assert.Empty(trace);
        Assert.Empty(next.Seen);
        Assert.Empty(typedNext.Seen);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "t76-normal-request-inflight-send-cancellation-preserves-owner")]
    public async Task NormalRequest_InFlightSendCancellationPreservesPreviousIdAndSkipsContinuationAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var sent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var trace = new List<string>();
        var previousId = Guid.NewGuid();
        var saga = new Saga { RequestId = previousId };
        IAdvancedSendEndpoint endpoint = Proxy<IAdvancedSendEndpoint>((method, args) =>
        {
            Assert.Equal("SendAsync", method.Name);
            Assert.Equal(cancellation.Token, args[2]);
            trace.Add("send");
            return sent.Task;
        });
        var request = NewRequest(TimeSpan.Zero, ConfiguredAddress, out _, trace);
        IBehaviorContext<Saga, Data> context = NewContext<IBehaviorContext<Saga, Data>>(saga,
            endpoint: endpoint, cancellationToken: cancellation.Token,
            endpointAddress: address => Assert.Equal(ConfiguredAddress, address));
        var factory = new ContextMessageFactory<IBehaviorContext<Saga, Data>, Request>(_ =>
            Task.FromResult(new InitializedMessage<Request>(new Request())));
        var next = new TypedNextBehavior(trace: trace);
        var activity = new RequestActivity<Saga, Data, Request, Response>(request, factory);

        Task running = activity.ExecuteAsync(context, next);
        Assert.False(running.IsCompleted);
        Assert.Equal(["generate", "send"], trace);
        Assert.Equal(previousId, saga.RequestId);
        cancellation.Cancel();
        sent.SetCanceled(cancellation.Token);
        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => running);
        Assert.Equal(cancellation.Token, canceled.CancellationToken);
        Assert.Equal(previousId, saga.RequestId);
        Assert.Empty(next.Seen);
        Assert.Equal(["generate", "send"], trace);
    }

    static readonly Uri InputAddress = new("loopback://localhost/input");
    static readonly Uri ConfiguredAddress = new("loopback://localhost/configured");
    static readonly Uri OverrideAddress = new("loopback://localhost/override");
    static readonly DateTimeOffset DueAt = new(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-221-faulted-request-schedule-unschedule-exact-api-nullability-async-shape")]
    public void ActivitySurface_PreservesVisibilityInterfacesConstructorsAndAsyncNaming()
    {
        Type untypedRequest = typeof(FaultedRequestActivity<Saga, BaseFault, Request, Response>);
        Type typedRequest = typeof(FaultedRequestActivity<Saga, Data, BaseFault, Request, Response>);
        Type unschedule = typeof(FaultedUnscheduleActivity<Saga>);
        Type untypedSchedule = NewFaultedSchedule(new Schedule(), NewNoticeFactory(), _ => DueAt).GetType();
        Type typedSchedule = NewTypedFaultedSchedule(new Schedule(), NewTypedNoticeFactory(), _ => DueAt).GetType();
        Assert.True(untypedRequest.IsPublic);
        Assert.True(typedRequest.IsPublic);
        Assert.True(unschedule.IsPublic);
        Assert.True(untypedSchedule.IsNotPublic);
        Assert.True(typedSchedule.IsNotPublic);
        AssertGenericContract(typeof(FaultedRequestActivity<,,,>), [
            typeof(ISagaStateMachineInstance), typeof(Exception), null, null]);
        AssertGenericContract(typeof(FaultedRequestActivity<,,,,>), [
            typeof(ISagaStateMachineInstance), null, typeof(Exception), null, null]);
        AssertGenericContract(untypedSchedule.GetGenericTypeDefinition(), [
            typeof(ISagaStateMachineInstance), typeof(Exception), null]);
        AssertGenericContract(typedSchedule.GetGenericTypeDefinition(), [
            typeof(ISagaStateMachineInstance), null, typeof(Exception), null]);
        AssertGenericContract(typeof(FaultedUnscheduleActivity<>), [typeof(ISagaStateMachineInstance)]);
        Assert.Equal(2, untypedRequest.GetConstructors().Length);
        Assert.Equal(2, typedRequest.GetConstructors().Length);
        Assert.Single(unschedule.GetConstructors());
        Assert.Single(untypedSchedule.GetConstructors());
        Assert.Single(typedSchedule.GetConstructors());
        Assert.Contains(typeof(IStateMachineActivity<Saga>), untypedSchedule.GetInterfaces());
        Assert.Contains(typeof(IStateMachineActivity<Saga, Data>), typedSchedule.GetInterfaces());

        AssertDeclaredMethods(untypedRequest, ["Accept", "Probe", "ExecuteAsync", "ExecuteAsync", "FaultedAsync", "FaultedAsync"]);
        AssertDeclaredMethods(typedRequest, ["Accept", "Probe", "ExecuteAsync", "FaultedAsync"]);
        AssertDeclaredMethods(unschedule, ["Accept", "Probe", "ExecuteAsync", "ExecuteAsync", "FaultedAsync", "FaultedAsync"]);
        AssertDeclaredMethods(untypedSchedule, ["Accept", "Probe", "ExecuteAsync", "ExecuteAsync", "FaultedAsync", "FaultedAsync"]);
        AssertDeclaredMethods(typedSchedule, ["Accept", "Probe", "ExecuteAsync", "FaultedAsync"]);
        Assert.NotNull(untypedRequest.GetMethod("Probe"));
        Assert.NotNull(typedRequest.GetMethod("Probe"));

        var nullability = new NullabilityInfoContext();
        foreach (Type type in new[] { untypedRequest, typedRequest, unschedule, untypedSchedule, typedSchedule })
        {
            foreach (ConstructorInfo constructor in type.GetConstructors())
                Assert.All(constructor.GetParameters(), parameter =>
                    Assert.Equal(NullabilityState.NotNull, nullability.Create(parameter).ReadState));
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                Assert.All(method.GetParameters(), parameter =>
                    Assert.Equal(NullabilityState.NotNull, nullability.Create(parameter).ReadState));
        }

        var request = NewRequest(TimeSpan.Zero, ConfiguredAddress, out _);
        IStateMachineActivity[] activities =
        [
            new FaultedRequestActivity<Saga, BaseFault, Request, Response>(request, NewRequestFactory()),
            new FaultedRequestActivity<Saga, Data, BaseFault, Request, Response>(request, NewTypedRequestFactory()),
            new FaultedUnscheduleActivity<Saga>(new Schedule()),
            NewFaultedSchedule(new Schedule(), NewNoticeFactory(), _ => DueAt),
            NewTypedFaultedSchedule(new Schedule(), NewTypedNoticeFactory(), _ => DueAt)
        ];
        var visited = new List<object>();
        IStateMachineVisitor visitor = Proxy<IStateMachineVisitor>((method, args) =>
        {
            Assert.Equal("Visit", method.Name);
            visited.Add(Assert.Single(args)!);
            return null;
        });
        var scopes = new List<string>();
        var sets = new List<object>();
        ProbeContext? probe = null;
        probe = Proxy<ProbeContext>((method, args) => method.Name switch
        {
            "CreateScope" => RecordScope(args),
            "Add" => null,
            "Set" => RecordSet(args),
            _ => throw new NotSupportedException(method.Name)
        });
        object RecordScope(object?[] args)
        {
            scopes.Add(Assert.IsType<string>(args[0]));
            return probe!;
        }
        object? RecordSet(object?[] args)
        {
            sets.Add(Assert.Single(args)!);
            return null;
        }
        foreach (IStateMachineActivity activity in activities)
        {
            activity.Accept(visitor);
            activity.Probe(probe);
        }
        Assert.Equal(activities, visited);
        Assert.Equal(["request", "request", "unschedule-faulted", "schedule-faulted", "schedule-faulted"], scopes);
        Assert.Equal(2, sets.Count);
        Assert.All(sets, value => Assert.Same(request.Settings, value));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-221-faulted-request-unschedule-public-contract-and-null-boundaries")]
    public async Task PublicActivities_HaveStableGenericShapeAndRejectRequiredNullsAsync()
    {
        Assert.Equal(["TSaga", "TException", "TRequest", "TResponse"],
            typeof(FaultedRequestActivity<,,,>).GetGenericArguments().Select(argument => argument.Name));
        Assert.Equal(["TInstance", "TData", "TException", "TRequest", "TResponse"],
            typeof(FaultedRequestActivity<,,,,>).GetGenericArguments().Select(argument => argument.Name));
        Assert.True(typeof(IStateMachineActivity<Saga>).IsAssignableFrom(typeof(FaultedRequestActivity<Saga, BaseFault, Request, Response>)));
        Assert.True(typeof(IStateMachineActivity<Saga, Data>).IsAssignableFrom(typeof(FaultedRequestActivity<Saga, Data, BaseFault, Request, Response>)));
        Assert.True(typeof(IStateMachineActivity<Saga>).IsAssignableFrom(typeof(FaultedUnscheduleActivity<Saga>)));
        Assert.False(typeof(FaultedUnscheduleActivity<Saga>).IsSealed);
        Assert.True(NewFaultedSchedule(new Schedule(), NewNoticeFactory(), _ => DueAt).GetType().IsNotPublic);
        Assert.True(NewTypedFaultedSchedule(new Schedule(), NewTypedNoticeFactory(), _ => DueAt).GetType().IsNotPublic);

        var request = NewRequest(TimeSpan.Zero, ConfiguredAddress, out _);
        var untypedFactory = NewRequestFactory();
        var typedFactory = NewTypedRequestFactory();
        AssertArgument("request", () => new FaultedRequestActivity<Saga, BaseFault, Request, Response>(null!, untypedFactory));
        AssertArgument("messageFactory", () => new FaultedRequestActivity<Saga, BaseFault, Request, Response>(request, null!));
        AssertArgument("request", () => new FaultedRequestActivity<Saga, BaseFault, Request, Response>(
            null!, _ => OverrideAddress, untypedFactory));
        AssertArgument("serviceAddressProvider", () => new FaultedRequestActivity<Saga, BaseFault, Request, Response>(request, null!, untypedFactory));
        AssertArgument("messageFactory", () => new FaultedRequestActivity<Saga, BaseFault, Request, Response>(
            request, _ => OverrideAddress, null!));
        AssertArgument("request", () => new FaultedRequestActivity<Saga, Data, BaseFault, Request, Response>(null!, typedFactory));
        AssertArgument("messageFactory", () => new FaultedRequestActivity<Saga, Data, BaseFault, Request, Response>(request, null!));
        AssertArgument("request", () => new FaultedRequestActivity<Saga, Data, BaseFault, Request, Response>(
            null!, _ => OverrideAddress, typedFactory));
        AssertArgument("serviceAddressProvider", () => new FaultedRequestActivity<Saga, Data, BaseFault, Request, Response>(request, null!, typedFactory));
        AssertArgument("messageFactory", () => new FaultedRequestActivity<Saga, Data, BaseFault, Request, Response>(
            request, _ => OverrideAddress, null!));
        AssertArgument("schedule", () => new FaultedUnscheduleActivity<Saga>(null!));

        var saga = new Saga();
        IBehaviorContext<Saga> success = NewContext<IBehaviorContext<Saga>>(saga);
        IBehaviorContext<Saga, Data> typedSuccess = NewContext<IBehaviorContext<Saga, Data>>(saga);
        IBehaviorExceptionContext<Saga, BaseFault> fault = NewContext<IBehaviorExceptionContext<Saga, BaseFault>>(saga);
        IBehaviorExceptionContext<Saga, Data, BaseFault> typedFault = NewContext<IBehaviorExceptionContext<Saga, Data, BaseFault>>(saga);
        var next = new NextBehavior();
        var typedNext = new TypedNextBehavior();
        var untyped = new FaultedRequestActivity<Saga, BaseFault, Request, Response>(request, untypedFactory);
        var typed = new FaultedRequestActivity<Saga, Data, BaseFault, Request, Response>(request, typedFactory);
        var unschedule = new FaultedUnscheduleActivity<Saga>(new Schedule());

        AssertArgument("visitor", () => untyped.Accept(null!));
        AssertArgument("visitor", () => typed.Accept(null!));
        AssertArgument("context", () => untyped.Probe(null!));
        AssertArgument("context", () => typed.Probe(null!));
        AssertArgument("inspector", () => unschedule.Accept(null!));
        AssertArgument("context", () => unschedule.Probe(null!));
        await AssertArgumentAsync("context", () => untyped.ExecuteAsync(null!, next));
        await AssertArgumentAsync("next", () => untyped.ExecuteAsync(success, null!));
        await AssertArgumentAsync("context", () => untyped.ExecuteAsync<Data>(null!, typedNext));
        await AssertArgumentAsync("next", () => untyped.ExecuteAsync(typedSuccess, null!));
        await AssertArgumentAsync("context", () => untyped.FaultedAsync<BaseFault>(null!, next));
        await AssertArgumentAsync("next", () => untyped.FaultedAsync(fault, null!));
        await AssertArgumentAsync("context", () => untyped.FaultedAsync<Data, BaseFault>(null!, typedNext));
        await AssertArgumentAsync("next", () => untyped.FaultedAsync(typedFault, null!));
        await AssertArgumentAsync("context", () => typed.ExecuteAsync(null!, typedNext));
        await AssertArgumentAsync("next", () => typed.ExecuteAsync(typedSuccess, null!));
        await AssertArgumentAsync("context", () => typed.FaultedAsync<BaseFault>(null!, typedNext));
        await AssertArgumentAsync("next", () => typed.FaultedAsync(typedFault, null!));
        await AssertArgumentAsync("context", () => unschedule.ExecuteAsync(null!, next));
        await AssertArgumentAsync("next", () => unschedule.ExecuteAsync(success, null!));
        await AssertArgumentAsync("context", () => unschedule.ExecuteAsync<Data>(null!, typedNext));
        await AssertArgumentAsync("next", () => unschedule.ExecuteAsync(typedSuccess, null!));
        await AssertArgumentAsync("context", () => unschedule.FaultedAsync<BaseFault>(null!, next));
        await AssertArgumentAsync("next", () => unschedule.FaultedAsync(fault, null!));
        await AssertArgumentAsync("context", () => unschedule.FaultedAsync<Data, BaseFault>(null!, typedNext));
        await AssertArgumentAsync("next", () => unschedule.FaultedAsync(typedFault, null!));
        Assert.Empty(next.Seen);
        Assert.Empty(typedNext.Seen);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-SCHEDULE", "iteration-221-faulted-schedule-internal-constructor-and-method-null-boundaries")]
    public async Task FaultedSchedules_RejectRequiredNullsBeforeSchedulerOrFactoryWorkAsync()
    {
        var schedule = new Schedule();
        var factory = NewNoticeFactory();
        var typedFactory = NewTypedNoticeFactory();
        ScheduleTimeExceptionProvider<Saga, BaseFault> time = _ => DueAt;
        ScheduleTimeExceptionProvider<Saga, Data, BaseFault> typedTime = _ => DueAt;
        AssertWrappedArgument("schedule", () => NewFaultedSchedule(null!, factory, time));
        AssertWrappedArgument("timeProvider", () => NewFaultedSchedule(schedule, factory, null!));
        AssertWrappedArgument("messageFactory", () => NewFaultedSchedule(schedule, null!, time));
        AssertWrappedArgument("schedule", () => NewTypedFaultedSchedule(null!, typedFactory, typedTime));
        AssertWrappedArgument("timeProvider", () => NewTypedFaultedSchedule(schedule, typedFactory, null!));
        AssertWrappedArgument("messageFactory", () => NewTypedFaultedSchedule(schedule, null!, typedTime));

        var saga = new Saga();
        IBehaviorContext<Saga> success = NewContext<IBehaviorContext<Saga>>(saga);
        IBehaviorContext<Saga, Data> typedSuccess = NewContext<IBehaviorContext<Saga, Data>>(saga);
        IBehaviorExceptionContext<Saga, BaseFault> fault = NewContext<IBehaviorExceptionContext<Saga, BaseFault>>(saga);
        IBehaviorExceptionContext<Saga, Data, BaseFault> typedFault =
            NewContext<IBehaviorExceptionContext<Saga, Data, BaseFault>>(saga);
        IStateMachineActivity<Saga> untyped = NewFaultedSchedule(schedule, factory, time);
        IStateMachineActivity<Saga, Data> typed = NewTypedFaultedSchedule(schedule, typedFactory, typedTime);
        var next = new NextBehavior();
        var typedNext = new TypedNextBehavior();
        AssertArgument("inspector", () => untyped.Accept(null!));
        AssertArgument("context", () => untyped.Probe(null!));
        AssertArgument("inspector", () => typed.Accept(null!));
        AssertArgument("context", () => typed.Probe(null!));
        await AssertArgumentAsync("context", () => untyped.ExecuteAsync(null!, next));
        await AssertArgumentAsync("next", () => untyped.ExecuteAsync(success, null!));
        await AssertArgumentAsync("context", () => untyped.ExecuteAsync<Data>(null!, typedNext));
        await AssertArgumentAsync("next", () => untyped.ExecuteAsync(typedSuccess, null!));
        await AssertArgumentAsync("context", () => untyped.FaultedAsync<BaseFault>(null!, next));
        await AssertArgumentAsync("next", () => untyped.FaultedAsync(fault, null!));
        await AssertArgumentAsync("context", () => untyped.FaultedAsync<Data, BaseFault>(null!, typedNext));
        await AssertArgumentAsync("next", () => untyped.FaultedAsync(typedFault, null!));
        await AssertArgumentAsync("context", () => typed.ExecuteAsync(null!, typedNext));
        await AssertArgumentAsync("next", () => typed.ExecuteAsync(typedSuccess, null!));
        await AssertArgumentAsync("context", () => typed.FaultedAsync<BaseFault>(null!, typedNext));
        await AssertArgumentAsync("next", () => typed.FaultedAsync(typedFault, null!));
        Assert.Empty(next.Seen);
        Assert.Empty(typedNext.Seen);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-221-faulted-request-schedule-unschedule-success-pass-through-task-identity")]
    public void SuccessPaths_ForwardOriginalContextAndExactContinuationTask()
    {
        var saga = new Saga();
        IBehaviorContext<Saga> context = NewContext<IBehaviorContext<Saga>>(saga);
        IBehaviorContext<Saga, Data> typedContext = NewContext<IBehaviorContext<Saga, Data>>(saga);
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var next = new NextBehavior(completion: pending.Task);
        var typedNext = new TypedNextBehavior(pending.Task);
        var request = NewRequest(TimeSpan.Zero, ConfiguredAddress, out _);
        IStateMachineActivity<Saga> untypedRequest =
            new FaultedRequestActivity<Saga, BaseFault, Request, Response>(request, NewRequestFactory());
        IStateMachineActivity<Saga, Data> typedRequest =
            new FaultedRequestActivity<Saga, Data, BaseFault, Request, Response>(request, NewTypedRequestFactory());
        IStateMachineActivity<Saga> schedule = NewFaultedSchedule(new Schedule(), NewNoticeFactory(), _ => DueAt);
        IStateMachineActivity<Saga, Data> typedSchedule =
            NewTypedFaultedSchedule(new Schedule(), NewTypedNoticeFactory(), _ => DueAt);
        IStateMachineActivity<Saga> unschedule = new FaultedUnscheduleActivity<Saga>(new Schedule());

        Assert.Same(pending.Task, untypedRequest.ExecuteAsync(context, next));
        Assert.Same(pending.Task, untypedRequest.ExecuteAsync(typedContext, typedNext));
        Assert.Same(pending.Task, typedRequest.ExecuteAsync(typedContext, typedNext));
        Assert.Same(pending.Task, schedule.ExecuteAsync(context, next));
        Assert.Same(pending.Task, schedule.ExecuteAsync(typedContext, typedNext));
        Assert.Same(pending.Task, typedSchedule.ExecuteAsync(typedContext, typedNext));
        Assert.Same(pending.Task, unschedule.ExecuteAsync(context, next));
        Assert.Same(pending.Task, unschedule.ExecuteAsync(typedContext, typedNext));
        Assert.All(next.Seen, seen => Assert.Same(context, seen));
        Assert.Equal(3, next.Seen.Count);
        Assert.All(typedNext.Seen, seen => Assert.Same(typedContext, seen));
        Assert.Equal(5, typedNext.Seen.Count);
        pending.SetResult();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "iteration-221-faulted-request-address-precedence-factory-and-send-state-order")]
    public async Task FaultedRequest_UsesProviderBeforeSettingsAndPersistsOnlyAfterAcceptedSendAsync()
    {
        var trace = new List<string>();
        var sendGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var message = new Request();
        using var cancellation = new CancellationTokenSource();
        IAdvancedSendEndpoint endpoint = Proxy<IAdvancedSendEndpoint>((method, args) =>
        {
            Assert.Equal("SendAsync", method.Name);
            Assert.Same(message, args[0]);
            Assert.Equal(3, args.Length);
            Assert.Equal(cancellation.Token, args[2]);
            trace.Add("send");
            return sendGate.Task;
        });
        var saga = new Saga();
        var request = NewRequest(TimeSpan.Zero, ConfiguredAddress, out Guid requestId, trace);
        IBehaviorExceptionContext<Saga, DerivedFault> context = NewContext<IBehaviorExceptionContext<Saga, DerivedFault>>(
            saga, endpoint: endpoint, endpointAddress: address =>
            {
                trace.Add("endpoint");
                Assert.Equal(OverrideAddress, address);
            }, cancellationToken: cancellation.Token,
            endpointCancellation: token => Assert.Equal(CancellationToken.None, token));
        var factory = new ContextMessageFactory<IBehaviorExceptionContext<Saga, BaseFault>, Request>(ctx =>
        {
            trace.Add("factory");
            Assert.Same(context, ctx);
            return Task.FromResult(new InitializedMessage<Request>(message));
        });
        var next = new NextBehavior(trace);
        var activity = new FaultedRequestActivity<Saga, BaseFault, Request, Response>(request, ctx =>
        {
            trace.Add("provider");
            Assert.Same(context, ctx);
            return OverrideAddress;
        }, factory);

        Task pending = activity.FaultedAsync(context, next);
        Assert.False(pending.IsCompleted);
        Assert.Null(saga.RequestId);
        Assert.Equal(["provider", "factory", "generate", "endpoint", "send"], trace);
        Assert.Empty(next.Seen);
        sendGate.SetResult();
        await pending;
        Assert.Equal(requestId, saga.RequestId);
        Assert.Equal(["provider", "factory", "generate", "endpoint", "send", "persist", "next"], trace);
        Assert.Same(context, Assert.Single(next.Seen));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "iteration-221-faulted-request-settings-fallback-nonmatch-failure-and-precancel")]
    public async Task FaultedRequest_FallsBackToSettingsAndNeverContinuesAfterSendOrCancellationFailureAsync()
    {
        var saga = new Saga { RequestId = Guid.NewGuid() };
        Guid originalRequestId = saga.RequestId!.Value;
        var trace = new List<string>();
        var failure = new InvalidOperationException("send failed");
        IAdvancedSendEndpoint endpoint = Proxy<IAdvancedSendEndpoint>((_, _) => Task.FromException(failure));
        var request = NewRequest(TimeSpan.Zero, ConfiguredAddress, out _, trace);
        var next = new NextBehavior(trace);
        var providerCalls = 0;
        var activity = new FaultedRequestActivity<Saga, BaseFault, Request, Response>(request,
            _ =>
            {
                providerCalls++;
                return null!;
            }, NewRequestFactory(() => trace.Add("factory")));
        IBehaviorExceptionContext<Saga, DerivedFault> matching = NewContext<IBehaviorExceptionContext<Saga, DerivedFault>>(
            saga, endpoint: endpoint, endpointAddress: address => Assert.Equal(ConfiguredAddress, address));
        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => activity.FaultedAsync(matching, next)));
        Assert.Equal(originalRequestId, saga.RequestId);
        Assert.Equal(["factory", "generate"], trace.Take(2));
        Assert.Empty(next.Seen);

        IBehaviorExceptionContext<Saga, Exception> nonmatching = NewContext<IBehaviorExceptionContext<Saga, Exception>>(saga);
        await activity.FaultedAsync(nonmatching, next);
        Assert.Same(nonmatching, Assert.Single(next.Seen));
        Assert.Equal(1, trace.Count(item => item == "factory"));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        IBehaviorExceptionContext<Saga, DerivedFault> canceled = NewContext<IBehaviorExceptionContext<Saga, DerivedFault>>(
            saga, cancellationToken: cancellation.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => activity.FaultedAsync(canceled, next));
        Assert.Equal(originalRequestId, saga.RequestId);
        Assert.Equal(1, trace.Count(item => item == "factory"));
        Assert.Equal(1, providerCalls);
        Assert.Single(next.Seen);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "iteration-221-request-timeout-missing-scheduler-rejects-before-dispatch")]
    public async Task Request_MissingTimeoutSchedulerRejectsBeforeDispatchAsync(bool faulted, bool typed)
    {
        var previousId = Guid.NewGuid();
        var saga = new Saga { RequestId = previousId };
        var trace = new List<string>();
        var request = NewRequest(TimeSpan.FromMinutes(1), ConfiguredAddress, out _, trace);
        IAdvancedSendEndpoint endpoint = Proxy<IAdvancedSendEndpoint>((_, _) =>
        {
            trace.Add("send");
            return Task.CompletedTask;
        });

        ConfigurationException error;
        if (faulted && typed)
        {
            IBehaviorExceptionContext<Saga, Data, BaseFault> context = NewContext<IBehaviorExceptionContext<Saga, Data, BaseFault>>(
                saga, endpoint: endpoint, endpointAddress: _ => trace.Add("endpoint"));
            var next = new TypedNextBehavior(trace: trace);
            var activity = new FaultedRequestActivity<Saga, Data, BaseFault, Request, Response>(request, NewTypedRequestFactory());
            error = await Assert.ThrowsAsync<ConfigurationException>(() => activity.FaultedAsync(context, next));
            Assert.Empty(next.Seen);
        }
        else if (faulted)
        {
            IBehaviorExceptionContext<Saga, BaseFault> context = NewContext<IBehaviorExceptionContext<Saga, BaseFault>>(
                saga, endpoint: endpoint, endpointAddress: _ => trace.Add("endpoint"));
            var next = new NextBehavior(trace);
            var activity = new FaultedRequestActivity<Saga, BaseFault, Request, Response>(request, NewRequestFactory());
            error = await Assert.ThrowsAsync<ConfigurationException>(() => activity.FaultedAsync(context, next));
            Assert.Empty(next.Seen);
        }
        else if (typed)
        {
            IBehaviorContext<Saga, Data> context = NewContext<IBehaviorContext<Saga, Data>>(
                saga, endpoint: endpoint, endpointAddress: _ => trace.Add("endpoint"));
            var next = new TypedNextBehavior(trace: trace);
            var factory = new ContextMessageFactory<IBehaviorContext<Saga, Data>, Request>(
                _ => Task.FromResult(new InitializedMessage<Request>(new Request())));
            var activity = new RequestActivity<Saga, Data, Request, Response>(request, factory);
            error = await Assert.ThrowsAsync<ConfigurationException>(() => activity.ExecuteAsync(context, next));
            Assert.Empty(next.Seen);
        }
        else
        {
            IBehaviorContext<Saga> context = NewContext<IBehaviorContext<Saga>>(
                saga, endpoint: endpoint, endpointAddress: _ => trace.Add("endpoint"));
            var next = new NextBehavior(trace);
            var factory = new ContextMessageFactory<IBehaviorContext<Saga>, Request>(
                _ => Task.FromResult(new InitializedMessage<Request>(new Request())));
            var activity = new RequestActivity<Saga, Request, Response>(request, factory);
            error = await Assert.ThrowsAsync<ConfigurationException>(() => activity.ExecuteAsync(context, next));
            Assert.Empty(next.Seen);
        }

        Assert.Equal(previousId, saga.RequestId);
        Assert.Empty(trace);
        Assert.Contains("request timeout", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("message scheduler", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "t119-request-timeout-scheduler-accepts-send-and-expiry")]
    public async Task Request_WithTimeoutSchedulerSendsAndSchedulesMatchingExpiryAsync()
    {
        var trace = new List<string>();
        var saga = new Saga();
        var message = new Request();
        var now = new DateTimeOffset(2041, 4, 5, 6, 7, 8, TimeSpan.Zero);
        var sendCompletedAt = now + TimeSpan.FromSeconds(90);
        var timeProvider = new FakeTimeProvider(now);
        var request = NewRequest(TimeSpan.FromMinutes(1), ConfiguredAddress, out Guid requestId, trace);
        using var cancellation = new CancellationTokenSource();
        var sendGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IAdvancedSendEndpoint endpoint = Proxy<IAdvancedSendEndpoint>((method, args) =>
        {
            Assert.Equal("SendAsync", method.Name);
            Assert.Same(message, args[0]);
            Assert.Equal(cancellation.Token, args[2]);
            trace.Add("send");
            return sendGate.Task;
        });
        MessageSchedulerContext scheduler = NewScheduler((method, args) =>
        {
            Assert.Equal("ScheduleSendAsync", method.Name);
            Assert.Equal(typeof(IRequestTimeoutExpired<Request>), Assert.Single(method.GetGenericArguments()));
            Assert.Equal(cancellation.Token, args[3]);
            var expiry = Assert.IsAssignableFrom<IRequestTimeoutExpired<Request>>(args[1]);
            Assert.Equal(requestId, expiry.RequestId);
            Assert.Equal(saga.CorrelationId, expiry.CorrelationId);
            Assert.Same(message, expiry.Message);
            Assert.Equal(sendCompletedAt, expiry.Timestamp);
            Assert.Equal(sendCompletedAt + TimeSpan.FromMinutes(1), expiry.ExpirationTime);
            Assert.Equal(sendCompletedAt + TimeSpan.FromMinutes(1), Assert.IsType<DateTimeOffset>(args[0]));
            Assert.Equal(requestId, saga.RequestId);
            trace.Add("schedule");
            ScheduledMessage<IRequestTimeoutExpired<Request>> scheduled =
                Proxy<ScheduledMessage<IRequestTimeoutExpired<Request>>>((_, _) => throw new NotSupportedException());
            return Task.FromResult(scheduled);
        });
        IBehaviorContext<Saga> context = NewContext<IBehaviorContext<Saga>>(saga,
            scheduler: scheduler, endpoint: endpoint,
            endpointAddress: address =>
            {
                Assert.Equal(ConfiguredAddress, address);
                trace.Add("endpoint");
            }, cancellationToken: cancellation.Token, timeProvider: timeProvider);
        var factory = new ContextMessageFactory<IBehaviorContext<Saga>, Request>(
            _ => Task.FromResult(new InitializedMessage<Request>(message)));
        var next = new NextBehavior(trace);
        var activity = new RequestActivity<Saga, Request, Response>(request, factory);

        Task pending = activity.ExecuteAsync(context, next);
        Assert.False(pending.IsCompleted);
        Assert.Null(saga.RequestId);
        Assert.Equal(["generate", "endpoint", "send"], trace);
        timeProvider.Advance(TimeSpan.FromSeconds(90));
        sendGate.SetResult();
        await pending;

        Assert.Equal(requestId, saga.RequestId);
        Assert.Same(context, Assert.Single(next.Seen));
        Assert.Equal(["generate", "endpoint", "send", "persist", "schedule", "next"], trace);
    }

    [Theory]
    [InlineData(ScheduleCancellationMode.Unsupported)]
    [InlineData(ScheduleCancellationMode.ProviderAssignedToken)]
    [InlineData(ScheduleCancellationMode.Unknown)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "t120-request-timeout-incompatible-scheduler-rejects-before-dispatch")]
    public async Task Request_IncompatibleSchedulerRejectsBeforeDispatchAsync(ScheduleCancellationMode mode)
    {
        var trace = new List<string>();
        var priorId = Guid.NewGuid();
        var saga = new Saga { RequestId = priorId };
        var request = NewRequest(TimeSpan.FromMinutes(1), ConfiguredAddress, out _, trace);
        MessageSchedulerContext scheduler = NewScheduler((method, _) =>
        {
            trace.Add(method.Name);
            throw new InvalidOperationException("The scheduler must not be used.");
        }, mode);
        IAdvancedSendEndpoint endpoint = Proxy<IAdvancedSendEndpoint>((method, _) =>
        {
            trace.Add(method.Name);
            throw new InvalidOperationException("The endpoint must not be used.");
        });
        IBehaviorContext<Saga> context = NewContext<IBehaviorContext<Saga>>(saga,
            scheduler: scheduler, endpoint: endpoint, endpointAddress: _ => trace.Add("endpoint"));
        var factory = new ContextMessageFactory<IBehaviorContext<Saga>, Request>(
            _ => Task.FromResult(new InitializedMessage<Request>(new Request())));
        var next = new NextBehavior(trace);

        ConfigurationException error = await Assert.ThrowsAsync<ConfigurationException>(() =>
            new RequestActivity<Saga, Request, Response>(request, factory).ExecuteAsync(context, next));

        Assert.Contains("caller-specified scheduling token", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(priorId, saga.RequestId);
        Assert.Empty(trace);
        Assert.Empty(next.Seen);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "t120-request-timeout-undeclared-scheduler-rejects-before-dispatch")]
    public async Task Request_UndeclaredSchedulerRejectsBeforeDispatchAsync()
    {
        var trace = new List<string>();
        var saga = new Saga();
        var request = NewRequest(TimeSpan.FromMinutes(1), ConfiguredAddress, out _, trace);
        MessageSchedulerContext scheduler = Proxy<MessageSchedulerContext>((method, _) =>
        {
            trace.Add(method.Name);
            throw new InvalidOperationException("The scheduler must not be used.");
        });
        IBehaviorContext<Saga> context = NewContext<IBehaviorContext<Saga>>(saga,
            scheduler: scheduler, endpointAddress: _ => trace.Add("endpoint"));
        var factory = new ContextMessageFactory<IBehaviorContext<Saga>, Request>(
            _ => Task.FromResult(new InitializedMessage<Request>(new Request())));
        var next = new NextBehavior(trace);

        await Assert.ThrowsAsync<ConfigurationException>(() =>
            new RequestActivity<Saga, Request, Response>(request, factory).ExecuteAsync(context, next));

        Assert.Null(saga.RequestId);
        Assert.Empty(trace);
        Assert.Empty(next.Seen);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "t119-request-unrepresentable-timeout-rejects-before-dispatch")]
    public async Task Request_UnrepresentableTimeoutRejectsBeforeDispatchAsync()
    {
        var trace = new List<string>();
        var previousId = Guid.NewGuid();
        var saga = new Saga { RequestId = previousId };
        var request = NewRequest(TimeSpan.MaxValue, ConfiguredAddress, out _, trace);
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2041, 4, 5, 6, 7, 8, TimeSpan.Zero));
        IAdvancedSendEndpoint endpoint = Proxy<IAdvancedSendEndpoint>((_, _) =>
        {
            trace.Add("send");
            return Task.CompletedTask;
        });
        MessageSchedulerContext scheduler = NewScheduler((_, _) =>
        {
            trace.Add("schedule");
            throw new InvalidOperationException("An invalid deadline cannot be scheduled.");
        });
        IBehaviorContext<Saga> context = NewContext<IBehaviorContext<Saga>>(saga,
            scheduler: scheduler, endpoint: endpoint, timeProvider: timeProvider,
            endpointAddress: _ => trace.Add("endpoint"));
        var factory = new ContextMessageFactory<IBehaviorContext<Saga>, Request>(
            _ => Task.FromResult(new InitializedMessage<Request>(new Request())));
        var next = new NextBehavior(trace);
        var activity = new RequestActivity<Saga, Request, Response>(request, factory);

        ConfigurationException error = await Assert.ThrowsAsync<ConfigurationException>(() => activity.ExecuteAsync(context, next));

        Assert.Contains("request timeout", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("supported date range", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(previousId, saga.RequestId);
        Assert.Empty(trace);
        Assert.Empty(next.Seen);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "t119-request-clock-advance-at-date-limit-still-schedules")]
    public async Task Request_ClockAdvancesToDateLimitDuringSendStillSchedulesAsync()
    {
        var start = DateTimeOffset.MaxValue - TimeSpan.FromSeconds(2);
        var timeProvider = new FakeTimeProvider(start);
        var trace = new List<string>();
        var saga = new Saga();
        var request = NewRequest(TimeSpan.FromSeconds(2), ConfiguredAddress, out Guid requestId, trace);
        var sendGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IAdvancedSendEndpoint endpoint = Proxy<IAdvancedSendEndpoint>((method, _) =>
        {
            Assert.Equal("SendAsync", method.Name);
            trace.Add("send");
            return sendGate.Task;
        });
        MessageSchedulerContext scheduler = NewScheduler((method, args) =>
        {
            Assert.Equal("ScheduleSendAsync", method.Name);
            var expiry = Assert.IsAssignableFrom<IRequestTimeoutExpired<Request>>(args[1]);
            Assert.Equal(requestId, expiry.RequestId);
            Assert.Equal(DateTimeOffset.MaxValue - TimeSpan.FromSeconds(1), expiry.Timestamp);
            Assert.Equal(DateTimeOffset.MaxValue, expiry.ExpirationTime);
            Assert.Equal(DateTimeOffset.MaxValue, Assert.IsType<DateTimeOffset>(args[0]));
            trace.Add("schedule");
            return Task.FromResult(Proxy<ScheduledMessage<IRequestTimeoutExpired<Request>>>(
                (_, _) => throw new NotSupportedException()));
        });
        IBehaviorContext<Saga> context = NewContext<IBehaviorContext<Saga>>(saga,
            scheduler: scheduler, endpoint: endpoint, timeProvider: timeProvider);
        var factory = new ContextMessageFactory<IBehaviorContext<Saga>, Request>(
            _ => Task.FromResult(new InitializedMessage<Request>(new Request())));
        var next = new NextBehavior(trace);
        var activity = new RequestActivity<Saga, Request, Response>(request, factory);

        Task pending = activity.ExecuteAsync(context, next);
        Assert.False(pending.IsCompleted);
        Assert.Null(saga.RequestId);
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        sendGate.SetResult();
        await pending;

        Assert.Equal(requestId, saga.RequestId);
        Assert.Same(context, Assert.Single(next.Seen));
        Assert.Equal(["generate", "send", "persist", "schedule", "next"], trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "t120-request-timeout-schedule-and-cancel-share-token")]
    public async Task RequestTimeout_ScheduleAndCancelUseTheSameRequestTokenAsync()
    {
        var trace = new List<string>();
        var saga = new Saga();
        var request = NewRequest(TimeSpan.FromMinutes(1), ConfiguredAddress, out Guid requestId, trace);
        Guid? configuredToken = null;
        var scheduleCount = 0;
        var cancelCount = 0;
        MessageSchedulerContext scheduler = NewScheduler((method, args) =>
        {
            if (method.Name == "ScheduleSendAsync")
            {
                scheduleCount++;
                Assert.Equal(4, args.Length);
                var pipe = Assert.IsAssignableFrom<IPipe<SendContext<IRequestTimeoutExpired<Request>>>>(args[2]);
                var sendContext = Proxy<SendContext<IRequestTimeoutExpired<Request>>>((member, values) => member.Name switch
                {
                    "set_ScheduledMessageId" => SetToken(values),
                    "get_ScheduledMessageId" => configuredToken,
                    _ => throw new NotSupportedException(member.Name)
                });
                return AcceptAsync(pipe, sendContext);
            }

            Assert.Equal("CancelScheduledSendAsync", method.Name);
            Assert.Equal(InputAddress, args[0]);
            Assert.Equal(requestId, args[1]);
            Assert.Equal(configuredToken, args[1]);
            cancelCount++;
            trace.Add("cancel");
            return Task.CompletedTask;
        });
        IAdvancedSendEndpoint endpoint = Proxy<IAdvancedSendEndpoint>((method, _) =>
        {
            Assert.Equal("SendAsync", method.Name);
            trace.Add("send");
            return Task.CompletedTask;
        });
        IBehaviorContext<Saga> sendContext = NewContext<IBehaviorContext<Saga>>(saga,
            scheduler: scheduler, endpoint: endpoint);
        var factory = new ContextMessageFactory<IBehaviorContext<Saga>, Request>(
            _ => Task.FromResult(new InitializedMessage<Request>(new Request())));
        var sendNext = new NextBehavior(trace);

        await new RequestActivity<Saga, Request, Response>(request, factory).ExecuteAsync(sendContext, sendNext);
        Assert.Equal(requestId, saga.RequestId);

        IBehaviorContext<Saga, Data> responseContext = NewContext<IBehaviorContext<Saga, Data>>(saga, scheduler: scheduler);
        var cancelNext = new TypedNextBehavior(trace: trace);
        await new CancelRequestTimeoutActivity<Saga, Data, Request, Response>(request, completed: true)
            .ExecuteAsync(responseContext, cancelNext);

        Assert.Equal(1, scheduleCount);
        Assert.Equal(1, cancelCount);
        Assert.Equal(requestId, configuredToken);
        Assert.Null(saga.RequestId);
        Assert.Same(sendContext, Assert.Single(sendNext.Seen));
        Assert.Same(responseContext, Assert.Single(cancelNext.Seen));
        Assert.Equal(["generate", "send", "persist", "schedule", "next", "cancel", "persist", "next"], trace);

        object? SetToken(object?[] values)
        {
            configuredToken = Assert.IsType<Guid>(values[0]);
            return null;
        }

        async Task<ScheduledMessage<IRequestTimeoutExpired<Request>>> AcceptAsync(
            IPipe<SendContext<IRequestTimeoutExpired<Request>>> pipe,
            SendContext<IRequestTimeoutExpired<Request>> outgoing)
        {
            await pipe.SendAsync(outgoing);
            trace.Add("schedule");
            return Proxy<ScheduledMessage<IRequestTimeoutExpired<Request>>>((member, _) => member.Name switch
            {
                "get_TokenId" => configuredToken,
                _ => throw new NotSupportedException(member.Name)
            });
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "iteration-221-typed-faulted-request-provider-factory-covariance-and-continuation")]
    public async Task TypedFaultedRequest_UsesTypedFaultContextAndContinuesWithOriginalIdentityAsync()
    {
        var saga = new Saga();
        var request = NewRequest(TimeSpan.Zero, ConfiguredAddress, out Guid requestId);
        var factoryCalls = 0;
        var providerCalls = 0;
        IAdvancedSendEndpoint endpoint = Proxy<IAdvancedSendEndpoint>((method, args) =>
        {
            Assert.Equal("SendAsync", method.Name);
            Assert.IsType<Request>(args[0]);
            return Task.CompletedTask;
        });
        IBehaviorExceptionContext<Saga, Data, DerivedFault> context =
            NewContext<IBehaviorExceptionContext<Saga, Data, DerivedFault>>(
                saga, endpoint: endpoint, endpointAddress: address => Assert.Equal(OverrideAddress, address));
        var factory = new ContextMessageFactory<IBehaviorExceptionContext<Saga, Data, BaseFault>, Request>(ctx =>
        {
            factoryCalls++;
            Assert.Same(context, ctx);
            return Task.FromResult(new InitializedMessage<Request>(new Request()));
        });
        var next = new TypedNextBehavior();
        var activity = new FaultedRequestActivity<Saga, Data, BaseFault, Request, Response>(request,
            ctx =>
            {
                providerCalls++;
                Assert.Same(context, ctx);
                return OverrideAddress;
            }, factory);

        await activity.FaultedAsync(context, next);
        Assert.Equal(requestId, saga.RequestId);
        Assert.Equal(1, providerCalls);
        Assert.Equal(1, factoryCalls);
        Assert.Same(context, Assert.Single(next.Seen));

        var fallbackSaga = new Saga();
        IBehaviorExceptionContext<Saga, Data, DerivedFault> fallback =
            NewContext<IBehaviorExceptionContext<Saga, Data, DerivedFault>>(
                fallbackSaga, endpoint: endpoint, endpointAddress: address => Assert.Equal(ConfiguredAddress, address));
        var fallbackNext = new TypedNextBehavior();
        var fallbackActivity = new FaultedRequestActivity<Saga, Data, BaseFault, Request, Response>(request,
            _ => null!, NewTypedRequestFactory());
        await fallbackActivity.FaultedAsync(fallback, fallbackNext);
        Assert.Equal(requestId, fallbackSaga.RequestId);
        Assert.Same(fallback, Assert.Single(fallbackNext.Seen));

        var defaultNext = new TypedNextBehavior();
        var defaultActivity = new FaultedRequestActivity<Saga, Data, BaseFault, Request, Response>(
            request, NewTypedRequestFactory());
        await defaultActivity.FaultedAsync(fallback, defaultNext);
        Assert.Equal(requestId, fallbackSaga.RequestId);
        Assert.Same(fallback, Assert.Single(defaultNext.Seen));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var canceledSaga = new Saga { RequestId = Guid.NewGuid() };
        Guid originalRequestId = canceledSaga.RequestId!.Value;
        IBehaviorExceptionContext<Saga, Data, DerivedFault> canceled =
            NewContext<IBehaviorExceptionContext<Saga, Data, DerivedFault>>(
                canceledSaga, cancellationToken: cancellation.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => activity.FaultedAsync(canceled, next));
        Assert.Equal(originalRequestId, canceledSaga.RequestId);
        Assert.Equal(1, providerCalls);
        Assert.Equal(1, factoryCalls);
        Assert.Single(next.Seen);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "iteration-221-faulted-request-convention-fallback-owned-route-and-missing-route")]
    public async Task FaultedRequest_WhenProviderAndSettingsAreAbsentUsesBusOwnedRouteOrFailsBeforeMutationAsync()
    {
        var saga = new Saga();
        var request = NewRequest(TimeSpan.Zero, null!, out Guid requestId);
        var factoryCalls = 0;
        var next = new NextBehavior();
        ISendEndpointProvider routeLessProvider = Proxy<ISendEndpointProvider>((method, _) =>
            throw new InvalidOperationException($"Unexpected endpoint access: {method.Name}"));
        IBehaviorExceptionContext<Saga, BaseFault> context = NewContext<IBehaviorExceptionContext<Saga, BaseFault>>(
            saga, sendEndpointProvider: routeLessProvider);
        var activity = new FaultedRequestActivity<Saga, BaseFault, Request, Response>(request,
            _ => null!, NewRequestFactory(() => factoryCalls++));

        await Assert.ThrowsAsync<ConfigurationException>(() => activity.FaultedAsync(context, next));
        Assert.Equal(0, factoryCalls);
        Assert.Null(saga.RequestId);
        Assert.Empty(next.Seen);

        IBusControl bus = Bus.Factory.CreateUsingInMemory(configuration => configuration.Route<Request>(OverrideAddress));
        IAdvancedSendEndpoint endpoint = Proxy<IAdvancedSendEndpoint>((method, args) =>
        {
            Assert.Equal("SendAsync", method.Name);
            Assert.IsType<Request>(args[0]);
            return Task.CompletedTask;
        });
        var routedSaga = new Saga();
        IBehaviorExceptionContext<Saga, BaseFault> routed = NewContext<IBehaviorExceptionContext<Saga, BaseFault>>(
            routedSaga, endpoint: endpoint, endpointAddress: address => Assert.Equal(OverrideAddress, address),
            sendEndpointProvider: bus);
        await activity.FaultedAsync(routed, next);
        Assert.Equal(1, factoryCalls);
        Assert.Equal(requestId, routedSaga.RequestId);
        Assert.Same(routed, Assert.Single(next.Seen));

        var defaultSaga = new Saga();
        IBehaviorExceptionContext<Saga, BaseFault> defaultContext = NewContext<IBehaviorExceptionContext<Saga, BaseFault>>(
            defaultSaga, endpoint: endpoint, endpointAddress: address => Assert.Equal(OverrideAddress, address),
            sendEndpointProvider: bus);
        var defaultNext = new NextBehavior();
        await new FaultedRequestActivity<Saga, BaseFault, Request, Response>(request, NewRequestFactory())
            .FaultedAsync(defaultContext, defaultNext);
        Assert.Equal(requestId, defaultSaga.RequestId);
        Assert.Same(defaultContext, Assert.Single(defaultNext.Seen));

        var typedSaga = new Saga();
        IBehaviorExceptionContext<Saga, Data, BaseFault> typedContext =
            NewContext<IBehaviorExceptionContext<Saga, Data, BaseFault>>(
                typedSaga, endpoint: endpoint, endpointAddress: address => Assert.Equal(OverrideAddress, address),
                sendEndpointProvider: bus);
        var typedNext = new TypedNextBehavior();
        var typedActivity = new FaultedRequestActivity<Saga, Data, BaseFault, Request, Response>(
            request, _ => null!, NewTypedRequestFactory());
        await typedActivity.FaultedAsync(typedContext, typedNext);
        Assert.Equal(requestId, typedSaga.RequestId);
        Assert.Same(typedContext, Assert.Single(typedNext.Seen));

        var typedDefaultSaga = new Saga();
        IBehaviorExceptionContext<Saga, Data, BaseFault> typedDefaultContext =
            NewContext<IBehaviorExceptionContext<Saga, Data, BaseFault>>(
                typedDefaultSaga, endpoint: endpoint, endpointAddress: address => Assert.Equal(OverrideAddress, address),
                sendEndpointProvider: bus);
        var typedDefaultNext = new TypedNextBehavior();
        await new FaultedRequestActivity<Saga, Data, BaseFault, Request, Response>(request, NewTypedRequestFactory())
            .FaultedAsync(typedDefaultContext, typedDefaultNext);
        Assert.Equal(requestId, typedDefaultSaga.RequestId);
        Assert.Same(typedDefaultContext, Assert.Single(typedDefaultNext.Seen));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-221-untyped-faulted-request-schedule-typed-event-match-and-nonmatch")]
    public async Task UntypedFaultActivities_TypedEventOverloadsMatchCovariantlyAndForwardNonmatchingFaultsAsync()
    {
        var saga = new Saga();
        var request = NewRequest(TimeSpan.Zero, ConfiguredAddress, out Guid requestId);
        var requestFactoryCalls = 0;
        var scheduleFactoryCalls = 0;
        var scheduleCalls = 0;
        Guid scheduledToken = Guid.NewGuid();
        IAdvancedSendEndpoint endpoint = Proxy<IAdvancedSendEndpoint>((method, args) =>
        {
            Assert.Equal("SendAsync", method.Name);
            Assert.IsType<Request>(args[0]);
            return Task.CompletedTask;
        });
        MessageSchedulerContext scheduler = NewScheduler((method, _) =>
        {
            Assert.Equal("ScheduleSendAsync", method.Name);
            scheduleCalls++;
            return Task.FromResult<ScheduledMessage<Notice>>(NewScheduled(scheduledToken));
        });
        IBehaviorExceptionContext<Saga, Data, DerivedFault> matching =
            NewContext<IBehaviorExceptionContext<Saga, Data, DerivedFault>>(saga, scheduler: scheduler, endpoint: endpoint);
        IBehaviorExceptionContext<Saga, Data, Exception> nonmatching =
            NewContext<IBehaviorExceptionContext<Saga, Data, Exception>>(saga);
        var requestActivity = new FaultedRequestActivity<Saga, BaseFault, Request, Response>(request,
            NewRequestFactory(() => requestFactoryCalls++));
        var scheduleActivity = NewFaultedSchedule(new Schedule(),
            new ContextMessageFactory<IBehaviorExceptionContext<Saga, BaseFault>, Notice>(_ =>
            {
                scheduleFactoryCalls++;
                return Task.FromResult(new InitializedMessage<Notice>(new Notice()));
            }), _ => DueAt);
        var requestNext = new TypedNextBehavior();
        var scheduleNext = new TypedNextBehavior();

        await requestActivity.FaultedAsync(matching, requestNext);
        await scheduleActivity.FaultedAsync(matching, scheduleNext);
        Assert.Equal(requestId, saga.RequestId);
        Assert.Equal(scheduledToken, saga.ScheduleId);
        Assert.Equal(1, requestFactoryCalls);
        Assert.Equal(1, scheduleFactoryCalls);
        Assert.Equal(1, scheduleCalls);
        Assert.Same(matching, Assert.Single(requestNext.Seen));
        Assert.Same(matching, Assert.Single(scheduleNext.Seen));

        await requestActivity.FaultedAsync(nonmatching, requestNext);
        await scheduleActivity.FaultedAsync(nonmatching, scheduleNext);
        Assert.Equal(1, requestFactoryCalls);
        Assert.Equal(1, scheduleFactoryCalls);
        Assert.Equal(1, scheduleCalls);
        Assert.Same(nonmatching, requestNext.Seen[1]);
        Assert.Same(nonmatching, scheduleNext.Seen[1]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-SCHEDULE", "iteration-221-faulted-schedule-token-replacement-order-and-own-token-exclusion")]
    public async Task FaultedSchedule_AcceptedNewTokenPrecedesOldCancellationAndContinuationAsync()
    {
        var trace = new List<string>();
        var saga = new Saga { ScheduleId = Guid.NewGuid() };
        Guid oldToken = saga.ScheduleId!.Value;
        Guid newToken = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        var notice = new Notice();
        var pipe = new NoticePipe();
        var scheduleGate = new TaskCompletionSource<ScheduledMessage<Notice>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        MessageSchedulerContext scheduler = NewScheduler((method, args) =>
        {
            if (method.Name == "ScheduleSendAsync")
            {
                trace.Add("schedule");
                Assert.Equal(DueAt, args[0]);
                Assert.Same(notice, args[1]);
                Assert.Same(pipe, args[2]);
                Assert.Equal(cancellation.Token, args[3]);
                return scheduleGate.Task;
            }
            trace.Add("cancel");
            Assert.Equal(InputAddress, args[0]);
            Assert.Equal(oldToken, args[1]);
            Assert.Equal(cancellation.Token, args[2]);
            Assert.Equal(newToken, saga.ScheduleId);
            cancelStarted.SetResult();
            return cancelGate.Task;
        });
        IBehaviorExceptionContext<Saga, DerivedFault> context = NewContext<IBehaviorExceptionContext<Saga, DerivedFault>>(
            saga, scheduler: scheduler, cancellationToken: cancellation.Token);
        var next = new NextBehavior(trace);
        IBehaviorExceptionContext<Saga, DerivedFault> expectedContext = context;
        var factory = new ContextMessageFactory<IBehaviorExceptionContext<Saga, BaseFault>, Notice>(ctx =>
        {
            Assert.Same(expectedContext, ctx);
            return Task.FromResult(new InitializedMessage<Notice>(notice, pipe));
        });
        IStateMachineActivity<Saga> activity = NewFaultedSchedule(new Schedule(), factory, _ => DueAt);

        Task pending = activity.FaultedAsync(context, next);
        Assert.Equal(oldToken, saga.ScheduleId);
        Assert.Equal(["schedule"], trace);
        scheduleGate.SetResult(NewScheduled(newToken));
        await cancelStarted.Task;
        Assert.Equal(newToken, saga.ScheduleId);
        Assert.Equal(["schedule", "cancel"], trace);
        Assert.Empty(next.Seen);
        cancelGate.SetResult();
        await pending;
        Assert.Equal(["schedule", "cancel", "next"], trace);
        Assert.Same(context, Assert.Single(next.Seen));

        trace.Clear();
        saga.ScheduleId = oldToken;
        IBehaviorExceptionContext<Saga, DerivedFault> ownDelivery = NewContext<IBehaviorExceptionContext<Saga, DerivedFault>>(
            saga, scheduler: NewScheduler((method, _) =>
            {
                trace.Add(method.Name);
                Assert.Equal("ScheduleSendAsync", method.Name);
                return Task.FromResult<ScheduledMessage<Notice>>(NewScheduled(newToken));
            }), scheduledToken: oldToken);
        expectedContext = ownDelivery;
        await activity.FaultedAsync(ownDelivery, next);
        Assert.Equal(newToken, saga.ScheduleId);
        Assert.Equal(["ScheduleSendAsync", "next"], trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-SCHEDULE", "iteration-221-faulted-schedule-typed-covariance-no-match-and-failure-atomicity")]
    public async Task TypedFaultedSchedule_MatchesDerivedFaultAndPreservesOldTokenOnFailureAsync()
    {
        var saga = new Saga { ScheduleId = Guid.NewGuid() };
        Guid oldToken = saga.ScheduleId!.Value;
        var failure = new InvalidOperationException("scheduler failed");
        var calls = 0;
        using var cancellation = new CancellationTokenSource();
        var notice = new Notice();
        var pipe = new NoticePipe();
        MessageSchedulerContext scheduler = NewScheduler((method, args) =>
        {
            Assert.Equal("ScheduleSendAsync", method.Name);
            Assert.Same(notice, args[1]);
            Assert.Same(pipe, args[2]);
            Assert.Equal(cancellation.Token, args[3]);
            calls++;
            return Task.FromException<ScheduledMessage<Notice>>(failure);
        });
        var factoryCalls = 0;
        var factory = new ContextMessageFactory<IBehaviorExceptionContext<Saga, Data, BaseFault>, Notice>(_ =>
        {
            factoryCalls++;
            return Task.FromResult(new InitializedMessage<Notice>(notice, pipe));
        });
        IStateMachineActivity<Saga, Data> activity = NewTypedFaultedSchedule(new Schedule(), factory, _ => DueAt);
        IBehaviorExceptionContext<Saga, Data, DerivedFault> matching =
            NewContext<IBehaviorExceptionContext<Saga, Data, DerivedFault>>(
                saga, scheduler: scheduler, cancellationToken: cancellation.Token);
        var next = new TypedNextBehavior();

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => activity.FaultedAsync(matching, next)));
        Assert.Equal(oldToken, saga.ScheduleId);
        Assert.Equal(1, factoryCalls);
        Assert.Equal(1, calls);
        Assert.Empty(next.Seen);

        IBehaviorExceptionContext<Saga, Data, Exception> nonmatching =
            NewContext<IBehaviorExceptionContext<Saga, Data, Exception>>(saga);
        await activity.FaultedAsync(nonmatching, next);
        Assert.Same(nonmatching, Assert.Single(next.Seen));
        Assert.Equal(1, calls);
        Assert.Equal(1, factoryCalls);

        using var preCanceled = new CancellationTokenSource();
        preCanceled.Cancel();
        IBehaviorExceptionContext<Saga, Data, DerivedFault> canceled =
            NewContext<IBehaviorExceptionContext<Saga, Data, DerivedFault>>(
                saga, scheduler: scheduler, cancellationToken: preCanceled.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => activity.FaultedAsync(canceled, next));
        Assert.Equal(1, factoryCalls);
        Assert.Equal(oldToken, saga.ScheduleId);
        Assert.Single(next.Seen);

        var successSaga = new Saga { ScheduleId = Guid.NewGuid() };
        Guid previousToken = successSaga.ScheduleId!.Value;
        Guid newToken = Guid.NewGuid();
        var trace = new List<string>();
        using var successCancellation = new CancellationTokenSource();
        MessageSchedulerContext successfulScheduler = NewScheduler((method, args) => method.Name switch
        {
            "ScheduleSendAsync" => ScheduleAsync(args),
            "CancelScheduledSendAsync" => CancelAsync(args),
            _ => throw new NotSupportedException(method.Name)
        });
        Task<ScheduledMessage<Notice>> ScheduleAsync(object?[] args)
        {
            trace.Add("schedule");
            Assert.Equal(DueAt, args[0]);
            Assert.Same(notice, args[1]);
            Assert.Same(pipe, args[2]);
            Assert.Equal(successCancellation.Token, args[3]);
            Assert.Equal(previousToken, successSaga.ScheduleId);
            return Task.FromResult<ScheduledMessage<Notice>>(NewScheduled(newToken));
        }
        Task CancelAsync(object?[] args)
        {
            trace.Add("cancel");
            Assert.Equal(InputAddress, args[0]);
            Assert.Equal(previousToken, args[1]);
            Assert.Equal(successCancellation.Token, args[2]);
            Assert.Equal(newToken, successSaga.ScheduleId);
            return Task.CompletedTask;
        }
        IBehaviorExceptionContext<Saga, Data, DerivedFault> successful =
            NewContext<IBehaviorExceptionContext<Saga, Data, DerivedFault>>(
                successSaga, scheduler: successfulScheduler, cancellationToken: successCancellation.Token);
        var successNext = new TypedNextBehavior(trace: trace);
        await activity.FaultedAsync(successful, successNext);
        Assert.Equal(newToken, successSaga.ScheduleId);
        Assert.Equal(["schedule", "cancel", "next"], trace);
        Assert.Same(successful, Assert.Single(successNext.Seen));

        successSaga.ScheduleId = previousToken;
        trace.Clear();
        IBehaviorExceptionContext<Saga, Data, DerivedFault> ownDelivery =
            NewContext<IBehaviorExceptionContext<Saga, Data, DerivedFault>>(
                successSaga, scheduler: successfulScheduler, cancellationToken: successCancellation.Token,
                scheduledToken: previousToken);
        var ownNext = new TypedNextBehavior(trace: trace);
        await activity.FaultedAsync(ownDelivery, ownNext);
        Assert.Equal(newToken, successSaga.ScheduleId);
        Assert.Equal(["schedule", "next"], trace);
        Assert.Same(ownDelivery, Assert.Single(ownNext.Seen));

        successSaga.ScheduleId = previousToken;
        trace.Clear();
        Guid foreignToken = Guid.NewGuid();
        Assert.NotEqual(previousToken, foreignToken);
        IBehaviorExceptionContext<Saga, Data, DerivedFault> foreignDelivery =
            NewContext<IBehaviorExceptionContext<Saga, Data, DerivedFault>>(
                successSaga, scheduler: successfulScheduler, cancellationToken: successCancellation.Token,
                scheduledToken: foreignToken);
        var foreignNext = new TypedNextBehavior(trace: trace);
        await activity.FaultedAsync(foreignDelivery, foreignNext);
        Assert.Equal(newToken, successSaga.ScheduleId);
        Assert.Equal(["schedule", "cancel", "next"], trace);
        Assert.Same(foreignDelivery, Assert.Single(foreignNext.Seen));
        Assert.Equal(4, factoryCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-SCHEDULE", "iteration-221-faulted-schedule-untyped-missing-scheduler-and-no-previous-cancel")]
    public async Task UntypedFaultedSchedule_RequiresSchedulerAndDoesNotCancelWithoutPreviousTokenAsync()
    {
        var saga = new Saga();
        var activity = NewFaultedSchedule(new Schedule(), NewNoticeFactory(), _ => DueAt);
        var next = new NextBehavior();
        IBehaviorExceptionContext<Saga, BaseFault> missing = NewContext<IBehaviorExceptionContext<Saga, BaseFault>>(saga);
        await Assert.ThrowsAsync<PayloadNotFoundException>(() => activity.FaultedAsync(missing, next));
        Assert.Null(saga.ScheduleId);
        Assert.Empty(next.Seen);

        Guid token = Guid.NewGuid();
        MessageSchedulerContext scheduler = NewScheduler((method, _) =>
        {
            Assert.Equal("ScheduleSendAsync", method.Name);
            return Task.FromResult<ScheduledMessage<Notice>>(NewScheduled(token));
        });
        IBehaviorExceptionContext<Saga, BaseFault> context = NewContext<IBehaviorExceptionContext<Saga, BaseFault>>(
            saga, scheduler: scheduler);
        await activity.FaultedAsync(context, next);
        Assert.Equal(token, saga.ScheduleId);
        Assert.Same(context, Assert.Single(next.Seen));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var canceledSaga = new Saga { ScheduleId = Guid.NewGuid() };
        Guid originalToken = canceledSaga.ScheduleId!.Value;
        var factoryCalls = 0;
        var schedulerCalls = 0;
        IStateMachineActivity<Saga> canceledActivity = NewFaultedSchedule(new Schedule(),
            new ContextMessageFactory<IBehaviorExceptionContext<Saga, BaseFault>, Notice>(_ =>
            {
                factoryCalls++;
                return Task.FromResult(new InitializedMessage<Notice>(new Notice()));
            }), _ => DueAt);
        IBehaviorExceptionContext<Saga, BaseFault> canceled = NewContext<IBehaviorExceptionContext<Saga, BaseFault>>(
            canceledSaga, scheduler: NewScheduler((_, _) =>
            {
                schedulerCalls++;
                return Task.CompletedTask;
            }), cancellationToken: cancellation.Token);
        var canceledNext = new NextBehavior();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledActivity.FaultedAsync(canceled, canceledNext));
        Assert.Equal(originalToken, canceledSaga.ScheduleId);
        Assert.Equal(0, factoryCalls);
        Assert.Equal(0, schedulerCalls);
        Assert.Empty(canceledNext.Seen);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-SCHEDULE", "iteration-221-faulted-schedule-cancel-failure-retains-accepted-new-token")]
    public async Task FaultedSchedule_CancelFailureDoesNotLoseNewAcceptedTokenAsync()
    {
        var saga = new Saga { ScheduleId = Guid.NewGuid() };
        Guid oldToken = saga.ScheduleId!.Value;
        Guid newToken = Guid.NewGuid();
        var failure = new InvalidOperationException("old cancellation failed");
        MessageSchedulerContext scheduler = NewScheduler((method, args) => method.Name switch
        {
            "ScheduleSendAsync" => Task.FromResult<ScheduledMessage<Notice>>(NewScheduled(newToken)),
            "CancelScheduledSendAsync" => FailCancelAsync(args),
            _ => throw new NotSupportedException(method.Name)
        });
        Task FailCancelAsync(object?[] args)
        {
            Assert.Equal(oldToken, args[1]);
            Assert.Equal(newToken, saga.ScheduleId);
            return Task.FromException(failure);
        }
        var next = new NextBehavior();
        IBehaviorExceptionContext<Saga, BaseFault> context = NewContext<IBehaviorExceptionContext<Saga, BaseFault>>(
            saga, scheduler: scheduler);
        IStateMachineActivity<Saga> activity = NewFaultedSchedule(new Schedule(), NewNoticeFactory(), _ => DueAt);

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => activity.FaultedAsync(context, next)));
        Assert.Equal(newToken, saga.ScheduleId);
        Assert.Empty(next.Seen);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-SCHEDULE", "iteration-221-faulted-unschedule-empty-own-foreign-token-and-cancel-state-order")]
    public async Task FaultedUnschedule_OnlyCancelsForeignOwnedTokenAndClearsAfterAcceptanceAsync()
    {
        var saga = new Saga();
        var schedule = new Schedule();
        var activity = new FaultedUnscheduleActivity<Saga>(schedule);
        var next = new NextBehavior();
        IBehaviorExceptionContext<Saga, BaseFault> missingScheduler = NewContext<IBehaviorExceptionContext<Saga, BaseFault>>(saga);
        await activity.FaultedAsync(missingScheduler, next);
        Assert.Null(saga.ScheduleId);
        Assert.Same(missingScheduler, Assert.Single(next.Seen));

        Guid token = Guid.NewGuid();
        saga.ScheduleId = token;
        IBehaviorExceptionContext<Saga, BaseFault> ownDelivery = NewContext<IBehaviorExceptionContext<Saga, BaseFault>>(
            saga, scheduledToken: token);
        await activity.FaultedAsync(ownDelivery, next);
        Assert.Equal(token, saga.ScheduleId);
        Assert.Equal(2, next.Seen.Count);

        var cancellation = new CancellationTokenSource();
        var cancelGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        MessageSchedulerContext scheduler = NewScheduler((method, args) =>
        {
            Assert.Equal("CancelScheduledSendAsync", method.Name);
            Assert.Equal(InputAddress, args[0]);
            Assert.Equal(token, args[1]);
            Assert.Equal(cancellation.Token, args[2]);
            Assert.Equal(token, saga.ScheduleId);
            return cancelGate.Task;
        });
        IBehaviorExceptionContext<Saga, Data, BaseFault> foreignDelivery =
            NewContext<IBehaviorExceptionContext<Saga, Data, BaseFault>>(
                saga, scheduler: scheduler, cancellationToken: cancellation.Token, scheduledToken: Guid.NewGuid());
        var typedNext = new TypedNextBehavior();
        Task pending = activity.FaultedAsync(foreignDelivery, typedNext);
        Assert.Equal(token, saga.ScheduleId);
        Assert.Empty(typedNext.Seen);
        cancelGate.SetResult();
        await pending;
        Assert.Null(saga.ScheduleId);
        Assert.Same(foreignDelivery, Assert.Single(typedNext.Seen));

        var noHeaderSaga = new Saga { ScheduleId = Guid.NewGuid() };
        Guid noHeaderToken = noHeaderSaga.ScheduleId!.Value;
        var noHeaderCancelCalls = 0;
        IBehaviorExceptionContext<Saga, BaseFault> noHeader = NewContext<IBehaviorExceptionContext<Saga, BaseFault>>(
            noHeaderSaga, scheduler: NewScheduler((method, args) =>
            {
                Assert.Equal("CancelScheduledSendAsync", method.Name);
                Assert.Equal(InputAddress, args[0]);
                Assert.Equal(noHeaderToken, args[1]);
                Assert.Equal(noHeaderToken, noHeaderSaga.ScheduleId);
                noHeaderCancelCalls++;
                return Task.CompletedTask;
            }));
        var noHeaderNext = new NextBehavior();
        await activity.FaultedAsync(noHeader, noHeaderNext);
        Assert.Equal(1, noHeaderCancelCalls);
        Assert.Null(noHeaderSaga.ScheduleId);
        Assert.Same(noHeader, Assert.Single(noHeaderNext.Seen));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-SCHEDULE", "iteration-221-faulted-unschedule-cancel-failure-and-independent-concurrency")]
    public async Task FaultedUnschedule_CancelFailureKeepsTokenAndIndependentSagasDoNotCrossTalkAsync()
    {
        var activity = new FaultedUnscheduleActivity<Saga>(new Schedule());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var canceledSaga = new Saga { ScheduleId = Guid.NewGuid() };
        Guid canceledToken = canceledSaga.ScheduleId!.Value;
        var canceledSchedulerCalls = 0;
        var canceledNext = new NextBehavior();
        IBehaviorExceptionContext<Saga, BaseFault> canceledContext = NewContext<IBehaviorExceptionContext<Saga, BaseFault>>(
            canceledSaga, cancellationToken: cancellation.Token, scheduler: NewScheduler((_, _) =>
            {
                canceledSchedulerCalls++;
                return Task.CompletedTask;
            }));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => activity.FaultedAsync(canceledContext, canceledNext));
        Assert.Equal(canceledToken, canceledSaga.ScheduleId);
        Assert.Equal(0, canceledSchedulerCalls);
        Assert.Empty(canceledNext.Seen);

        var failedSaga = new Saga { ScheduleId = Guid.NewGuid() };
        Guid failedToken = failedSaga.ScheduleId!.Value;
        var failure = new InvalidOperationException("cancel failed");
        var failedNext = new NextBehavior();
        IBehaviorExceptionContext<Saga, BaseFault> failedContext = NewContext<IBehaviorExceptionContext<Saga, BaseFault>>(
            failedSaga, scheduler: NewScheduler((_, _) => Task.FromException(failure)));
        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => activity.FaultedAsync(failedContext, failedNext)));
        Assert.Equal(failedToken, failedSaga.ScheduleId);
        Assert.Empty(failedNext.Seen);

        var sagas = Enumerable.Range(0, 24).Select(_ => new Saga { ScheduleId = Guid.NewGuid() }).ToArray();
        var canceled = new List<Guid>();
        var gate = new object();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task[] operations = sagas.Select(saga => Task.Run(async () =>
        {
            await start.Task;
            IBehaviorExceptionContext<Saga, BaseFault> context = NewContext<IBehaviorExceptionContext<Saga, BaseFault>>(
                saga, scheduler: NewScheduler((method, args) =>
                {
                    Assert.Equal("CancelScheduledSendAsync", method.Name);
                    lock (gate)
                        canceled.Add((Guid)args[1]!);
                    return Task.CompletedTask;
                }));
            await activity.FaultedAsync(context, new NextBehavior());
        }, TestContext.Current.CancellationToken)).ToArray();
        start.SetResult();
        await Task.WhenAll(operations);
        Assert.All(sagas, saga => Assert.Null(saga.ScheduleId));
        Assert.Equal(24, canceled.Count);
        Assert.Equal(24, canceled.Distinct().Count());
    }

    static IRequest<Saga, Request, Response> NewRequest(TimeSpan timeout, Uri address, out Guid requestId, List<string>? trace = null)
    {
        requestId = Guid.NewGuid();
        Guid id = requestId;
        IRequestSettings<Saga, Request, Response> settings = Proxy<IRequestSettings<Saga, Request, Response>>((method, _) =>
            method.Name switch
            {
                "get_ServiceAddress" => address,
                "get_Timeout" => timeout,
                "get_ClearRequestIdOnFaulted" => false,
                _ => throw new NotSupportedException(method.Name)
            });
        return Proxy<IRequest<Saga, Request, Response>>((method, args) => method.Name switch
        {
            "get_Settings" => settings,
            "GenerateRequestId" => Generate(),
            "GetRequestId" => Assert.IsType<Saga>(args[0]).RequestId,
            "SetRequestId" => Persist(args),
            _ => throw new NotSupportedException(method.Name)
        });

        Guid Generate()
        {
            trace?.Add("generate");
            return id;
        }

        object? Persist(object?[] args)
        {
            trace?.Add("persist");
            Assert.IsType<Saga>(args[0]).RequestId = (Guid?)args[1];
            return null;
        }
    }

    static ContextMessageFactory<IBehaviorExceptionContext<Saga, BaseFault>, Request> NewRequestFactory(Action? action = null) =>
        new(_ =>
        {
            action?.Invoke();
            return Task.FromResult(new InitializedMessage<Request>(new Request()));
        });

    static ContextMessageFactory<IBehaviorExceptionContext<Saga, Data, BaseFault>, Request> NewTypedRequestFactory() =>
        new(_ => Task.FromResult(new InitializedMessage<Request>(new Request())));

    static ContextMessageFactory<IBehaviorExceptionContext<Saga, BaseFault>, Notice> NewNoticeFactory() =>
        new(_ => Task.FromResult(new InitializedMessage<Notice>(new Notice())));

    static ContextMessageFactory<IBehaviorExceptionContext<Saga, Data, BaseFault>, Notice> NewTypedNoticeFactory(Action? action = null) =>
        new(_ =>
        {
            action?.Invoke();
            return Task.FromResult(new InitializedMessage<Notice>(new Notice()));
        });

    static IStateMachineActivity<Saga> NewFaultedSchedule(Schedule schedule,
        ContextMessageFactory<IBehaviorExceptionContext<Saga, BaseFault>, Notice> factory,
        ScheduleTimeExceptionProvider<Saga, BaseFault> timeProvider)
    {
        Type definition = typeof(ScheduleActivity<Saga, Notice>).Assembly.GetType(
            "ViciOne.ServiceBus.SagaStateMachine.FaultedScheduleActivity`3", throwOnError: true)!;
        Type type = definition.MakeGenericType(typeof(Saga), typeof(BaseFault), typeof(Notice));
        return Assert.IsAssignableFrom<IStateMachineActivity<Saga>>(Activator.CreateInstance(type, schedule, timeProvider, factory));
    }

    static IStateMachineActivity<Saga, Data> NewTypedFaultedSchedule(Schedule schedule,
        ContextMessageFactory<IBehaviorExceptionContext<Saga, Data, BaseFault>, Notice> factory,
        ScheduleTimeExceptionProvider<Saga, Data, BaseFault> timeProvider)
    {
        Type definition = typeof(ScheduleActivity<Saga, Notice>).Assembly.GetType(
            "ViciOne.ServiceBus.SagaStateMachine.FaultedScheduleActivity`4", throwOnError: true)!;
        Type type = definition.MakeGenericType(typeof(Saga), typeof(Data), typeof(BaseFault), typeof(Notice));
        return Assert.IsAssignableFrom<IStateMachineActivity<Saga, Data>>(Activator.CreateInstance(type, schedule, timeProvider, factory));
    }

    static ScheduledMessage<Notice> NewScheduled(Guid token) =>
        Proxy<ScheduledMessage<Notice>>((method, _) => method.Name switch
        {
            "get_TokenId" => token,
            "get_DueAt" => DueAt,
            "get_Destination" => InputAddress,
            "get_Payload" => new Notice(),
            _ => throw new NotSupportedException(method.Name)
        });

    public interface CapableMessageSchedulerContext : MessageSchedulerContext, IScheduleCancellationCapability
    {
    }

    static MessageSchedulerContext NewScheduler(Func<MethodInfo, object?[], object?> handler,
        ScheduleCancellationMode mode = ScheduleCancellationMode.CallerSpecifiedToken) =>
        Proxy<CapableMessageSchedulerContext>((method, args) =>
            method.Name == "get_CancellationMode" ? mode : handler(method, args));

    static T NewContext<T>(Saga saga, MessageSchedulerContext? scheduler = null,
        IAdvancedSendEndpoint? endpoint = null, Action<Uri>? endpointAddress = null,
        CancellationToken? cancellationToken = null, Guid? scheduledToken = null,
        ISendEndpointProvider? sendEndpointProvider = null,
        Action<CancellationToken>? endpointCancellation = null,
        TimeProvider? timeProvider = null) where T : class
    {
        Headers headers = Proxy<Headers>((method, args) => method.Name switch
        {
            "Get" => scheduledToken,
            _ => throw new NotSupportedException(method.Name)
        });
        ReceiveContext receive = Proxy<ReceiveContext>((method, _) => method.Name switch
        {
            "get_InputAddress" => InputAddress,
            "get_SendEndpointProvider" => sendEndpointProvider,
            _ => throw new NotSupportedException(method.Name)
        });
        return Proxy<T>((method, args) => method.Name switch
        {
            "get_Saga" => saga,
            "get_CancellationToken" => cancellationToken ?? TestContext.Current.CancellationToken,
            "get_Headers" => headers,
            "get_ReceiveContext" => receive,
            "TryGetPayload" => SetPayload(method, args, scheduler, timeProvider),
            "GetSendEndpointAsync" => GetEndpointAsync(args, endpoint, endpointAddress, endpointCancellation),
            _ => throw new NotSupportedException(method.Name)
        });
    }

    static bool SetPayload(MethodInfo method, object?[] args, MessageSchedulerContext? scheduler, TimeProvider? timeProvider)
    {
        if (scheduler is not null && method.GetGenericArguments()[0] == typeof(MessageSchedulerContext))
        {
            args[0] = scheduler;
            return true;
        }
        if (timeProvider is not null && method.GetGenericArguments()[0] == typeof(TimeProvider))
        {
            args[0] = timeProvider;
            return true;
        }
        args[0] = null;
        return false;
    }

    static Task<ISendEndpoint> GetEndpointAsync(object?[] args, IAdvancedSendEndpoint? endpoint, Action<Uri>? endpointAddress,
        Action<CancellationToken>? endpointCancellation)
    {
        endpointAddress?.Invoke(Assert.IsType<Uri>(args[0]));
        endpointCancellation?.Invoke(Assert.IsType<CancellationToken>(args[1]));
        return Task.FromResult<ISendEndpoint>(Assert.IsAssignableFrom<ISendEndpoint>(endpoint));
    }

    static T Proxy<T>(Func<MethodInfo, object?[], object?> handler) where T : class
    {
        T value = DispatchProxy.Create<T, CallProxy>();
        ((CallProxy)(object)value).Handler = handler;
        return value;
    }

    static void AssertArgument(string name, Action action) =>
        Assert.Equal(name, Assert.Throws<ArgumentNullException>(action).ParamName);

    static void AssertWrappedArgument(string name, Action action)
    {
        TargetInvocationException invocation = Assert.Throws<TargetInvocationException>(action);
        Assert.Equal(name, Assert.IsType<ArgumentNullException>(invocation.InnerException).ParamName);
    }

    static void AssertDeclaredMethods(Type type, string[] expected)
    {
        MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.Equal(expected.Order(StringComparer.Ordinal), methods.Select(method => method.Name).Order(StringComparer.Ordinal));
        foreach (MethodInfo method in methods.Where(method => method.ReturnType == typeof(Task)))
            Assert.EndsWith("Async", method.Name, StringComparison.Ordinal);
    }

    static void AssertGenericContract(Type type, Type?[] requiredBaseConstraints)
    {
        Type[] arguments = type.GetGenericArguments();
        Assert.Equal(requiredBaseConstraints.Length, arguments.Length);
        for (var index = 0; index < arguments.Length; index++)
        {
            Type argument = arguments[index];
            if (requiredBaseConstraints[index] != typeof(Exception))
                Assert.True((argument.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0);
            if (requiredBaseConstraints[index] is { } constraint)
                Assert.Contains(constraint, argument.GetGenericParameterConstraints());
        }
    }

    static async Task AssertArgumentAsync(string name, Func<Task> action) =>
        Assert.Equal(name, (await Assert.ThrowsAsync<ArgumentNullException>(action)).ParamName);

    public class CallProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[], object?> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Handler(Assert.IsAssignableFrom<MethodInfo>(targetMethod), args ?? []);
    }

    public sealed class Saga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } = Guid.NewGuid();
        public Guid? RequestId { get; set; }
        public Guid? ScheduleId { get; set; }
    }

    public sealed record Data;
    public sealed record Request;
    public sealed record Response;
    public sealed record Notice;
    public class BaseFault : Exception;
    public sealed class DerivedFault : BaseFault;

    public sealed class Schedule : ISchedule<Saga, Notice>
    {
        public string Name => "notice";
        public IEvent<Notice> Received { get; set; } = null!;
        public IEvent<Notice> AnyReceived { get; set; } = null!;
        public TimeSpan GetDelay(IBehaviorContext<Saga> context) => TimeSpan.Zero;
        public Guid? GetTokenId(Saga instance) => instance.ScheduleId;
        public void SetTokenId(Saga instance, Guid? tokenId) => instance.ScheduleId = tokenId;
    }

    sealed class NoticePipe : IPipe<SendContext<Notice>>
    {
        public Task SendAsync(SendContext<Notice> context) => Task.CompletedTask;
        public void Probe(ProbeContext context) { }
    }

    sealed class NextBehavior(List<string>? trace = null, Task? completion = null) : IBehavior<Saga>
    {
        public List<object> Seen { get; } = [];
        public Task ExecuteAsync(IBehaviorContext<Saga> context) => RecordAsync(context);
        public Task ExecuteAsync<T>(IBehaviorContext<Saga, T> context) where T : class => RecordAsync(context);
        public Task FaultedAsync<TException>(IBehaviorExceptionContext<Saga, TException> context)
            where TException : Exception => RecordAsync(context);
        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<Saga, T, TException> context)
            where T : class where TException : Exception => RecordAsync(context);
        public void Accept(IStateMachineVisitor visitor) { }
        public void Probe(ProbeContext context) { }
        Task RecordAsync(object context)
        {
            trace?.Add("next");
            Seen.Add(context);
            return completion ?? Task.CompletedTask;
        }
    }

    sealed class TypedNextBehavior(Task? completion = null, List<string>? trace = null) : IBehavior<Saga, Data>
    {
        public List<object> Seen { get; } = [];
        public Task ExecuteAsync(IBehaviorContext<Saga, Data> context) => RecordAsync(context);
        public Task FaultedAsync<TException>(IBehaviorExceptionContext<Saga, Data, TException> context)
            where TException : Exception => RecordAsync(context);
        public void Accept(IStateMachineVisitor visitor) { }
        public void Probe(ProbeContext context) { }
        Task RecordAsync(object context)
        {
            trace?.Add("next");
            Seen.Add(context);
            return completion ?? Task.CompletedTask;
        }
    }
}
