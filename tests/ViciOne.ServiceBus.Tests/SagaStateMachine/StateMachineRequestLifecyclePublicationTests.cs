using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineRequestLifecyclePublicationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "t95-request-lifecycle-invalid-pipeline-has-no-publication")]
    public async Task StartedAndFaulted_RejectInvalidPipelineBeforePublishingAsync()
    {
        var saga = new Saga { CorrelationId = Guid.NewGuid() };
        var request = new Request(Guid.NewGuid());
        var trace = new List<string>();
        IBehaviorContext<Saga, Request> startedContext = Context(saga, request, CancellationToken.None,
            (method, _) => method.Name switch
            {
                "get_RequestId" => Guid.NewGuid(),
                "get_ResponseAddress" or "get_FaultAddress" => new Uri("loopback://request-route"),
                "get_ExpirationTime" => null,
                "PublishAsync" => RecordPublicationAsync(trace),
                _ => throw new NotSupportedException(method.Name),
            });
        Fault<Request> fault = Proxy<Fault<Request>>((method, _) => method.Name switch
        {
            "get_FaultId" => Guid.NewGuid(),
            "get_FaultedMessageId" => null,
            "get_Timestamp" => DateTimeOffset.UtcNow,
            "get_Host" => null,
            "get_Exceptions" => Array.Empty<ExceptionInfo>(),
            _ => throw new NotSupportedException(method.Name),
        });
        IBehaviorContext<Saga, Fault<Request>> faultedContext = Context(saga, fault, CancellationToken.None,
            (method, _) => method.Name == "PublishAsync" ? RecordPublicationAsync(trace) : throw new NotSupportedException(method.Name));
        var started = new RequestStartedActivity<Saga, Request>();
        var faulted = new RequestFaultedActivity<Saga, Fault<Request>, Request>();

        await AssertArgumentAsync("context", () => started.ExecuteAsync(null!, Next(startedContext, trace)));
        await AssertArgumentAsync("next", () => started.ExecuteAsync(startedContext, null!));
        await AssertArgumentAsync("context", () => faulted.ExecuteAsync(null!, Next(faultedContext, trace)));
        await AssertArgumentAsync("next", () => faulted.ExecuteAsync(faultedContext, null!));
        Assert.Empty(trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "t95-request-started-preserves-owner-route-and-payload-before-continuation")]
    public async Task Started_PublishesOwnerRouteAndOriginalPayloadBeforeContinuingAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var saga = new Saga { CorrelationId = Guid.NewGuid() };
        var request = new Request(Guid.NewGuid());
        Guid requestId = Guid.NewGuid();
        var responseAddress = new Uri("loopback://request-response");
        var faultAddress = new Uri("loopback://request-fault");
        DateTimeOffset expiration = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
        var trace = new List<string>();
        var publication = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        object? envelope = null;
        IBehaviorContext<Saga, Request> context = Context(saga, request, cancellation.Token,
            (method, args) => method.Name switch
            {
                "get_RequestId" => requestId,
                "get_ResponseAddress" => responseAddress,
                "get_FaultAddress" => faultAddress,
                "get_ExpirationTime" => expiration,
                "PublishAsync" => Publish(method, args, typeof(IRequestStarted), cancellation.Token,
                    () => { envelope = args[0]; trace.Add("publish"); return publication.Task; }),
                _ => throw new NotSupportedException(method.Name),
            });
        IBehavior<Saga, Request> next = Next(context, trace);

        Task running = new RequestStartedActivity<Saga, Request>().ExecuteAsync(context, next);

        Assert.False(running.IsCompleted);
        Assert.Equal(["publish"], trace);
        Assert.Equal(saga.CorrelationId, Property<Guid>(envelope, "CorrelationId"));
        Assert.Equal(requestId, Property<Guid>(envelope, "RequestId"));
        Assert.Same(responseAddress, Property<Uri>(envelope, "ResponseAddress"));
        Assert.Same(faultAddress, Property<Uri>(envelope, "FaultAddress"));
        Assert.Equal(expiration, Property<DateTimeOffset?>(envelope, "ExpirationTime"));
        Assert.Equal(MessageTypeCache<Request>.MessageTypeNames, Property<string[]>(envelope, "PayloadType"));
        Assert.Same(request, Property<object>(envelope, "Payload"));
        publication.SetResult();
        await running;
        Assert.Equal(["publish", "next"], trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "t95-request-started-publication-failure-skips-continuation")]
    public async Task Started_PublicationFailurePreservesCauseAndSkipsContinuationAsync()
    {
        var saga = new Saga { CorrelationId = Guid.NewGuid() };
        var failure = new InvalidOperationException("request tracking unavailable");
        var trace = new List<string>();
        IBehaviorContext<Saga, Request> context = Context(saga, new Request(Guid.NewGuid()), CancellationToken.None,
            (method, args) => method.Name switch
            {
                "get_RequestId" => Guid.NewGuid(),
                "get_ResponseAddress" or "get_FaultAddress" => new Uri("loopback://request-route"),
                "get_ExpirationTime" => null,
                "PublishAsync" => Publish(method, args, typeof(IRequestStarted), CancellationToken.None,
                    () => { trace.Add("publish"); return Task.FromException(failure); }),
                _ => throw new NotSupportedException(method.Name),
            });

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new RequestStartedActivity<Saga, Request>().ExecuteAsync(context, Next(context, trace))));
        Assert.Equal(["publish"], trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "t95-request-faulted-preserves-structured-cause-before-continuation")]
    public async Task Faulted_PublishesStructuredCauseAndOriginalOwnerBeforeContinuingAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var saga = new Saga { CorrelationId = Guid.NewGuid() };
        Guid faultId = Guid.NewGuid();
        Guid failedMessageId = Guid.NewGuid();
        DateTimeOffset timestamp = new(2026, 10, 2, 11, 30, 0, TimeSpan.Zero);
        HostInfo host = Proxy<HostInfo>((method, _) => method.Name == "get_MachineName" ? "node-a" : null);
        ExceptionInfo exception = Proxy<ExceptionInfo>((method, _) => method.Name == "get_Message" ? "broken" : null);
        ExceptionInfo[] exceptions = [exception];
        Fault<Request> fault = Proxy<Fault<Request>>((method, _) => method.Name switch
        {
            "get_FaultId" => faultId,
            "get_FaultedMessageId" => failedMessageId,
            "get_Timestamp" => timestamp,
            "get_Host" => host,
            "get_Exceptions" => exceptions,
            _ => throw new NotSupportedException(method.Name),
        });
        var trace = new List<string>();
        var publication = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        object? envelope = null;
        IBehaviorContext<Saga, Fault<Request>> context = Context(saga, fault, cancellation.Token,
            (method, args) => method.Name == "PublishAsync"
                ? Publish(method, args, typeof(IRequestFaulted), cancellation.Token,
                    () => { envelope = args[0]; trace.Add("publish"); return publication.Task; })
                : throw new NotSupportedException(method.Name));

        Task running = new RequestFaultedActivity<Saga, Fault<Request>, Request>().ExecuteAsync(
            context, Next(context, trace));

        Assert.False(running.IsCompleted);
        Assert.Equal(["publish"], trace);
        Assert.Equal(saga.CorrelationId, Property<Guid>(envelope, "CorrelationId"));
        Assert.Equal(MessageTypeCache<Fault<Request>>.MessageTypeNames, Property<string[]>(envelope, "PayloadType"));
        object payload = Property<object>(envelope, "Payload");
        Assert.Equal(faultId, Property<Guid>(payload, "FaultId"));
        Assert.Equal(failedMessageId, Property<Guid?>(payload, "FaultedMessageId"));
        Assert.Equal(timestamp, Property<DateTimeOffset>(payload, "Timestamp"));
        Assert.Same(host, Property<HostInfo>(payload, "Host"));
        Assert.Same(exceptions, Property<ExceptionInfo[]>(payload, "Exceptions"));
        publication.SetResult();
        await running;
        Assert.Equal(["publish", "next"], trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "t95-request-faulted-rejects-non-fault-without-publication")]
    public async Task Faulted_RejectsNonFaultMessageBeforePublicationOrContinuationAsync()
    {
        var saga = new Saga { CorrelationId = Guid.NewGuid() };
        var trace = new List<string>();
        IBehaviorContext<Saga, Request> context = Context(saga, new Request(Guid.NewGuid()), CancellationToken.None,
            (method, _) => { trace.Add(method.Name); throw new NotSupportedException(method.Name); });

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new RequestFaultedActivity<Saga, Request, Request>().ExecuteAsync(context, Next(context, trace)));
        Assert.Contains(nameof(Fault), failure.Message, StringComparison.Ordinal);
        Assert.Empty(trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "t95-request-faulted-publication-failure-skips-continuation")]
    public async Task Faulted_PublicationFailurePreservesCauseAndSkipsContinuationAsync()
    {
        var saga = new Saga { CorrelationId = Guid.NewGuid() };
        Fault<Request> fault = Proxy<Fault<Request>>((method, _) => method.Name switch
        {
            "get_FaultId" => Guid.NewGuid(),
            "get_FaultedMessageId" => null,
            "get_Timestamp" => DateTimeOffset.UtcNow,
            "get_Host" => null,
            "get_Exceptions" => Array.Empty<ExceptionInfo>(),
            _ => throw new NotSupportedException(method.Name),
        });
        var failure = new InvalidOperationException("fault tracking unavailable");
        var trace = new List<string>();
        IBehaviorContext<Saga, Fault<Request>> context = Context(saga, fault, CancellationToken.None,
            (method, args) => method.Name == "PublishAsync"
                ? Publish(method, args, typeof(IRequestFaulted), CancellationToken.None,
                    () => { trace.Add("publish"); return Task.FromException(failure); })
                : throw new NotSupportedException(method.Name));

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new RequestFaultedActivity<Saga, Fault<Request>, Request>().ExecuteAsync(context, Next(context, trace))));
        Assert.Equal(["publish"], trace);
    }

    private static T Property<T>(object? owner, string name)
    {
        Assert.NotNull(owner);
        object? value = owner.GetType().GetProperty(name)!.GetValue(owner);
        Assert.NotNull(value);
        return (T)value;
    }

    private static Task RecordPublicationAsync(List<string> trace)
    {
        trace.Add("publish");
        return Task.CompletedTask;
    }

    private static async Task AssertArgumentAsync(string name, Func<Task> action) =>
        Assert.Equal(name, (await Assert.ThrowsAsync<ArgumentNullException>(action)).ParamName);

    private static object Publish(MethodInfo method, object?[] args, Type contract, CancellationToken token, Func<Task> action)
    {
        Assert.Equal("PublishAsync", method.Name);
        Assert.Equal(contract, Assert.Single(method.GetGenericArguments()));
        Assert.Equal(2, args.Length);
        Assert.Equal(token, args[1]);
        return action();
    }

    private static IBehaviorContext<Saga, TMessage> Context<TMessage>(Saga saga, TMessage message, CancellationToken token,
        Func<MethodInfo, object?[], object?> additional) where TMessage : class =>
        Proxy<IBehaviorContext<Saga, TMessage>>((method, args) => method.Name switch
        {
            "get_Saga" => saga,
            "get_Message" => message,
            "get_CancellationToken" => token,
            _ => additional(method, args),
        });

    private static IBehavior<Saga, TMessage> Next<TMessage>(IBehaviorContext<Saga, TMessage> context, List<string> trace)
        where TMessage : class => Proxy<IBehavior<Saga, TMessage>>((method, args) =>
        {
            Assert.Equal("ExecuteAsync", method.Name);
            Assert.Same(context, Assert.Single(args));
            trace.Add("next");
            return Task.CompletedTask;
        });

    private static T Proxy<T>(Func<MethodInfo, object?[], object?> handler) where T : class
    {
        T proxy = DispatchProxy.Create<T, CallProxy>();
        ((CallProxy)(object)proxy).Handler = handler;
        return proxy;
    }

    public class CallProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[], object?> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args) =>
            Handler(method ?? throw new InvalidOperationException("Missing method."), args ?? []);
    }

    public sealed class Saga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed record Request(Guid Id);
}
