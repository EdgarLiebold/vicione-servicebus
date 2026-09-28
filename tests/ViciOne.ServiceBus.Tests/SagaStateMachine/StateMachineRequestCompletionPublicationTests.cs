using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineRequestCompletionPublicationTests
{
    static readonly DateTimeOffset Now = new(2026, 9, 28, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "t76-request-completion-invalid-pipeline-does-not-publish")]
    public async Task Completion_RejectsMissingPipelineDependenciesBeforePublishingAsync()
    {
        var saga = new Saga { CorrelationId = Guid.NewGuid() };
        var trace = new List<string>();
        IBehaviorContext<Saga, Request> context = Context(saga, new Request(Guid.NewGuid()),
            CancellationToken.None, (_, _) =>
            {
                trace.Add("publish");
                return Task.CompletedTask;
            });
        IBehavior<Saga, Request> next = Next(context, trace);
        var original = new RequestCompletedActivity<Saga, Request>();
        var factoryCalls = 0;
        var generated = new RequestCompletedActivity<Saga, Request, Response>(
            _ =>
            {
                factoryCalls++;
                return Task.FromResult(new Response(Guid.NewGuid()));
            });

        await AssertArgumentAsync("context", () => original.ExecuteAsync(null!, next));
        await AssertArgumentAsync("next", () => original.ExecuteAsync(context, null!));
        await AssertArgumentAsync("context", () => generated.ExecuteAsync(null!, next));
        await AssertArgumentAsync("next", () => generated.ExecuteAsync(context, null!));
        Assert.Empty(trace);
        Assert.Equal(0, factoryCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "t76-request-completion-original-payload-publication-order")]
    public async Task OriginalCompletion_PublishesTheExactRequestPayloadBeforeContinuingAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var saga = new Saga { CorrelationId = Guid.NewGuid() };
        var message = new Request(Guid.NewGuid());
        var trace = new List<string>();
        var publication = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        object? envelope = null;
        IBehaviorContext<Saga, Request> context = Context(saga, message, cancellation.Token, (method, args) =>
        {
            AssertPublicationCall(method, args, cancellation.Token);
            envelope = args[0];
            trace.Add("publish");
            return publication.Task;
        });
        IBehavior<Saga, Request> next = Next(context, trace);

        Task running = new RequestCompletedActivity<Saga, Request>().ExecuteAsync(context, next);

        Assert.False(running.IsCompleted);
        Assert.Equal(["publish"], trace);
        AssertEnvelope(envelope, saga.CorrelationId, message, MessageTypeCache<Request>.MessageTypeNames);
        publication.SetResult();
        await running;
        Assert.Equal(["publish", "next"], trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "t76-request-completion-generated-response-factory-publication-order")]
    public async Task GeneratedCompletion_AwaitsTheResponseFactoryAndPublicationBeforeContinuingAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var saga = new Saga { CorrelationId = Guid.NewGuid() };
        var message = new Request(Guid.NewGuid());
        var response = new Response(Guid.NewGuid());
        var factory = new TaskCompletionSource<Response>(TaskCreationOptions.RunContinuationsAsynchronously);
        var publication = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var published = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        var trace = new List<string>();
        object? envelope = null;
        IBehaviorContext<Saga, Request> context = Context(saga, message, cancellation.Token, (method, args) =>
        {
            AssertPublicationCall(method, args, cancellation.Token);
            envelope = args[0];
            trace.Add("publish");
            published.SetResult(envelope!);
            return publication.Task;
        });
        IBehavior<Saga, Request> next = Next(context, trace);
        var activity = new RequestCompletedActivity<Saga, Request, Response>(observed =>
        {
            Assert.Same(context, observed);
            trace.Add("factory");
            return factory.Task;
        });

        Task running = activity.ExecuteAsync(context, next);
        Assert.False(running.IsCompleted);
        Assert.Equal(["factory"], trace);
        factory.SetResult(response);
        await published.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.False(running.IsCompleted);
        Assert.Equal(["factory", "publish"], trace);
        AssertEnvelope(envelope, saga.CorrelationId, response, MessageTypeCache<Response>.MessageTypeNames);
        Assert.DoesNotContain(MessageTypeCache<Request>.MessageTypeNames[0],
            Assert.IsType<string[]>(envelope!.GetType().GetProperty("PayloadType")!.GetValue(envelope)));
        publication.SetResult();
        await running;
        Assert.Equal(["factory", "publish", "next"], trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "t76-request-completion-factory-failure-never-publishes")]
    public async Task GeneratedCompletion_FactoryFailureDoesNotPublishOrContinueAsync()
    {
        var saga = new Saga { CorrelationId = Guid.NewGuid() };
        var trace = new List<string>();
        IBehaviorContext<Saga, Request> context = Context(saga, new Request(Guid.NewGuid()),
            CancellationToken.None, (_, _) =>
            {
                trace.Add("publish");
                return Task.CompletedTask;
            });
        IBehavior<Saga, Request> next = Next(context, trace);
        var failure = new InvalidOperationException("response creation failed");
        var activity = new RequestCompletedActivity<Saga, Request, Response>(_ => Task.FromException<Response>(failure));

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => activity.ExecuteAsync(context, next)));
        Assert.Empty(trace);

        activity = new RequestCompletedActivity<Saga, Request, Response>(_ => Task.FromResult<Response>(null!));
        InvalidOperationException nullResponse = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            activity.ExecuteAsync(context, next));
        Assert.Equal("The response factory returned a null response.", nullResponse.Message);
        Assert.Empty(trace);

        activity = new RequestCompletedActivity<Saga, Request, Response>(_ => null!);
        InvalidOperationException nullTask = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            activity.ExecuteAsync(context, next));
        Assert.Equal("The response factory returned a null task.", nullTask.Message);
        Assert.Empty(trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "t76-request-completion-publication-failure-skips-continuation")]
    public async Task Completion_PublicationFailureRetainsTheCauseAndSkipsContinuationAsync()
    {
        var saga = new Saga { CorrelationId = Guid.NewGuid() };
        var trace = new List<string>();
        var failure = new InvalidOperationException("completion transport failed");
        IBehaviorContext<Saga, Request> context = Context(saga, new Request(Guid.NewGuid()),
            CancellationToken.None, (method, args) =>
            {
                AssertPublicationCall(method, args, CancellationToken.None);
                trace.Add("publish");
                return Task.FromException(failure);
            });
        IBehavior<Saga, Request> next = Next(context, trace);
        var original = new RequestCompletedActivity<Saga, Request>();
        var generated = new RequestCompletedActivity<Saga, Request, Response>(
            _ => Task.FromResult(new Response(Guid.NewGuid())));

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => original.ExecuteAsync(context, next)));
        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => generated.ExecuteAsync(context, next)));
        Assert.Equal(["publish", "publish"], trace);
    }

    static IBehaviorContext<Saga, Request> Context(Saga saga, Request message, CancellationToken token,
        Func<MethodInfo, object?[], object?> publish) => Proxy<IBehaviorContext<Saga, Request>>((method, args) => method.Name switch
        {
            "get_Saga" => saga,
            "get_Message" => message,
            "get_CancellationToken" => token,
            "TryGetPayload" => TimePayload(method, args),
            "PublishAsync" => publish(method, args),
            _ => throw new NotSupportedException(method.Name)
        });

    static bool TimePayload(MethodInfo method, object?[] args)
    {
        Assert.Equal(typeof(TimeProvider), Assert.Single(method.GetGenericArguments()));
        args[0] = new FixedClock();
        return true;
    }

    static IBehavior<Saga, Request> Next(IBehaviorContext<Saga, Request> context, List<string> trace) =>
        Proxy<IBehavior<Saga, Request>>((method, args) =>
        {
            Assert.Equal("ExecuteAsync", method.Name);
            Assert.Same(context, Assert.Single(args));
            trace.Add("next");
            return Task.CompletedTask;
        });

    static void AssertPublicationCall(MethodInfo method, object?[] args, CancellationToken token)
    {
        Assert.Equal("PublishAsync", method.Name);
        Assert.Equal(typeof(IRequestCompleted), Assert.Single(method.GetGenericArguments()));
        Assert.Equal(2, args.Length);
        Assert.Equal(token, args[1]);
    }

    static async Task AssertArgumentAsync(string name, Func<Task> action) =>
        Assert.Equal(name, (await Assert.ThrowsAsync<ArgumentNullException>(action)).ParamName);

    static void AssertEnvelope(object? envelope, Guid correlationId, object payload, IReadOnlyList<string> payloadTypes)
    {
        Assert.NotNull(envelope);
        object value = envelope;
        Type type = value.GetType();
        Assert.Equal(correlationId, Assert.IsType<Guid>(type.GetProperty("CorrelationId")!.GetValue(value)));
        Assert.Equal(Now, Assert.IsType<DateTimeOffset>(type.GetProperty("Timestamp")!.GetValue(value)));
        Assert.Same(payload, type.GetProperty("Payload")!.GetValue(value));
        Assert.Equal(payloadTypes, Assert.IsType<string[]>(type.GetProperty("PayloadType")!.GetValue(value)));
    }

    static T Proxy<T>(Func<MethodInfo, object?[], object?> handler) where T : class
    {
        T value = DispatchProxy.Create<T, CallProxy>();
        ((CallProxy)(object)value).Handler = handler;
        return value;
    }

    public class CallProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[], object?> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args) =>
            Handler(method ?? throw new InvalidOperationException("Missing method."), args ?? []);
    }

    sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    public sealed class Saga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed record Request(Guid Id);
    public sealed record Response(Guid Id);
}
