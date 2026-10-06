using System.Collections.Concurrent;
using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachinePublishSendRespondActivitiesDeepContractTests
{
    static readonly Uri Destination = new("queue:activity-fault");

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-221-publish-send-respond-public-contract")]
    public void PublicSurface_HasExactShapesConstraintsNullabilityAndAsyncNames()
    {
        AssertSurface(typeof(PublishActivity<,>), typeof(IStateMachineActivity<>), 6,
            ["TSaga", "TMessage"]);
        AssertSurface(typeof(PublishActivity<,,>), typeof(IStateMachineActivity<,>), 4,
            ["TSaga", "TMessage", "T"]);
        foreach (Type faulted in new[] { typeof(FaultedPublishActivity<,,>), typeof(FaultedRespondActivity<,,>),
                     typeof(FaultedSendActivity<,,>) })
            AssertSurface(faulted, typeof(IStateMachineActivity<>), 6, ["TSaga", "TException", "TMessage"]);
        foreach (Type faulted in new[] { typeof(FaultedPublishActivity<,,,>), typeof(FaultedRespondActivity<,,,>),
                     typeof(FaultedSendActivity<,,,>) })
            AssertSurface(faulted, typeof(IStateMachineActivity<,>), 4,
                ["TSaga", "TData", "TException", "TMessage"]);

        AssertConstructor(typeof(PublishActivity<TestSaga, Output>),
            (typeof(ContextMessageFactory<IBehaviorContext<TestSaga>, Output>), "messageFactory"));
        AssertConstructor(typeof(PublishActivity<TestSaga, Input, Output>),
            (typeof(ContextMessageFactory<IBehaviorContext<TestSaga, Input>, Output>), "messageFactory"));
        AssertConstructor(typeof(FaultedPublishActivity<TestSaga, BaseFault, Output>),
            (typeof(ContextMessageFactory<IBehaviorExceptionContext<TestSaga, BaseFault>, Output>), "messageFactory"));
        AssertConstructor(typeof(FaultedRespondActivity<TestSaga, BaseFault, Output>),
            (typeof(ContextMessageFactory<IBehaviorExceptionContext<TestSaga, BaseFault>, Output>), "messageFactory"));
        AssertConstructor(typeof(FaultedSendActivity<TestSaga, BaseFault, Output>),
            (typeof(DestinationAddressProvider<TestSaga>), "destinationAddressProvider"),
            (typeof(ContextMessageFactory<IBehaviorExceptionContext<TestSaga, BaseFault>, Output>), "messageFactory"));
        AssertConstructor(typeof(FaultedPublishActivity<TestSaga, Input, BaseFault, Output>),
            (typeof(ContextMessageFactory<IBehaviorExceptionContext<TestSaga, Input, BaseFault>, Output>), "messageFactory"));
        AssertConstructor(typeof(FaultedRespondActivity<TestSaga, Input, BaseFault, Output>),
            (typeof(ContextMessageFactory<IBehaviorExceptionContext<TestSaga, Input, BaseFault>, Output>), "messageFactory"));
        AssertConstructor(typeof(FaultedSendActivity<TestSaga, Input, BaseFault, Output>),
            (typeof(DestinationAddressProvider<TestSaga, Input>), "destinationAddressProvider"),
            (typeof(ContextMessageFactory<IBehaviorExceptionContext<TestSaga, Input, BaseFault>, Output>), "messageFactory"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-221-publish-send-respond-required-boundaries")]
    public async Task ConstructorsAndLifecycleMethods_RejectNullBeforeAnyEffectsAsync()
    {
        var output = new Output();
        var effects = 0;
        var normalFactory = Factory<IBehaviorContext<TestSaga>>(_ => { effects++; return output; });
        var typedNormalFactory = Factory<IBehaviorContext<TestSaga, Input>>(_ => { effects++; return output; });
        var faultFactory = Factory<IBehaviorExceptionContext<TestSaga, BaseFault>>(_ => { effects++; return output; });
        var typedFaultFactory = Factory<IBehaviorExceptionContext<TestSaga, Input, BaseFault>>(_ => { effects++; return output; });
        DestinationAddressProvider<TestSaga> address = _ => { effects++; return Destination; };
        DestinationAddressProvider<TestSaga, Input> typedAddress = _ => { effects++; return Destination; };

        AssertArgument("messageFactory", () => new PublishActivity<TestSaga, Output>(null!));
        AssertArgument("messageFactory", () => new PublishActivity<TestSaga, Input, Output>(null!));
        AssertArgument("messageFactory", () => new FaultedPublishActivity<TestSaga, BaseFault, Output>(null!));
        AssertArgument("messageFactory", () => new FaultedPublishActivity<TestSaga, Input, BaseFault, Output>(null!));
        AssertArgument("messageFactory", () => new FaultedRespondActivity<TestSaga, BaseFault, Output>(null!));
        AssertArgument("messageFactory", () => new FaultedRespondActivity<TestSaga, Input, BaseFault, Output>(null!));
        AssertArgument("destinationAddressProvider", () => new FaultedSendActivity<TestSaga, BaseFault, Output>(null!, faultFactory));
        AssertArgument("messageFactory", () => new FaultedSendActivity<TestSaga, BaseFault, Output>(address, null!));
        AssertArgument("destinationAddressProvider", () => new FaultedSendActivity<TestSaga, Input, BaseFault, Output>(null!, typedFaultFactory));
        AssertArgument("messageFactory", () => new FaultedSendActivity<TestSaga, Input, BaseFault, Output>(typedAddress, null!));

        IStateMachineActivity<TestSaga>[] untyped =
        [
            new PublishActivity<TestSaga, Output>(normalFactory),
            new FaultedPublishActivity<TestSaga, BaseFault, Output>(faultFactory),
            new FaultedRespondActivity<TestSaga, BaseFault, Output>(faultFactory),
            new FaultedSendActivity<TestSaga, BaseFault, Output>(address, faultFactory),
        ];
        IStateMachineActivity<TestSaga, Input>[] typed =
        [
            new PublishActivity<TestSaga, Input, Output>(typedNormalFactory),
            new FaultedPublishActivity<TestSaga, Input, BaseFault, Output>(typedFaultFactory),
            new FaultedRespondActivity<TestSaga, Input, BaseFault, Output>(typedFaultFactory),
            new FaultedSendActivity<TestSaga, Input, BaseFault, Output>(typedAddress, typedFaultFactory),
        ];
        IBehaviorContext<TestSaga> normal = Context<IBehaviorContext<TestSaga>>();
        IBehaviorContext<TestSaga, Input> message = Context<IBehaviorContext<TestSaga, Input>>();
        IBehaviorExceptionContext<TestSaga, BaseFault> fault = Context<IBehaviorExceptionContext<TestSaga, BaseFault>>();
        IBehaviorExceptionContext<TestSaga, Input, BaseFault> typedFault =
            Context<IBehaviorExceptionContext<TestSaga, Input, BaseFault>>();
        var next = new Next();
        var typedNext = new TypedNext();

        foreach (IStateMachineActivity<TestSaga> activity in untyped)
        {
            AssertArgument("inspector", () => activity.Accept(null!));
            AssertArgument("context", () => activity.Probe(null!));
            await AssertArgumentAsync("context", () => activity.ExecuteAsync(null!, next));
            await AssertArgumentAsync("next", () => activity.ExecuteAsync(normal, null!));
            await AssertArgumentAsync("context", () => activity.ExecuteAsync<Input>(null!, typedNext));
            await AssertArgumentAsync("next", () => activity.ExecuteAsync(message, null!));
            await AssertArgumentAsync("context", () => activity.FaultedAsync<BaseFault>(null!, next));
            await AssertArgumentAsync("next", () => activity.FaultedAsync(fault, null!));
            await AssertArgumentAsync("context", () => activity.FaultedAsync<Input, BaseFault>(null!, typedNext));
            await AssertArgumentAsync("next", () => activity.FaultedAsync(typedFault, null!));
        }
        foreach (IStateMachineActivity<TestSaga, Input> activity in typed)
        {
            AssertArgument("inspector", () => activity.Accept(null!));
            AssertArgument("context", () => activity.Probe(null!));
            await AssertArgumentAsync("context", () => activity.ExecuteAsync(null!, typedNext));
            await AssertArgumentAsync("next", () => activity.ExecuteAsync(message, null!));
            await AssertArgumentAsync("context", () => activity.FaultedAsync<BaseFault>(null!, typedNext));
            await AssertArgumentAsync("next", () => activity.FaultedAsync(typedFault, null!));
        }

        var visited = new List<IStateMachineActivity>();
        IStateMachineVisitor visitor = Inspection<IStateMachineVisitor>((method, args) =>
        {
            Assert.Equal("Visit", method.Name);
            visited.Add(Assert.IsAssignableFrom<IStateMachineActivity>(Assert.Single(args)));
            return null;
        });
        var scopes = new List<string>();
        ProbeContext? scope = null;
        scope = Inspection<ProbeContext>((method, args) =>
        {
            Assert.Equal("CreateScope", method.Name);
            scopes.Add(Assert.IsType<string>(Assert.Single(args)));
            return scope;
        });
        IStateMachineActivity[] all = [.. untyped, .. typed];
        foreach (IStateMachineActivity activity in all)
        {
            activity.Accept(visitor);
            activity.Probe(scope!);
        }
        Assert.Equal(all, visited);
        Assert.Equal(["publish", "publish-faulted", "respond-faulted", "send-faulted",
            "publish", "publish-faulted", "respond-faulted", "send-faulted"], scopes);
        Assert.Equal(0, effects);
        Assert.Equal(0, next.Calls);
        Assert.Equal(0, typedNext.Calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "iteration-221-publish-payload-pipe-token-order")]
    public async Task Publish_UsesExactFactoryContextPayloadPipeAndTokenBeforeContinuationAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var recorder = new TransportRecorder(cancellation.Token);
        IBehaviorContext<TestSaga> context = Context<IBehaviorContext<TestSaga>>(recorder);
        IBehaviorContext<TestSaga, Input> messageContext = Context<IBehaviorContext<TestSaga, Input>>(recorder);
        var output = new Output();
        var initialized = new InitializedMessage<Output>(output);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        recorder.TransportTask = gate.Task;
        var factoryCalls = 0;
        var factory = new ContextMessageFactory<IBehaviorContext<TestSaga>, Output>(observed =>
        {
            factoryCalls++;
            Assert.Same(context, observed);
            recorder.Events.Enqueue("factory");
            return Task.FromResult(initialized);
        });
        var next = new Next(execute: observed =>
        {
            Assert.Same(context, observed);
            recorder.Events.Enqueue("next");
            return Task.CompletedTask;
        });
        Task pending = new PublishActivity<TestSaga, Output>(factory).ExecuteAsync(context, next);
        Assert.Equal(["factory", "PublishAsync"], recorder.Events);
        Assert.False(pending.IsCompleted);
        Assert.Equal(0, next.Calls);
        Assert.Same(output, recorder.Payload);
        Assert.Same(initialized.Pipe, recorder.Pipe);
        Assert.Equal(cancellation.Token, recorder.TransportToken);
        gate.SetResult();
        await pending;
        Assert.Equal(["factory", "PublishAsync", "next"], recorder.Events);
        Assert.Equal(1, factoryCalls);
        Assert.Equal(1, next.Calls);

        recorder.Events.Clear();
        recorder.TransportTask = Task.CompletedTask;
        var genericFactoryCalls = 0;
        var genericFactory = Factory<IBehaviorContext<TestSaga>>(observed =>
        {
            genericFactoryCalls++;
            Assert.Same(messageContext, observed);
            return output;
        });
        var typedNext = new TypedNext();
        await new PublishActivity<TestSaga, Output>(genericFactory).ExecuteAsync(messageContext, typedNext);
        Assert.Equal(1, genericFactoryCalls);
        Assert.Equal(1, typedNext.Calls);
        Assert.Same(messageContext, typedNext.LastContext);
        Assert.Equal(["PublishAsync"], recorder.Events);

        recorder.Events.Clear();
        var typedFactory = Factory<IBehaviorContext<TestSaga, Input>>(observed =>
        {
            Assert.Same(messageContext, observed);
            recorder.Events.Enqueue("typed-factory");
            return output;
        });
        await new PublishActivity<TestSaga, Input, Output>(typedFactory).ExecuteAsync(messageContext, typedNext);
        Assert.Equal(["typed-factory", "PublishAsync"], recorder.Events);
        Assert.Equal(2, typedNext.Calls);
        Assert.Same(output, recorder.Payload);
        Assert.Equal(cancellation.Token, recorder.TransportToken);

        recorder.Events.Clear();
        await new PublishActivity<TestSaga, Output>(factory).FaultedAsync(
            Context<IBehaviorExceptionContext<TestSaga, BaseFault>>(recorder), next);
        Assert.Empty(recorder.Events);
        Assert.Equal(1, next.FaultCalls);

        var typedFault = Context<IBehaviorExceptionContext<TestSaga, Input, BaseFault>>(recorder);
        await new PublishActivity<TestSaga, Output>(factory).FaultedAsync(typedFault, typedNext);
        await new PublishActivity<TestSaga, Input, Output>(typedFactory).FaultedAsync(typedFault, typedNext);
        Assert.Empty(recorder.Events);
        Assert.Equal(2, typedNext.FaultCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RECOVERY", "iteration-221-fault-publish-send-respond-covariance-dispatch")]
    public async Task FaultedActivities_MatchCovariantExceptionAndSkipNonmatchingFaultsAsync()
    {
        foreach (Operation operation in Enum.GetValues<Operation>())
        {
            var recorder = new TransportRecorder();
            var output = new Output();
            var calls = 0;
            var next = new Next(recorder: recorder);
            var typedNext = new TypedNext(recorder);
            var untypedFactory = Factory<IBehaviorExceptionContext<TestSaga, BaseFault>>(observed =>
            {
                calls++;
                Assert.NotNull(observed);
                recorder.Events.Enqueue("factory");
                return output;
            });
            var typedFactory = Factory<IBehaviorExceptionContext<TestSaga, Input, BaseFault>>(observed =>
            {
                calls++;
                Assert.NotNull(observed);
                recorder.Events.Enqueue("factory");
                return output;
            });
            IStateMachineActivity<TestSaga> untyped = UntypedFaultActivity(operation, recorder, untypedFactory);
            IStateMachineActivity<TestSaga, Input> typed = TypedFaultActivity(operation, recorder, typedFactory);
            IBehaviorExceptionContext<TestSaga, DerivedFault> fault =
                Context<IBehaviorExceptionContext<TestSaga, DerivedFault>>(recorder);
            IBehaviorExceptionContext<TestSaga, Input, DerivedFault> dataFault =
                Context<IBehaviorExceptionContext<TestSaga, Input, DerivedFault>>(recorder);

            await untyped.ExecuteAsync(Context<IBehaviorContext<TestSaga>>(recorder), next);
            await untyped.ExecuteAsync(Context<IBehaviorContext<TestSaga, Input>>(recorder), typedNext);
            await typed.ExecuteAsync(Context<IBehaviorContext<TestSaga, Input>>(recorder), typedNext);
            Assert.Equal(0, calls);
            Assert.Equal(0, recorder.TransportCalls);
            Assert.Equal(0, recorder.EndpointRequests);

            await untyped.FaultedAsync(fault, next);
            Assert.Equal(1, calls);
            Assert.Equal(1, next.FaultCalls);
            Assert.Same(fault, next.LastContext);
            Assert.Equal(ExpectedEvents(operation), recorder.Events);
            Assert.Same(output, recorder.Payload);
            Assert.NotNull(recorder.Pipe);
            Assert.Equal(1, recorder.TransportCalls);
            recorder.Events.Clear();

            await untyped.FaultedAsync(dataFault, typedNext);
            Assert.Equal(2, calls);
            Assert.Equal(1, typedNext.FaultCalls);
            Assert.Same(dataFault, typedNext.LastContext);
            Assert.Equal(ExpectedEvents(operation), recorder.Events);
            recorder.Events.Clear();

            await typed.FaultedAsync(dataFault, typedNext);
            Assert.Equal(3, calls);
            Assert.Equal(2, typedNext.FaultCalls);
            Assert.Equal(ExpectedEvents(operation), recorder.Events);
            recorder.Events.Clear();

            await untyped.FaultedAsync(Context<IBehaviorExceptionContext<TestSaga, OtherFault>>(recorder), next);
            await untyped.FaultedAsync(Context<IBehaviorExceptionContext<TestSaga, Input, OtherFault>>(recorder), typedNext);
            await typed.FaultedAsync(Context<IBehaviorExceptionContext<TestSaga, Input, OtherFault>>(recorder), typedNext);
            Assert.Equal(3, calls);
            Assert.Equal(3, recorder.TransportCalls);
            Assert.Equal(["next-fault", "next-fault", "next-fault"], recorder.Events);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RECOVERY", "iteration-221-send-address-endpoint-factory-cardinality")]
    public async Task FaultedSend_ResolvesAddressThenEndpointThenFactoryExactlyOncePerMatchAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var recorder = new TransportRecorder(cancellation.Token);
        var context = Context<IBehaviorExceptionContext<TestSaga, DerivedFault>>(recorder);
        var output = new Output();
        var initialized = new InitializedMessage<Output>(output);
        var factory = new ContextMessageFactory<IBehaviorExceptionContext<TestSaga, BaseFault>, Output>(observed =>
        {
            Assert.Same(context, observed);
            recorder.Events.Enqueue("factory");
            return Task.FromResult(initialized);
        });
        var addressCalls = 0;
        var activity = new FaultedSendActivity<TestSaga, BaseFault, Output>(observed =>
        {
            addressCalls++;
            Assert.Same(context, observed);
            recorder.Events.Enqueue("address");
            return Destination;
        }, factory);
        var next = new Next(recorder: recorder);

        await activity.FaultedAsync(context, next);
        Assert.Equal(["address", "GetSendEndpointAsync", "factory", "SendAsync", "next-fault"], recorder.Events);
        Assert.Equal(1, addressCalls);
        Assert.Equal(1, recorder.EndpointRequests);
        Assert.Equal(1, recorder.TransportCalls);
        Assert.Equal(Destination, recorder.Address);
        Assert.Equal(cancellation.Token, recorder.EndpointToken);
        Assert.Equal(cancellation.Token, recorder.TransportToken);
        Assert.Same(output, recorder.Payload);
        Assert.Same(initialized.Pipe, recorder.Pipe);
        Assert.Equal(1, next.FaultCalls);

        Uri typedDestination = new("queue:typed-fault-elsewhere");
        var typedRecorder = new TransportRecorder(cancellation.Token);
        var typedContext = Context<IBehaviorExceptionContext<TestSaga, Input, DerivedFault>>(typedRecorder);
        var typedMessage = new Output();
        var typedInitialized = new InitializedMessage<Output>(typedMessage);
        var typedFactory = new ContextMessageFactory<IBehaviorExceptionContext<TestSaga, Input, BaseFault>, Output>(observed =>
        {
            Assert.Same(typedContext, observed);
            typedRecorder.Events.Enqueue("factory");
            return Task.FromResult(typedInitialized);
        });
        var typedAddressCalls = 0;
        var typedActivity = new FaultedSendActivity<TestSaga, Input, BaseFault, Output>(observed =>
        {
            typedAddressCalls++;
            Assert.Same(typedContext, observed);
            typedRecorder.Events.Enqueue("address");
            return typedDestination;
        }, typedFactory);
        var typedNext = new TypedNext(typedRecorder);

        await typedActivity.FaultedAsync(typedContext, typedNext);
        Assert.Equal(["address", "GetSendEndpointAsync", "factory", "SendAsync", "next-fault"], typedRecorder.Events);
        Assert.Equal(1, typedAddressCalls);
        Assert.Equal(1, typedRecorder.EndpointRequests);
        Assert.Equal(1, typedRecorder.TransportCalls);
        Assert.Equal(typedDestination, typedRecorder.Address);
        Assert.NotEqual(Destination, typedRecorder.Address);
        Assert.NotEqual(CancellationToken.None, cancellation.Token);
        Assert.Equal(cancellation.Token, typedRecorder.EndpointToken);
        Assert.Equal(cancellation.Token, typedRecorder.TransportToken);
        Assert.Same(typedMessage, typedRecorder.Payload);
        Assert.Same(typedInitialized.Pipe, typedRecorder.Pipe);
        Assert.Equal(1, typedNext.FaultCalls);
        Assert.Same(typedContext, typedNext.LastContext);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RECOVERY", "iteration-221-dispatch-failure-cancellation-continuation-order")]
    public async Task FailureAndCancellation_PreventContinuationAndUnstartedSideEffectsAsync()
    {
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        var canceledRecorder = new TransportRecorder(canceled.Token);
        var fault = Context<IBehaviorExceptionContext<TestSaga, BaseFault>>(canceledRecorder);
        var normal = Context<IBehaviorContext<TestSaga>>(canceledRecorder);
        var factoryCalls = 0;
        var factory = Factory<IBehaviorExceptionContext<TestSaga, BaseFault>>(_ =>
        {
            factoryCalls++;
            return new Output();
        });
        var publishFactory = Factory<IBehaviorContext<TestSaga>>(_ =>
        {
            factoryCalls++;
            return new Output();
        });
        var addressCalls = 0;
        var send = new FaultedSendActivity<TestSaga, BaseFault, Output>(_ =>
        {
            addressCalls++;
            return Destination;
        }, factory);
        var next = new Next();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => send.FaultedAsync(fault, next));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new FaultedPublishActivity<TestSaga, BaseFault, Output>(factory).FaultedAsync(fault, next));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new FaultedRespondActivity<TestSaga, BaseFault, Output>(factory).FaultedAsync(fault, next));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new PublishActivity<TestSaga, Output>(publishFactory).ExecuteAsync(normal, next));
        Assert.Equal(0, addressCalls);
        Assert.Equal(0, factoryCalls);
        Assert.Equal(0, canceledRecorder.EndpointRequests);
        Assert.Equal(0, canceledRecorder.TransportCalls);
        Assert.Equal(0, next.Calls);

        var failure = new InvalidOperationException("transport failure");
        var recorder = new TransportRecorder { TransportTask = Task.FromException(failure) };
        var activeFault = Context<IBehaviorExceptionContext<TestSaga, BaseFault>>(recorder);
        var activeNormal = Context<IBehaviorContext<TestSaga>>(recorder);
        var activeFactory = Factory<IBehaviorExceptionContext<TestSaga, BaseFault>>(_ => new Output());
        foreach (Operation operation in Enum.GetValues<Operation>())
        {
            InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                UntypedFaultActivity(operation, recorder, activeFactory).FaultedAsync(activeFault, next));
            Assert.Same(failure, actual);
        }
        InvalidOperationException normalFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new PublishActivity<TestSaga, Output>(Factory<IBehaviorContext<TestSaga>>(_ => new Output()))
                .ExecuteAsync(activeNormal, next));
        Assert.Same(failure, normalFailure);
        Assert.Equal(0, next.Calls);

        var factoryFailure = new InvalidOperationException("factory failure");
        var brokenFactory = new ContextMessageFactory<IBehaviorExceptionContext<TestSaga, BaseFault>, Output>(
            _ => Task.FromException<InitializedMessage<Output>>(factoryFailure));
        var factoryActual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new FaultedPublishActivity<TestSaga, BaseFault, Output>(brokenFactory).FaultedAsync(activeFault, next));
        Assert.Same(factoryFailure, factoryActual);
        Assert.Equal(0, next.Calls);

        var endpointFailure = new InvalidOperationException("endpoint failure");
        var endpointRecorder = new TransportRecorder
        {
            EndpointTask = Task.FromException<ISendEndpoint>(endpointFailure),
        };
        var endpointFault = Context<IBehaviorExceptionContext<TestSaga, BaseFault>>(endpointRecorder);
        var endpointFactoryCalls = 0;
        var endpointFactory = Factory<IBehaviorExceptionContext<TestSaga, BaseFault>>(_ =>
        {
            endpointFactoryCalls++;
            return new Output();
        });
        InvalidOperationException endpointActual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new FaultedSendActivity<TestSaga, BaseFault, Output>(_ => Destination, endpointFactory)
                .FaultedAsync(endpointFault, next));
        Assert.Same(endpointFailure, endpointActual);
        Assert.Equal(1, endpointRecorder.EndpointRequests);
        Assert.Equal(0, endpointFactoryCalls);
        Assert.Equal(0, endpointRecorder.TransportCalls);
        Assert.Equal(0, next.Calls);

        using var transportCancellation = new CancellationTokenSource();
        transportCancellation.Cancel();
        var transportRecorder = new TransportRecorder
        {
            TransportTask = Task.FromCanceled(transportCancellation.Token),
        };
        var transportFault = Context<IBehaviorExceptionContext<TestSaga, BaseFault>>(transportRecorder);
        OperationCanceledException canceledTransport = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new FaultedRespondActivity<TestSaga, BaseFault, Output>(activeFactory)
                .FaultedAsync(transportFault, next));
        Assert.Equal(transportCancellation.Token, canceledTransport.CancellationToken);
        Assert.Equal(0, next.Calls);

        var typedCanceledRecorder = new TransportRecorder(canceled.Token);
        var typedCanceledFault = Context<IBehaviorExceptionContext<TestSaga, Input, BaseFault>>(typedCanceledRecorder);
        var typedCanceledNormal = Context<IBehaviorContext<TestSaga, Input>>(typedCanceledRecorder);
        var typedFactoryCalls = 0;
        var typedAddressCalls = 0;
        var typedFactory = Factory<IBehaviorExceptionContext<TestSaga, Input, BaseFault>>(_ =>
        {
            typedFactoryCalls++;
            return new Output();
        });
        var typedPublishFactory = Factory<IBehaviorContext<TestSaga, Input>>(_ =>
        {
            typedFactoryCalls++;
            return new Output();
        });
        var typedNext = new TypedNext();
        foreach (Operation operation in Enum.GetValues<Operation>())
        {
            IStateMachineActivity<TestSaga, Input> typed = operation == Operation.Send
                ? new FaultedSendActivity<TestSaga, Input, BaseFault, Output>(_ =>
                {
                    typedAddressCalls++;
                    return Destination;
                }, typedFactory)
                : TypedFaultActivity(operation, typedCanceledRecorder, typedFactory);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => typed.FaultedAsync(typedCanceledFault, typedNext));
        }
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new PublishActivity<TestSaga, Input, Output>(typedPublishFactory).ExecuteAsync(typedCanceledNormal, typedNext));
        Assert.Equal(0, typedFactoryCalls);
        Assert.Equal(0, typedAddressCalls);
        Assert.Equal(0, typedCanceledRecorder.EndpointRequests);
        Assert.Equal(0, typedCanceledRecorder.TransportCalls);
        Assert.Equal(0, typedNext.Calls);

        var typedFailureRecorder = new TransportRecorder { TransportTask = Task.FromException(failure) };
        var typedActiveFault = Context<IBehaviorExceptionContext<TestSaga, Input, BaseFault>>(typedFailureRecorder);
        var typedActiveNormal = Context<IBehaviorContext<TestSaga, Input>>(typedFailureRecorder);
        var typedActiveFactory = Factory<IBehaviorExceptionContext<TestSaga, Input, BaseFault>>(_ => new Output());
        foreach (Operation operation in Enum.GetValues<Operation>())
        {
            InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                TypedFaultActivity(operation, typedFailureRecorder, typedActiveFactory)
                    .FaultedAsync(typedActiveFault, typedNext));
            Assert.Same(failure, actual);
        }
        InvalidOperationException typedPublishFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new PublishActivity<TestSaga, Input, Output>(Factory<IBehaviorContext<TestSaga, Input>>(_ => new Output()))
                .ExecuteAsync(typedActiveNormal, typedNext));
        Assert.Same(failure, typedPublishFailure);
        Assert.Equal(0, typedNext.Calls);

        var typedFactoryFailure = new InvalidOperationException("typed factory failure");
        var brokenTypedFactory = new ContextMessageFactory<IBehaviorExceptionContext<TestSaga, Input, BaseFault>, Output>(
            _ => Task.FromException<InitializedMessage<Output>>(typedFactoryFailure));
        foreach (Operation operation in Enum.GetValues<Operation>())
        {
            int beforeTransport = typedFailureRecorder.TransportCalls;
            InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                TypedFaultActivity(operation, typedFailureRecorder, brokenTypedFactory)
                    .FaultedAsync(typedActiveFault, typedNext));
            Assert.Same(typedFactoryFailure, actual);
            Assert.Equal(beforeTransport, typedFailureRecorder.TransportCalls);
        }
        var brokenTypedPublishFactory = new ContextMessageFactory<IBehaviorContext<TestSaga, Input>, Output>(
            _ => Task.FromException<InitializedMessage<Output>>(typedFactoryFailure));
        InvalidOperationException typedPublishFactoryFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new PublishActivity<TestSaga, Input, Output>(brokenTypedPublishFactory)
                .ExecuteAsync(typedActiveNormal, typedNext));
        Assert.Same(typedFactoryFailure, typedPublishFactoryFailure);
        Assert.Equal(0, typedNext.Calls);

        var typedEndpointFailure = new InvalidOperationException("typed endpoint failure");
        var typedEndpointRecorder = new TransportRecorder
        {
            EndpointTask = Task.FromException<ISendEndpoint>(typedEndpointFailure),
        };
        var typedEndpointFault = Context<IBehaviorExceptionContext<TestSaga, Input, BaseFault>>(typedEndpointRecorder);
        var typedEndpointFactoryCalls = 0;
        var typedEndpointFactory = Factory<IBehaviorExceptionContext<TestSaga, Input, BaseFault>>(_ =>
        {
            typedEndpointFactoryCalls++;
            return new Output();
        });
        InvalidOperationException typedEndpointActual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new FaultedSendActivity<TestSaga, Input, BaseFault, Output>(_ => Destination, typedEndpointFactory)
                .FaultedAsync(typedEndpointFault, typedNext));
        Assert.Same(typedEndpointFailure, typedEndpointActual);
        Assert.Equal(1, typedEndpointRecorder.EndpointRequests);
        Assert.Equal(0, typedEndpointFactoryCalls);
        Assert.Equal(0, typedEndpointRecorder.TransportCalls);
        Assert.Equal(0, typedNext.Calls);

        var typedTransportRecorder = new TransportRecorder
        {
            TransportTask = Task.FromCanceled(transportCancellation.Token),
        };
        var typedTransportFault = Context<IBehaviorExceptionContext<TestSaga, Input, BaseFault>>(typedTransportRecorder);
        var typedTransportNormal = Context<IBehaviorContext<TestSaga, Input>>(typedTransportRecorder);
        foreach (Operation operation in Enum.GetValues<Operation>())
        {
            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                TypedFaultActivity(operation, typedTransportRecorder, typedActiveFactory)
                    .FaultedAsync(typedTransportFault, typedNext));
            Assert.Equal(transportCancellation.Token, actual.CancellationToken);
        }
        OperationCanceledException typedPublishedCanceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new PublishActivity<TestSaga, Input, Output>(Factory<IBehaviorContext<TestSaga, Input>>(_ => new Output()))
                .ExecuteAsync(typedTransportNormal, typedNext));
        Assert.Equal(transportCancellation.Token, typedPublishedCanceled.CancellationToken);
        Assert.Equal(0, typedNext.Calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RECOVERY", "iteration-221-faulted-dispatch-independent-concurrency")]
    public async Task ConcurrentFaults_KeepEachContextAndContinuationIndependentAsync()
    {
        const int Count = 32;
        var seen = new ConcurrentDictionary<object, int>();
        var factory = Factory<IBehaviorExceptionContext<TestSaga, BaseFault>>(context =>
        {
            seen.AddOrUpdate(context, 1, (_, count) => count + 1);
            return new Output();
        });
        var activity = new FaultedPublishActivity<TestSaga, BaseFault, Output>(factory);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var contexts = Enumerable.Range(0, Count)
            .Select(_ => Context<IBehaviorExceptionContext<TestSaga, BaseFault>>(
                new TransportRecorder { TransportTask = gate.Task }))
            .ToArray();
        var next = new Next();
        Task[] pending = contexts.Select(context => activity.FaultedAsync(context, next)).ToArray();
        Assert.Equal(Count, seen.Count);
        Assert.All(contexts, context => Assert.Equal(1, seen[context]));
        Assert.Equal(0, next.FaultCalls);
        Assert.All(pending, task => Assert.False(task.IsCompleted));
        gate.SetResult();
        await Task.WhenAll(pending);
        Assert.Equal(Count, next.FaultCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "normal-respond-null-context-preserves-argument-validation-before-effects")]
    public async Task Respond_RejectsNullContextBeforeFactoryTransportAndContinuationAsync()
    {
        var recorder = new TransportRecorder();
        var factoryCalls = 0;
        var factory = new ContextMessageFactory<IBehaviorContext<TestSaga, Input>, Output>(_ =>
        {
            factoryCalls++;
            return Task.FromResult(new InitializedMessage<Output>(new Output()));
        });
        var next = new TypedNext(recorder);
        Task operation = new RespondActivity<TestSaga, Input, Output>(factory).ExecuteAsync(null!, next);
        ArgumentNullException failure = await Assert.ThrowsAsync<ArgumentNullException>(() => operation);

        Assert.Equal("context", failure.ParamName);
        Assert.Equal(0, factoryCalls);
        Assert.Equal(0, recorder.TransportCalls);
        Assert.Empty(recorder.Events);
        Assert.Equal(0, next.Calls);
        Assert.Equal(0, next.FaultCalls);
        Assert.Null(next.LastContext);
        Assert.True(operation.IsFaulted);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "normal-respond-context-cancellation-factory-admission-and-awaited-response")]
    public async Task Respond_UsesContextCancellationForFactoryAdmissionAndAwaitsTheResponseAsync(
        bool withCancellationToken, bool cancelBeforeExecution)
    {
        using var caller = new CancellationTokenSource();
        CancellationToken token = withCancellationToken ? caller.Token : CancellationToken.None;
        if (cancelBeforeExecution)
            caller.Cancel();
        var recorder = new TransportRecorder(token);
        IBehaviorContext<TestSaga, Input> context = Context<IBehaviorContext<TestSaga, Input>>(recorder);
        var output = new Output();
        var initialized = new InitializedMessage<Output>(output);
        var factoryContexts = new List<object>();
        var factory = new ContextMessageFactory<IBehaviorContext<TestSaga, Input>, Output>(observed =>
        {
            factoryContexts.Add(observed);
            recorder.Events.Enqueue("factory");
            return Task.FromResult(initialized);
        });
        var response = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        recorder.TransportTask = cancelBeforeExecution ? Task.CompletedTask : response.Task;
        var next = new TypedNext();
        Task? operation = null;
        try
        {
            operation = new RespondActivity<TestSaga, Input, Output>(factory).ExecuteAsync(context, next);
            if (cancelBeforeExecution)
            {
                Exception? observed = await Record.ExceptionAsync(() => operation);
                Assert.Empty(factoryContexts);
                Assert.Equal(0, recorder.TransportCalls);
                Assert.Equal(0, next.Calls);
                Assert.Empty(recorder.Events);
                OperationCanceledException canceled = Assert.IsAssignableFrom<OperationCanceledException>(observed);
                Assert.Equal(token, canceled.CancellationToken);
                Assert.True(operation.IsCanceled);
            }
            else
            {
                Assert.Same(context, Assert.Single(factoryContexts));
                Assert.Equal(token, context.CancellationToken);
                Assert.Equal(["factory", "RespondAsync"], recorder.Events);
                Assert.Equal(1, recorder.TransportCalls);
                Assert.Same(output, recorder.Payload);
                Assert.Same(initialized.Pipe, recorder.Pipe);
                Assert.Equal(0, next.Calls);
                Assert.False(operation.IsCompleted);
                response.SetResult();
                await operation;
                Assert.Equal(1, next.Calls);
                Assert.Equal(0, next.FaultCalls);
                Assert.Same(context, next.LastContext);
                Assert.Equal(1, recorder.TransportCalls);
                Assert.Equal(["factory", "RespondAsync"], recorder.Events);
            }
        }
        finally
        {
            response.TrySetResult();
            if (operation is not null)
                await Record.ExceptionAsync(() => operation);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "normal-respond-exact-factory-and-response-failure-before-continuation")]
    public async Task Respond_FactoryAndResponseFailuresPreserveTheExactFailureAndPreventContinuationAsync(bool factoryFails)
    {
        var failure = new System.IO.IOException("unique normal respond failure");
        var recorder = new TransportRecorder
        {
            TransportTask = factoryFails ? Task.CompletedTask : Task.FromException(failure),
        };
        IBehaviorContext<TestSaga, Input> context = Context<IBehaviorContext<TestSaga, Input>>(recorder);
        var initialized = new InitializedMessage<Output>(new Output());
        var factoryContexts = new List<object>();
        var factory = new ContextMessageFactory<IBehaviorContext<TestSaga, Input>, Output>(observed =>
        {
            factoryContexts.Add(observed);
            recorder.Events.Enqueue("factory");
            return factoryFails
                ? Task.FromException<InitializedMessage<Output>>(failure)
                : Task.FromResult(initialized);
        });
        var next = new TypedNext();
        Task operation = new RespondActivity<TestSaga, Input, Output>(factory).ExecuteAsync(context, next);
        Exception? observed = await Record.ExceptionAsync(() => operation);

        Assert.Same(failure, observed);
        Assert.Same(context, Assert.Single(factoryContexts));
        Assert.Equal(factoryFails ? 0 : 1, recorder.TransportCalls);
        Assert.Equal(0, next.Calls);
        Assert.Equal(0, next.FaultCalls);
        Assert.True(operation.IsFaulted);
        Assert.Equal(factoryFails ? new[] { "factory" } : new[] { "factory", "RespondAsync" }, recorder.Events);
        if (!factoryFails)
        {
            Assert.Same(initialized.Message, recorder.Payload);
            Assert.Same(initialized.Pipe, recorder.Pipe);
        }
    }


    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "normal-send-resolution-token-and-healthy-continuation")]
    public async Task Send_UsesTheExactContextTokenForEndpointResolutionAndHealthyContinuationAsync(bool typed, bool withCancellationToken)
    {
        using var caller = new CancellationTokenSource();
        CancellationToken token = withCancellationToken ? caller.Token : CancellationToken.None;
        var recorder = new TransportRecorder(token);
        var output = new Output();
        var initialized = new InitializedMessage<Output>(output);
        var factoryContexts = new List<object>();
        var next = new Next(execute: _ =>
        {
            recorder.Events.Enqueue("next");
            return Task.CompletedTask;
        });
        var typedNext = new TypedNext();

        await ExecuteNormalSendAsync(typed, recorder, initialized, factoryContexts, next, typedNext, out object context);

        Assert.Equal(token, recorder.EndpointToken);
        Assert.Equal(token, recorder.TransportToken);
        Assert.Equal(Destination, recorder.Address);
        Assert.Same(context, Assert.Single(factoryContexts));
        Assert.Same(output, recorder.Payload);
        Assert.Same(initialized.Pipe, recorder.Pipe);
        Assert.Equal(1, recorder.EndpointRequests);
        Assert.Equal(1, recorder.TransportCalls);
        Assert.Equal(typed ? 0 : 1, next.Calls);
        Assert.Equal(typed ? 1 : 0, typedNext.Calls);
        Assert.Same(context, typed ? typedNext.LastContext : next.LastContext);
        string[] expectedEvents = typed
            ? ["address", "GetSendEndpointAsync", "factory", "SendAsync"]
            : ["address", "GetSendEndpointAsync", "factory", "SendAsync", "next"];
        Assert.Equal(expectedEvents, recorder.Events);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "normal-send-cooperative-resolution-cancellation")]
    public async Task Send_CooperativeEndpointCancellationPreventsFactorySendAndContinuationAsync(bool typed)
    {
        using var caller = new CancellationTokenSource();
        var endpoint = new TaskCompletionSource<ISendEndpoint>(TaskCreationOptions.RunContinuationsAsynchronously);
        var recorder = new TransportRecorder(caller.Token) { EndpointTask = endpoint.Task };
        var initialized = new InitializedMessage<Output>(new Output());
        var factoryContexts = new List<object>();
        var next = new Next();
        var typedNext = new TypedNext();
        CancellationTokenRegistration registration = default;
        recorder.EndpointResolutionStarted = token =>
        {
            registration = token.Register(() => endpoint.TrySetCanceled(token));
        };
        Task? operation = null;
        try
        {
            operation = ExecuteNormalSendAsync(typed, recorder, initialized, factoryContexts, next, typedNext, out _);
            Assert.False(operation.IsCompleted);
            Assert.Equal(["address", "GetSendEndpointAsync"], recorder.Events);
            Assert.Equal(1, recorder.EndpointRequests);

            caller.Cancel();

            // This finite state check also protects the original implementation from a hanging await.
            Assert.True(endpoint.Task.IsCanceled, "The saga token must cancel cooperative endpoint resolution.");
            Assert.Equal(caller.Token, recorder.EndpointToken);
            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
            Assert.Equal(caller.Token, actual.CancellationToken);
            Assert.Empty(factoryContexts);
            Assert.Equal(0, recorder.TransportCalls);
            Assert.Equal(0, next.Calls);
            Assert.Equal(0, typedNext.Calls);
            Assert.Equal(["address", "GetSendEndpointAsync"], recorder.Events);
        }
        finally
        {
            endpoint.TrySetCanceled(caller.Token);
            if (operation is not null)
                await Record.ExceptionAsync(() => operation);
            registration.Dispose();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "normal-send-cancellation-before-factory-admission")]
    public async Task Send_CancellationAfterResolutionPreventsFactorySendAndContinuationAsync(bool typed)
    {
        using var caller = new CancellationTokenSource();
        var recorder = new TransportRecorder(caller.Token);
        recorder.EndpointResolutionStarted = _ =>
        {
            caller.Cancel();
            recorder.TransportTask = Task.FromCanceled(caller.Token);
        };
        var initialized = new InitializedMessage<Output>(new Output());
        var factoryContexts = new List<object>();
        var next = new Next();
        var typedNext = new TypedNext();

        Task operation = ExecuteNormalSendAsync(typed, recorder, initialized, factoryContexts, next, typedNext, out _);
        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);

        Assert.Equal(caller.Token, actual.CancellationToken);
        Assert.Empty(factoryContexts);
        Assert.Equal(1, recorder.EndpointRequests);
        Assert.Equal(0, recorder.TransportCalls);
        Assert.Equal(0, next.Calls);
        Assert.Equal(0, typedNext.Calls);
        Assert.Equal(["address", "GetSendEndpointAsync"], recorder.Events);
    }

    static Task ExecuteNormalSendAsync(bool typed, TransportRecorder recorder, InitializedMessage<Output> initialized,
        List<object> factoryContexts, Next next, TypedNext typedNext, out object context)
    {
        if (typed)
        {
            IBehaviorContext<TestSaga, Input> typedContext = Context<IBehaviorContext<TestSaga, Input>>(recorder);
            context = typedContext;
            var factory = new ContextMessageFactory<IBehaviorContext<TestSaga, Input>, Output>(observed =>
            {
                factoryContexts.Add(observed);
                recorder.Events.Enqueue("factory");
                return Task.FromResult(initialized);
            });
            return new SendActivity<TestSaga, Input, Output>(_ =>
            {
                recorder.Events.Enqueue("address");
                return Destination;
            }, factory).ExecuteAsync(typedContext, typedNext);
        }

        IBehaviorContext<TestSaga> untypedContext = Context<IBehaviorContext<TestSaga>>(recorder);
        context = untypedContext;
        var untypedFactory = new ContextMessageFactory<IBehaviorContext<TestSaga>, Output>(observed =>
        {
            factoryContexts.Add(observed);
            recorder.Events.Enqueue("factory");
            return Task.FromResult(initialized);
        });
        return new SendActivity<TestSaga, Output>(_ =>
        {
            recorder.Events.Enqueue("address");
            return Destination;
        }, untypedFactory).ExecuteAsync(untypedContext, next);
    }

    static IStateMachineActivity<TestSaga> UntypedFaultActivity(Operation operation, TransportRecorder recorder,
        ContextMessageFactory<IBehaviorExceptionContext<TestSaga, BaseFault>, Output> factory) => operation switch
        {
            Operation.Publish => new FaultedPublishActivity<TestSaga, BaseFault, Output>(factory),
            Operation.Respond => new FaultedRespondActivity<TestSaga, BaseFault, Output>(factory),
            Operation.Send => new FaultedSendActivity<TestSaga, BaseFault, Output>(_ =>
            {
                recorder.Events.Enqueue("address");
                return Destination;
            }, factory),
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };

    static IStateMachineActivity<TestSaga, Input> TypedFaultActivity(Operation operation, TransportRecorder recorder,
        ContextMessageFactory<IBehaviorExceptionContext<TestSaga, Input, BaseFault>, Output> factory) => operation switch
        {
            Operation.Publish => new FaultedPublishActivity<TestSaga, Input, BaseFault, Output>(factory),
            Operation.Respond => new FaultedRespondActivity<TestSaga, Input, BaseFault, Output>(factory),
            Operation.Send => new FaultedSendActivity<TestSaga, Input, BaseFault, Output>(_ =>
            {
                recorder.Events.Enqueue("address");
                return Destination;
            }, factory),
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };

    static ContextMessageFactory<TContext, Output> Factory<TContext>(Func<TContext, Output> create)
        where TContext : class, ConsumeContext => new(context => Task.FromResult(new InitializedMessage<Output>(create(context))));

    static T Context<T>(TransportRecorder? recorder = null) where T : class
    {
        T context = DispatchProxy.Create<T, ContextProxy>();
        ((ContextProxy)(object)context).Recorder = recorder ?? new TransportRecorder();
        return context;
    }

    static T Inspection<T>(Func<MethodInfo, object?[], object?> handler) where T : class
    {
        T value = DispatchProxy.Create<T, InspectionProxy>();
        ((InspectionProxy)(object)value).Handler = handler;
        return value;
    }

    static void AssertSurface(Type definition, Type expectedInterface, int expectedMethods, string[] genericNames)
    {
        Assert.True(definition.IsPublic);
        Assert.True(definition.IsClass);
        Assert.False(definition.IsAbstract);
        Assert.Equal(genericNames, definition.GetGenericArguments().Select(argument => argument.Name));
        Type[] parameters = definition.GetGenericArguments();
        Assert.True((parameters[0].GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0);
        Assert.Contains(typeof(ISagaStateMachineInstance), parameters[0].GetGenericParameterConstraints());
        for (var index = 1; index < parameters.Length; index++)
        {
            Type parameter = parameters[index];
            if (parameter.Name.Contains("Exception", StringComparison.Ordinal))
                Assert.Contains(typeof(Exception), parameter.GetGenericParameterConstraints());
            else
                Assert.True((parameter.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0);
        }
        Assert.Contains(definition.GetInterfaces(), implemented =>
            implemented.IsGenericType && implemented.GetGenericTypeDefinition() == expectedInterface);
        MethodInfo[] methods = definition.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.Equal(expectedMethods, methods.Length);
        string[] expectedNames = expectedMethods == 6
            ? ["Accept/0", "ExecuteAsync/0", "ExecuteAsync/1", "FaultedAsync/1", "FaultedAsync/2", "Probe/0"]
            : ["Accept/0", "ExecuteAsync/0", "FaultedAsync/1", "Probe/0"];
        Assert.Equal(expectedNames.OrderBy(name => name, StringComparer.Ordinal),
            methods.Select(method => $"{method.Name}/{method.GetGenericArguments().Length}")
                .OrderBy(name => name, StringComparer.Ordinal));
        Assert.All(methods, method =>
        {
            if (method.ReturnType == typeof(Task))
                Assert.EndsWith("Async", method.Name, StringComparison.Ordinal);
            Assert.All(method.GetParameters(), parameter =>
                Assert.Equal(NullabilityState.NotNull, new NullabilityInfoContext().Create(parameter).ReadState));
        });
        Assert.Empty(definition.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.Empty(definition.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
    }

    static void AssertConstructor(Type type, params (Type Type, string Name)[] expected)
    {
        ConstructorInfo constructor = Assert.Single(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        ParameterInfo[] actual = constructor.GetParameters();
        Assert.Equal(expected.Length, actual.Length);
        for (var index = 0; index < expected.Length; index++)
        {
            Assert.Equal(expected[index].Type, actual[index].ParameterType);
            Assert.Equal(expected[index].Name, actual[index].Name);
            Assert.Equal(NullabilityState.NotNull, new NullabilityInfoContext().Create(actual[index]).ReadState);
        }
    }

    static void AssertArgument(string name, Action action) =>
        Assert.Equal(name, Assert.Throws<ArgumentNullException>(action).ParamName);

    static async Task AssertArgumentAsync(string name, Func<Task> action) =>
        Assert.Equal(name, (await Assert.ThrowsAsync<ArgumentNullException>(action)).ParamName);

    static string MethodName(Operation operation) => operation switch
    {
        Operation.Publish => "PublishAsync",
        Operation.Respond => "RespondAsync",
        Operation.Send => "SendAsync",
        _ => throw new ArgumentOutOfRangeException(nameof(operation)),
    };

    static string[] ExpectedEvents(Operation operation) => operation == Operation.Send
        ? ["address", "GetSendEndpointAsync", "factory", "SendAsync", "next-fault"]
        : ["factory", MethodName(operation), "next-fault"];

    enum Operation { Publish, Respond, Send }

    public sealed class TestSaga : ISagaStateMachineInstance { public Guid CorrelationId { get; set; } }
    public sealed record Input;
    public sealed record Output;
    public class BaseFault : Exception { }
    public sealed class DerivedFault : BaseFault { }
    public sealed class OtherFault : Exception { }

    public sealed class TransportRecorder(CancellationToken token = default)
    {
        public CancellationToken Token { get; } = token;
        public ConcurrentQueue<string> Events { get; } = new();
        public object? Payload { get; set; }
        public object? Pipe { get; set; }
        public CancellationToken TransportToken { get; set; }
        public CancellationToken EndpointToken { get; set; }
        public Uri? Address { get; set; }
        public int EndpointRequests { get; set; }
        public int TransportCalls { get; set; }
        public Task TransportTask { get; set; } = Task.CompletedTask;
        public Task<ISendEndpoint>? EndpointTask { get; set; }
        public Action<CancellationToken>? EndpointResolutionStarted { get; set; }
    }

    public class ContextProxy : DispatchProxy
    {
        public TransportRecorder Recorder { get; set; } = null!;

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name == "get_CancellationToken")
                return Recorder.Token;
            if (method?.Name == "GetSendEndpointAsync")
            {
                Recorder.Events.Enqueue(method.Name);
                Recorder.Address = Assert.IsType<Uri>(args![0]);
                Recorder.EndpointToken = Assert.IsType<CancellationToken>(args[1]);
                Recorder.EndpointRequests++;
                Recorder.EndpointResolutionStarted?.Invoke(Recorder.EndpointToken);
                ISendEndpoint endpoint = DispatchProxy.Create<IAdvancedSendEndpoint, EndpointProxy>();
                ((EndpointProxy)endpoint).Recorder = Recorder;
                return Recorder.EndpointTask ?? Task.FromResult(endpoint);
            }
            if (method?.Name is "PublishAsync" or "RespondAsync")
            {
                Recorder.Events.Enqueue(method.Name);
                Recorder.Payload = args![0];
                Recorder.Pipe = args[1];
                if (args.Length == 3)
                    Recorder.TransportToken = Assert.IsType<CancellationToken>(args[2]);
                Recorder.TransportCalls++;
                return Recorder.TransportTask;
            }
            throw new NotSupportedException(method?.Name);
        }
    }

    public class EndpointProxy : DispatchProxy
    {
        public TransportRecorder Recorder { get; set; } = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name != "SendAsync")
                throw new NotSupportedException(method?.Name);
            Recorder.Events.Enqueue(method.Name);
            Recorder.Payload = args![0];
            Recorder.Pipe = args[1];
            Recorder.TransportToken = Assert.IsType<CancellationToken>(args[2]);
            Recorder.TransportCalls++;
            return Recorder.TransportTask;
        }
    }

    public class InspectionProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[], object?> Handler { get; set; } = null!;

        protected override object? Invoke(MethodInfo? method, object?[]? args) =>
            Handler(method ?? throw new InvalidOperationException("Missing inspection method."), args ?? []);
    }

    sealed class Next(Func<object, Task>? execute = null, Func<object, Task>? fault = null,
        TransportRecorder? recorder = null) : IBehavior<TestSaga>
    {
        int _calls;
        int _faultCalls;
        public int Calls => Volatile.Read(ref _calls);
        public int FaultCalls => Volatile.Read(ref _faultCalls);
        public object? LastContext { get; private set; }
        public Task ExecuteAsync(IBehaviorContext<TestSaga> context) => RunExecuteAsync(context);
        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context) where T : class => RunExecuteAsync(context);
        public Task FaultedAsync<T>(IBehaviorExceptionContext<TestSaga, T> context) where T : Exception => RunFaultAsync(context);
        public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TestSaga, T, TException> context)
            where T : class where TException : Exception => RunFaultAsync(context);
        public void Accept(IStateMachineVisitor visitor) { }
        public void Probe(ProbeContext context) { }
        Task RunExecuteAsync(object context)
        {
            Interlocked.Increment(ref _calls);
            LastContext = context;
            return execute?.Invoke(context) ?? Task.CompletedTask;
        }
        Task RunFaultAsync(object context)
        {
            Interlocked.Increment(ref _calls);
            Interlocked.Increment(ref _faultCalls);
            LastContext = context;
            recorder?.Events.Enqueue("next-fault");
            return fault?.Invoke(context) ?? Task.CompletedTask;
        }
    }

    sealed class TypedNext(TransportRecorder? recorder = null) : IBehavior<TestSaga, Input>
    {
        public int Calls { get; private set; }
        public int FaultCalls { get; private set; }
        public object? LastContext { get; private set; }
        public Task ExecuteAsync(IBehaviorContext<TestSaga, Input> context)
        {
            Calls++;
            LastContext = context;
            return Task.CompletedTask;
        }
        public Task FaultedAsync<T>(IBehaviorExceptionContext<TestSaga, Input, T> context) where T : Exception
        {
            Calls++;
            FaultCalls++;
            LastContext = context;
            recorder?.Events.Enqueue("next-fault");
            return Task.CompletedTask;
        }
        public void Accept(IStateMachineVisitor visitor) { }
        public void Probe(ProbeContext context) { }
    }

}
