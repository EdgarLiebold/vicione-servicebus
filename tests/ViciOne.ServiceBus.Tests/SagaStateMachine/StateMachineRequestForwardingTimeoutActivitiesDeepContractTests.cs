using System.Net.Mime;
using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Components;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineRequestForwardingTimeoutActivitiesDeepContractTests
{
    static readonly DateTimeOffset Now = new(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-221-request-activities-public-shape-and-required-boundaries")]
    public async Task Activities_ExposeExactShapeAndRejectMissingOwnedArgumentsAsync()
    {
        Type openCancel = typeof(CancelRequestTimeoutActivity<,,,>);
        Assert.Equal(["TSaga", "TMessage", "TRequest", "TResponse"], openCancel.GetGenericArguments().Select(x => x.Name));
        Assert.All(openCancel.GetGenericArguments(), x => Assert.True(
            x.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint)));
        Assert.Equal(typeof(ISagaStateMachineInstance), Assert.Single(openCancel.GetGenericArguments()[0].GetGenericParameterConstraints()));

        Type cancelType = typeof(CancelRequestTimeoutActivity<TestSaga, Message, Request, Response>);
        Assert.Contains(typeof(IStateMachineActivity<TestSaga, Message>), cancelType.GetInterfaces());
        Assert.Contains(typeof(IStateMachineActivity<RequestState, IRequestCompleted>),
            typeof(CompleteRequestActivity).GetInterfaces());
        Assert.Contains(typeof(IStateMachineActivity<RequestState, IRequestFaulted>),
            typeof(FaultRequestActivity).GetInterfaces());
        Assert.All(new[] { cancelType, typeof(CompleteRequestActivity), typeof(FaultRequestActivity) }, type =>
        {
            Assert.True(type.IsPublic);
            Assert.False(type.IsSealed);
            Assert.Equal(["Accept", "ExecuteAsync", "FaultedAsync", "Probe"],
                type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Select(x => x.Name).OrderBy(x => x));
            Assert.All(type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(x => x.ReturnType == typeof(Task)),
                method => Assert.EndsWith("Async", method.Name, StringComparison.Ordinal));
        });

        AssertArgument("request", () => new CancelRequestTimeoutActivity<TestSaga, Message, Request, Response>(null!, true));
        var cancel = new CancelRequestTimeoutActivity<TestSaga, Message, Request, Response>(CreateRequest().Request, true);
        var completed = new CompleteRequestActivity();
        var faulted = new FaultRequestActivity();
        AssertArgument("visitor", () => cancel.Accept(null!));
        AssertArgument("context", () => cancel.Probe(null!));
        AssertArgument("visitor", () => completed.Accept(null!));
        AssertArgument("context", () => completed.Probe(null!));
        AssertArgument("visitor", () => faulted.Accept(null!));
        AssertArgument("context", () => faulted.Probe(null!));
        var visited = new List<object>();
        var visitor = Strict<IStateMachineVisitor>((method, args) =>
        {
            Assert.Equal("Visit", method.Name);
            visited.Add(Assert.Single(args)!);
            return null;
        });
        var scopes = new List<string>();
        var scope = Strict<ProbeContext>();
        var probe = Strict<ProbeContext>((method, args) =>
        {
            Assert.Equal("CreateScope", method.Name);
            scopes.Add(Assert.IsType<string>(Assert.Single(args)));
            return scope;
        });
        cancel.Accept(visitor);
        completed.Accept(visitor);
        faulted.Accept(visitor);
        cancel.Probe(probe);
        completed.Probe(probe);
        faulted.Probe(probe);
        Assert.Equal(new object[] { cancel, completed, faulted }, visited);
        Assert.Equal(["cancelRequest", "completeRequest", "faultRequest"], scopes);

        var cancelContext = Strict<IBehaviorContext<TestSaga, Message>>();
        var cancelNext = Strict<IBehavior<TestSaga, Message>>();
        await AssertArgumentAsync("context", () => cancel.ExecuteAsync(null!, cancelNext));
        await AssertArgumentAsync("next", () => cancel.ExecuteAsync(cancelContext, null!));
        var completionContext = Strict<IBehaviorContext<RequestState, IRequestCompleted>>();
        var completionNext = Strict<IBehavior<RequestState, IRequestCompleted>>();
        await AssertArgumentAsync("context", () => completed.ExecuteAsync(null!, completionNext));
        await AssertArgumentAsync("next", () => completed.ExecuteAsync(completionContext, null!));
        var faultContext = Strict<IBehaviorContext<RequestState, IRequestFaulted>>();
        var faultNext = Strict<IBehavior<RequestState, IRequestFaulted>>();
        await AssertArgumentAsync("context", () => faulted.ExecuteAsync(null!, faultNext));
        await AssertArgumentAsync("next", () => faulted.ExecuteAsync(faultContext, null!));

        AssertArgument("context", () => cancel.FaultedAsync<MarkerException>(null!, cancelNext));
        AssertArgument("next", () => cancel.FaultedAsync(Strict<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>(), null!));
        AssertArgument("context", () => completed.FaultedAsync<MarkerException>(null!, completionNext));
        AssertArgument("next", () => completed.FaultedAsync(Strict<IBehaviorExceptionContext<RequestState, IRequestCompleted, MarkerException>>(), null!));
        AssertArgument("context", () => faulted.FaultedAsync<MarkerException>(null!, faultNext));
        AssertArgument("next", () => faulted.FaultedAsync(Strict<IBehaviorExceptionContext<RequestState, IRequestFaulted, MarkerException>>(), null!));
    }

    [Theory]
    [InlineData(true, -1, false, 1)]
    [InlineData(false, 0, true, 1)]
    [InlineData(true, 0, false, 1)]
    [InlineData(false, 0, false, 0)]
    [InlineData(false, 1, false, 0)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "iteration-221-cancel-timeout-clear-policy-and-scheduler-absence")]
    public async Task CancelTimeout_UsesRequestIdentityAndClearPolicyWithoutInventingASchedulerAsync(
        bool completed, int timeoutMinutes, bool clearOnFault, int expectedClears)
    {
        Guid requestId = Guid.NewGuid();
        var request = CreateRequest(requestId, TimeSpan.FromMinutes(timeoutMinutes), clearOnFault);
        var saga = new TestSaga { CorrelationId = Guid.NewGuid(), RequestId = requestId };
        var calls = new List<string>();
        var next = Strict<IBehavior<TestSaga, Message>>((method, args) =>
        {
            Assert.Equal("ExecuteAsync", method.Name);
            calls.Add("next");
            return Task.CompletedTask;
        });
        var context = CancelContext(saga, new Message(), scheduler: null, CancellationToken.None);
        var activity = new CancelRequestTimeoutActivity<TestSaga, Message, Request, Response>(request.Request, completed);

        if (timeoutMinutes > 0)
        {
            ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(() => activity.ExecuteAsync(context, next));
            Assert.Contains("scheduler", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(calls);
            Assert.Empty(request.Writes);
            Assert.Equal(requestId, saga.RequestId);
        }
        else
        {
            await activity.ExecuteAsync(context, next);
            Assert.Equal(["next"], calls);
            Assert.Equal(expectedClears, request.Writes.Count);
            Assert.Equal(expectedClears == 1 ? null : requestId, saga.RequestId);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "iteration-221-cancel-missing-request-id-no-scheduler-lookup")]
    public async Task CancelTimeout_WithoutRequestIdSkipsCancellationEvenWhenTimeoutIsPositiveAsync()
    {
        var request = CreateRequest(timeout: TimeSpan.FromMinutes(2));
        var saga = new TestSaga { CorrelationId = Guid.NewGuid() };
        var context = CancelContext(saga, new Message(), scheduler: null, CancellationToken.None);
        var continuations = 0;
        var next = Strict<IBehavior<TestSaga, Message>>((method, args) =>
        {
            Assert.Same(context, args[0]);
            continuations++;
            return Task.CompletedTask;
        });

        await new CancelRequestTimeoutActivity<TestSaga, Message, Request, Response>(request.Request, completed: false)
            .ExecuteAsync(context, next);

        Assert.Equal(1, continuations);
        Assert.Empty(request.Writes);
        Assert.Null(saga.RequestId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "iteration-221-cancel-timeout-await-identity-token-failure-and-continuation")]
    public async Task CancelTimeout_AwaitsExactSchedulerCancellationBeforeClearingAndContinuingAsync()
    {
        Guid requestId = Guid.NewGuid();
        Uri inputAddress = new("loopback://localhost/request-state");
        using var cancellation = new CancellationTokenSource();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = new List<string>();
        var scheduler = Strict<MessageSchedulerContext>((method, args) =>
        {
            Assert.Equal("CancelScheduledSendAsync", method.Name);
            Assert.Equal(inputAddress, args[0]);
            Assert.Equal(requestId, args[1]);
            Assert.Equal(cancellation.Token, args[2]);
            calls.Add("cancel");
            return completion.Task;
        });
        var request = CreateRequest(requestId, TimeSpan.FromMinutes(2), clearOnFault: false);
        var saga = new TestSaga { CorrelationId = Guid.NewGuid(), RequestId = requestId };
        var context = CancelContext(saga, new Message(), scheduler, cancellation.Token, inputAddress);
        var next = Strict<IBehavior<TestSaga, Message>>((method, args) =>
        {
            Assert.Equal("ExecuteAsync", method.Name);
            Assert.Same(context, args[0]);
            calls.Add("next");
            return Task.CompletedTask;
        });
        var activity = new CancelRequestTimeoutActivity<TestSaga, Message, Request, Response>(request.Request, completed: true);

        Task running = activity.ExecuteAsync(context, next);
        Assert.False(running.IsCompleted);
        Assert.Equal(["cancel"], calls);
        Assert.Equal(requestId, saga.RequestId);
        Assert.Empty(request.Writes);

        completion.SetResult();
        await running;
        Assert.Equal(["cancel", "next"], calls);
        Assert.Equal(new Guid?[] { null }, request.Writes);
        Assert.Null(saga.RequestId);

        var failure = new MarkerException();
        var failedCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failingScheduler = Strict<MessageSchedulerContext>((method, _) => failedCompletion.Task);
        request = CreateRequest(requestId, TimeSpan.FromMinutes(2), clearOnFault: true);
        saga.RequestId = requestId;
        context = CancelContext(saga, new Message(), failingScheduler, cancellation.Token, inputAddress);
        activity = new CancelRequestTimeoutActivity<TestSaga, Message, Request, Response>(request.Request, completed: false);
        Task failed = activity.ExecuteAsync(context, next);
        failedCompletion.SetException(failure);
        Assert.Same(failure, await Assert.ThrowsAsync<MarkerException>(() => failed));
        Assert.Equal(requestId, saga.RequestId);
        Assert.Empty(request.Writes);
        Assert.Equal(["cancel", "next"], calls);

        using var canceledSource = new CancellationTokenSource();
        canceledSource.Cancel();
        var canceledScheduler = Strict<MessageSchedulerContext>((method, args) =>
        {
            Assert.Equal(canceledSource.Token, args[2]);
            return Task.FromCanceled(canceledSource.Token);
        });
        request = CreateRequest(requestId, TimeSpan.FromMinutes(2), clearOnFault: true);
        context = CancelContext(saga, new Message(), canceledScheduler, canceledSource.Token, inputAddress);
        activity = new CancelRequestTimeoutActivity<TestSaga, Message, Request, Response>(request.Request, completed: false);
        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            activity.ExecuteAsync(context, next));
        Assert.Equal(canceledSource.Token, canceled.CancellationToken);
        Assert.Equal(requestId, saga.RequestId);
        Assert.Empty(request.Writes);
        Assert.Equal(["cancel", "next"], calls);

        var retainRequest = CreateRequest(requestId, TimeSpan.FromMinutes(2), clearOnFault: false);
        var retainSaga = new TestSaga { CorrelationId = Guid.NewGuid(), RequestId = requestId };
        var retainCancelCalls = 0;
        var retainScheduler = Strict<MessageSchedulerContext>((method, args) =>
        {
            Assert.Equal("CancelScheduledSendAsync", method.Name);
            Assert.Equal(inputAddress, args[0]);
            Assert.Equal(requestId, args[1]);
            Assert.Equal(cancellation.Token, args[2]);
            retainCancelCalls++;
            return Task.CompletedTask;
        });
        var retainContext = CancelContext(retainSaga, new Message(), retainScheduler, cancellation.Token, inputAddress);
        var retainNextCalls = 0;
        var retainNext = Strict<IBehavior<TestSaga, Message>>((method, args) =>
        {
            Assert.Equal("ExecuteAsync", method.Name);
            Assert.Same(retainContext, args[0]);
            retainNextCalls++;
            return Task.CompletedTask;
        });
        await new CancelRequestTimeoutActivity<TestSaga, Message, Request, Response>(retainRequest.Request, completed: false)
            .ExecuteAsync(retainContext, retainNext);
        Assert.Equal(1, retainCancelCalls);
        Assert.Equal(1, retainNextCalls);
        Assert.Empty(retainRequest.Writes);
        Assert.Equal(requestId, retainSaga.RequestId);

        using var canceledBeforeLookup = new CancellationTokenSource();
        canceledBeforeLookup.Cancel();
        foreach (bool hasRequestId in new[] { true, false })
        {
            Guid? originalId = hasRequestId ? Guid.NewGuid() : null;
            var canceledSaga = new TestSaga { CorrelationId = Guid.NewGuid(), RequestId = originalId };
            var canceledSettings = Strict<IRequestSettings<TestSaga, Request, Response>>((method, _) => method.Name switch
            {
                "get_Timeout" => TimeSpan.Zero,
                "get_ClearRequestIdOnFaulted" => true,
                _ => throw Unexpected()
            });
            var getterCalls = 0;
            var setterCalls = 0;
            var canceledRequest = Strict<IRequest<TestSaga, Request, Response>>((method, args) =>
            {
                if (method.Name == "GetRequestId")
                {
                    Assert.Same(canceledSaga, args[0]);
                    getterCalls++;
                    return originalId;
                }

                if (method.Name == "SetRequestId")
                {
                    Assert.Same(canceledSaga, args[0]);
                    setterCalls++;
                    canceledSaga.RequestId = (Guid?)args[1];
                    return null;
                }

                return method.Name == "get_Settings" ? canceledSettings : throw Unexpected();
            });
            var canceledContext = CancelContext(canceledSaga, new Message(), scheduler: null, canceledBeforeLookup.Token);
            var nextCalls = 0;
            var canceledNext = Strict<IBehavior<TestSaga, Message>>((method, args) =>
            {
                Assert.Equal("ExecuteAsync", method.Name);
                Assert.Same(canceledContext, args[0]);
                nextCalls++;
                return Task.CompletedTask;
            });

            OperationCanceledException preCanceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                new CancelRequestTimeoutActivity<TestSaga, Message, Request, Response>(canceledRequest, completed: true)
                    .ExecuteAsync(canceledContext, canceledNext));
            Assert.Equal(canceledBeforeLookup.Token, preCanceled.CancellationToken);
            Assert.Equal(0, getterCalls);
            Assert.Equal(0, setterCalls);
            Assert.Equal(0, nextCalls);
            Assert.Equal(originalId, canceledSaga.RequestId);
        }
    }

    [Theory]
    [InlineData(false, -1)]
    [InlineData(true, -1)]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(false, 999)]
    [InlineData(true, 999)]
    [InlineData(false, 5)]
    [InlineData(true, 5)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "iteration-221-complete-fault-forward-expired-future-open-and-pipe-metadata")]
    public async Task RequestOutcome_ForwardsCompletedAndFaultedPayloadWithPipeMetadataAsync(bool fault, int expirationMinutes)
    {
        var payload = new Response("exact-payload");
        string[] types = ["urn:message:first", "urn:message:second"];
        DateTimeOffset? expiration = expirationMinutes switch
        {
            0 => null,
            999 => Now,
            _ => Now.AddMinutes(expirationMinutes)
        };
        var saga = new RequestState
        {
            CorrelationId = Guid.NewGuid(),
            ResponseAddress = new Uri("loopback://localhost/original-requester"),
            SagaAddress = new Uri("loopback://localhost/original-saga"),
            FaultAddress = new Uri("loopback://localhost/original-fault"),
            ConversationId = Guid.NewGuid(),
            ExpirationTime = expiration
        };
        using var cancellation = new CancellationTokenSource();
        var serializer = Strict<IMessageSerializer>((method, _) =>
            method.Name == "get_ContentType" ? new ContentType("application/json") : throw Unexpected());
        object? forwardedPayload = null;
        string[]? forwardedTypes = null;
        var serializerContext = Strict<SerializerContext>((method, args) =>
        {
            Assert.Equal("GetMessageSerializer", method.Name);
            forwardedPayload = args[0];
            forwardedTypes = (string[])args[1]!;
            return serializer;
        });
        var sendContext = new MessageSendContext<object>(new object(), cancellation.Token);
        var calls = new List<string>();
        var endpoint = Strict<IAdvancedSendEndpoint>((method, args) =>
        {
            Assert.Equal("SendAsync", method.Name);
            Assert.Equal(cancellation.Token, args[2]);
            calls.Add("send");
            Assert.NotSame(payload, args[0]);
            return ((IPipe<SendContext>)args[1]!).SendAsync(sendContext);
        });

        if (fault)
        {
            var message = Strict<IRequestFaulted>((method, _) => method.Name switch
            {
                "get_Payload" => payload,
                "get_PayloadType" => types,
                _ => throw Unexpected()
            });
            var context = ForwardContext(saga, message, endpoint, serializerContext, cancellation.Token);
            var next = Strict<IBehavior<RequestState, IRequestFaulted>>((method, args) =>
            {
                Assert.Equal("ExecuteAsync", method.Name);
                Assert.Same(context, args[0]);
                calls.Add("next");
                return Task.CompletedTask;
            });
            await new FaultRequestActivity().ExecuteAsync(context, next);
        }
        else
        {
            var message = Strict<IRequestCompleted>((method, _) => method.Name switch
            {
                "get_Payload" => payload,
                "get_PayloadType" => types,
                _ => throw Unexpected()
            });
            var context = ForwardContext(saga, message, endpoint, serializerContext, cancellation.Token);
            var next = Strict<IBehavior<RequestState, IRequestCompleted>>((method, args) =>
            {
                Assert.Equal("ExecuteAsync", method.Name);
                Assert.Same(context, args[0]);
                calls.Add("next");
                return Task.CompletedTask;
            });
            await new CompleteRequestActivity().ExecuteAsync(context, next);
        }

        Assert.Equal(["send", "next"], calls);
        Assert.Same(payload, forwardedPayload);
        Assert.Equal(["urn:message:first", "urn:message:second"], Assert.IsType<string[]>(forwardedTypes));
        Assert.NotSame(types, forwardedTypes);
        Assert.Equal(saga.ResponseAddress, sendContext.DestinationAddress);
        Assert.Equal(saga.SagaAddress, sendContext.SourceAddress);
        Assert.Equal(saga.FaultAddress, sendContext.FaultAddress);
        Assert.Equal(saga.CorrelationId, sendContext.RequestId);
        Assert.Equal(saga.ConversationId, sendContext.ConversationId);
        Assert.Same(serializer, sendContext.Serializer);
        Assert.Equal(expirationMinutes is 0 ? null :
            expirationMinutes is 999 or < 0 ? TimeSpan.FromSeconds(1) :
            TimeSpan.FromMinutes(expirationMinutes), sendContext.TimeToLive);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "iteration-221-forward-resolution-and-send-failure-exact-identity")]
    public async Task RequestOutcome_DoesNotContinueAfterEndpointResolutionOrSendFailureAsync()
    {
        var saga = new RequestState
        {
            CorrelationId = Guid.NewGuid(),
            ResponseAddress = new Uri("loopback://localhost/response")
        };
        var payload = new Response("payload");
        var completed = Strict<IRequestCompleted>((method, _) => method.Name switch
        {
            "get_Payload" => payload,
            "get_PayloadType" => new[] { "urn:message:response" },
            _ => throw Unexpected()
        });
        var faulted = Strict<IRequestFaulted>((method, _) => method.Name switch
        {
            "get_Payload" => payload,
            "get_PayloadType" => new[] { "urn:message:response" },
            _ => throw Unexpected()
        });
        var failure = new MarkerException();
        var serializer = Strict<SerializerContext>();
        var send = Strict<IAdvancedSendEndpoint>((method, _) => Task.FromException(failure));
        var completedContext = ForwardContext(saga, completed, send, serializer, CancellationToken.None);
        var faultedContext = ForwardContext(saga, faulted, send, serializer, CancellationToken.None);
        var completionNext = Strict<IBehavior<RequestState, IRequestCompleted>>();
        var faultNext = Strict<IBehavior<RequestState, IRequestFaulted>>();

        Assert.Same(failure, await Assert.ThrowsAsync<MarkerException>(() =>
            new CompleteRequestActivity().ExecuteAsync(completedContext, completionNext)));
        Assert.Same(failure, await Assert.ThrowsAsync<MarkerException>(() =>
            new FaultRequestActivity().ExecuteAsync(faultedContext, faultNext)));

        var failedResolution = Strict<IBehaviorContext<RequestState, IRequestCompleted>>((method, _) => method.Name switch
        {
            "get_Saga" => saga,
            "get_Message" => completed,
            "get_CancellationToken" => CancellationToken.None,
            "GetSendEndpointAsync" => Task.FromException<ISendEndpoint>(failure),
            _ => throw Unexpected()
        });
        Assert.Same(failure, await Assert.ThrowsAsync<MarkerException>(() =>
            new CompleteRequestActivity().ExecuteAsync(failedResolution, completionNext)));

        var failedFaultResolution = Strict<IBehaviorContext<RequestState, IRequestFaulted>>((method, _) => method.Name switch
        {
            "get_Saga" => saga,
            "get_Message" => faulted,
            "get_CancellationToken" => CancellationToken.None,
            "GetSendEndpointAsync" => Task.FromException<ISendEndpoint>(failure),
            _ => throw Unexpected()
        });
        Assert.Same(failure, await Assert.ThrowsAsync<MarkerException>(() =>
            new FaultRequestActivity().ExecuteAsync(failedFaultResolution, faultNext)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "iteration-221-forward-contract-snapshot-before-async-resolution")]
    public async Task RequestOutcome_SnapshotsContractIdentitiesBeforeEndpointResolutionAsync(bool fault)
    {
        var saga = new RequestState
        {
            CorrelationId = Guid.NewGuid(),
            ResponseAddress = new Uri("loopback://localhost/response")
        };
        var payload = new Response("snapshot");
        string[] identities = ["urn:message:original"];
        var completion = Strict<IRequestCompleted>((method, _) => method.Name switch
        {
            "get_Payload" => payload,
            "get_PayloadType" => identities,
            _ => throw Unexpected()
        });
        var failure = Strict<IRequestFaulted>((method, _) => method.Name switch
        {
            "get_Payload" => payload,
            "get_PayloadType" => identities,
            _ => throw Unexpected()
        });
        var resolution = new TaskCompletionSource<ISendEndpoint>(TaskCreationOptions.RunContinuationsAsynchronously);
        string[]? serializedIdentities = null;
        var serializer = Strict<IMessageSerializer>((method, _) =>
            method.Name == "get_ContentType" ? new ContentType("application/json") : throw Unexpected());
        var serializerContext = Strict<SerializerContext>((method, args) =>
        {
            Assert.Same(payload, args[0]);
            serializedIdentities = (string[])args[1]!;
            return serializer;
        });
        var endpoint = Strict<IAdvancedSendEndpoint>((method, args) =>
        {
            Assert.NotSame(payload, args[0]);
            return ((IPipe<SendContext>)args[1]!).SendAsync(new MessageSendContext<object>(args[0]!));
        });
        var continued = false;
        Task forwarding;
        if (fault)
        {
            var context = Strict<IBehaviorContext<RequestState, IRequestFaulted>>((method, args) => method.Name switch
            {
                "get_Saga" => saga,
                "get_Message" => failure,
                "get_SerializerContext" => serializerContext,
                "GetSendEndpointAsync" => resolution.Task,
                "TryGetPayload" => TryPayload(method, args, null, new FixedTimeProvider(Now)),
                "get_CancellationToken" => CancellationToken.None,
                _ => throw Unexpected()
            });
            var next = Strict<IBehavior<RequestState, IRequestFaulted>>((method, args) =>
            {
                Assert.Same(context, args[0]);
                continued = true;
                return Task.CompletedTask;
            });
            forwarding = new FaultRequestActivity().ExecuteAsync(context, next);
        }
        else
        {
            var context = Strict<IBehaviorContext<RequestState, IRequestCompleted>>((method, args) => method.Name switch
            {
                "get_Saga" => saga,
                "get_Message" => completion,
                "get_SerializerContext" => serializerContext,
                "GetSendEndpointAsync" => resolution.Task,
                "TryGetPayload" => TryPayload(method, args, null, new FixedTimeProvider(Now)),
                "get_CancellationToken" => CancellationToken.None,
                _ => throw Unexpected()
            });
            var next = Strict<IBehavior<RequestState, IRequestCompleted>>((method, args) =>
            {
                Assert.Same(context, args[0]);
                continued = true;
                return Task.CompletedTask;
            });
            forwarding = new CompleteRequestActivity().ExecuteAsync(context, next);
        }

        Assert.False(forwarding.IsCompleted);
        Assert.False(continued);
        identities[0] = "urn:message:mutated";
        resolution.SetResult(endpoint);
        await forwarding;

        Assert.True(continued);
        Assert.Equal(["urn:message:original"], Assert.IsType<string[]>(serializedIdentities));
        Assert.NotSame(identities, serializedIdentities);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "iteration-221-forward-concurrent-saga-and-pipe-isolation")]
    public async Task RequestOutcome_IsolatesConcurrentSagaRoutesPayloadsAndContinuationsAsync()
    {
        const int count = 16;
        Task[] tasks = Enumerable.Range(0, count).Select(index => Task.Run(async () =>
        {
            var payload = new Response($"response-{index}");
            string identity = $"urn:message:response-{index}";
            var saga = new RequestState
            {
                CorrelationId = Guid.NewGuid(),
                ConversationId = Guid.NewGuid(),
                ResponseAddress = new Uri($"loopback://localhost/response-{index}"),
                SagaAddress = new Uri($"loopback://localhost/saga-{index}"),
                FaultAddress = new Uri($"loopback://localhost/fault-{index}"),
                ExpirationTime = Now.AddMinutes(index + 1)
            };
            var message = Strict<IRequestCompleted>((method, _) => method.Name switch
            {
                "get_Payload" => payload,
                "get_PayloadType" => new[] { identity },
                _ => throw Unexpected()
            });
            var serializer = Strict<IMessageSerializer>((method, _) =>
                method.Name == "get_ContentType" ? new ContentType("application/json") : throw Unexpected());
            var serializerContext = Strict<SerializerContext>((method, args) =>
            {
                Assert.Same(payload, args[0]);
                Assert.Equal([identity], (string[])args[1]!);
                return serializer;
            });
            var outgoing = new MessageSendContext<object>(new object());
            var endpoint = Strict<IAdvancedSendEndpoint>((method, args) =>
                ((IPipe<SendContext>)args[1]!).SendAsync(outgoing));
            var context = ForwardContext(saga, message, endpoint, serializerContext, CancellationToken.None);
            var continuations = 0;
            var next = Strict<IBehavior<RequestState, IRequestCompleted>>((method, args) =>
            {
                Assert.Same(context, args[0]);
                Interlocked.Increment(ref continuations);
                return Task.CompletedTask;
            });

            await new CompleteRequestActivity().ExecuteAsync(context, next);

            Assert.Equal(1, continuations);
            Assert.Equal(saga.CorrelationId, outgoing.RequestId);
            Assert.Equal(saga.ConversationId, outgoing.ConversationId);
            Assert.Equal(saga.ResponseAddress, outgoing.DestinationAddress);
            Assert.Equal(saga.SagaAddress, outgoing.SourceAddress);
            Assert.Equal(saga.FaultAddress, outgoing.FaultAddress);
            Assert.Equal(TimeSpan.FromMinutes(index + 1), outgoing.TimeToLive);
            Assert.Same(serializer, outgoing.Serializer);
        })).ToArray();

        await Task.WhenAll(tasks);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-221-request-activities-fault-continuation-exact-task")]
    public void FaultedPaths_ReturnTheExactNextTaskAndContext()
    {
        Task pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task;
        var cancelContext = Strict<IBehaviorExceptionContext<TestSaga, Message, MarkerException>>();
        var cancelNext = Strict<IBehavior<TestSaga, Message>>((method, args) =>
        {
            Assert.Equal("FaultedAsync", method.Name);
            Assert.Same(cancelContext, args[0]);
            return pending;
        });
        Assert.Same(pending, new CancelRequestTimeoutActivity<TestSaga, Message, Request, Response>(CreateRequest().Request, true)
            .FaultedAsync(cancelContext, cancelNext));

        var completedContext = Strict<IBehaviorExceptionContext<RequestState, IRequestCompleted, MarkerException>>();
        var completionNext = Strict<IBehavior<RequestState, IRequestCompleted>>((method, args) =>
        {
            Assert.Equal("FaultedAsync", method.Name);
            Assert.Same(completedContext, args[0]);
            return pending;
        });
        Assert.Same(pending, new CompleteRequestActivity().FaultedAsync(completedContext, completionNext));

        var faultedContext = Strict<IBehaviorExceptionContext<RequestState, IRequestFaulted, MarkerException>>();
        var faultNext = Strict<IBehavior<RequestState, IRequestFaulted>>((method, args) =>
        {
            Assert.Equal("FaultedAsync", method.Name);
            Assert.Same(faultedContext, args[0]);
            return pending;
        });
        Assert.Same(pending, new FaultRequestActivity().FaultedAsync(faultedContext, faultNext));
    }

    static RequestDouble CreateRequest(Guid? requestId = null, TimeSpan? timeout = null, bool clearOnFault = false)
    {
        var doubleValue = new RequestDouble { RequestId = requestId };
        var settings = Strict<IRequestSettings<TestSaga, Request, Response>>((method, _) => method.Name switch
        {
            "get_Timeout" => timeout ?? TimeSpan.Zero,
            "get_ClearRequestIdOnFaulted" => clearOnFault,
            _ => throw Unexpected()
        });
        doubleValue.Request = Strict<IRequest<TestSaga, Request, Response>>((method, args) => method.Name switch
        {
            "get_Settings" => settings,
            "GetRequestId" => doubleValue.RequestId,
            "SetRequestId" => SetRequestId(doubleValue, (TestSaga)args[0]!, (Guid?)args[1]),
            _ => throw Unexpected()
        });
        return doubleValue;
    }

    static object? SetRequestId(RequestDouble request, TestSaga saga, Guid? id)
    {
        request.Writes.Add(id);
        request.RequestId = id;
        saga.RequestId = id;
        return null;
    }

    static IBehaviorContext<TestSaga, Message> CancelContext(TestSaga saga, Message message,
        MessageSchedulerContext? scheduler, CancellationToken token, Uri? inputAddress = null)
    {
        ReceiveContext receive = Strict<ReceiveContext>((method, _) => method.Name == "get_InputAddress"
            ? inputAddress ?? new Uri("loopback://localhost/request-state") : throw Unexpected());
        return Strict<IBehaviorContext<TestSaga, Message>>((method, args) => method.Name switch
        {
            "get_Saga" => saga,
            "get_Message" => message,
            "get_ReceiveContext" => receive,
            "get_CancellationToken" => token,
            "TryGetPayload" => TryPayload(method, args, scheduler, null),
            _ => throw Unexpected()
        });
    }

    static IBehaviorContext<RequestState, T> ForwardContext<T>(RequestState saga, T message,
        IAdvancedSendEndpoint endpoint, SerializerContext serializer, CancellationToken token)
        where T : class => Strict<IBehaviorContext<RequestState, T>>((method, args) => method.Name switch
        {
            "get_Saga" => saga,
            "get_Message" => message,
            "get_SerializerContext" => serializer,
            "get_CancellationToken" => token,
            "GetSendEndpointAsync" => ResolveEndpoint(args, saga.ResponseAddress, token, endpoint),
            "TryGetPayload" => TryPayload(method, args, null, new FixedTimeProvider(Now)),
            _ => throw Unexpected()
        });

    static Task<ISendEndpoint> ResolveEndpoint(object?[] args, Uri expected, CancellationToken token,
        IAdvancedSendEndpoint endpoint)
    {
        Assert.Equal(expected, args[0]);
        Assert.Equal(token, args[1]);
        return Task.FromResult<ISendEndpoint>(endpoint);
    }

    static bool TryPayload(MethodInfo method, object?[] args, MessageSchedulerContext? scheduler, TimeProvider? clock)
    {
        Type type = Assert.Single(method.GetGenericArguments());
        object? value = type == typeof(MessageSchedulerContext) ? scheduler : type == typeof(TimeProvider) ? clock : null;
        args[0] = value;
        return value is not null;
    }

    static T Strict<T>(Func<MethodInfo, object?[], object?>? handler = null) where T : class
    {
        T value = DispatchProxy.Create<T, StrictProxy>();
        ((StrictProxy)(object)value).Handler = handler;
        return value;
    }

    static void AssertArgument(string name, Action action) =>
        Assert.Equal(name, Assert.Throws<ArgumentNullException>(action).ParamName);

    static async Task AssertArgumentAsync(string name, Func<Task> action) =>
        Assert.Equal(name, (await Assert.ThrowsAsync<ArgumentNullException>(action)).ParamName);

    static InvalidOperationException Unexpected() => new("Unexpected test-double call.");

    public class StrictProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[], object?>? Handler { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Handler is { } handler ? handler(targetMethod ?? throw Unexpected(), args ?? []) : throw Unexpected();
    }

    sealed class RequestDouble
    {
        public IRequest<TestSaga, Request, Response> Request { get; set; } = null!;
        public Guid? RequestId { get; set; }
        public List<Guid?> Writes { get; } = [];
    }

    sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    public sealed class TestSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public Guid? RequestId { get; set; }
    }

    public sealed record Message;
    public sealed record Request;
    public sealed record Response(string Value);
    public sealed class MarkerException : Exception;
}
