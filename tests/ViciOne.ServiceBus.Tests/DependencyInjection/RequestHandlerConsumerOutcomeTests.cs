using System.Reflection;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class RequestHandlerConsumerOutcomeTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    [RequirementCoverage("REQ-VSB-DI-HANDLER", "t96-request-handler-awaits-handler-and-response-across-all-signatures")]
    public async Task Consumer_AwaitsHandlerThenExactResponseAcrossEverySignatureAsync(int dependencies, bool messageOnly)
    {
        var request = new Request(Guid.NewGuid());
        var response = new Response(Guid.NewGuid());
        var handlerCompletion = new TaskCompletionSource<Response>(TaskCreationOptions.RunContinuationsAsynchronously);
        var responseCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var responseStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = new List<string>();
        int responseCalls = 0;
        ConsumeContext<Request> context = Context(request, (method, args) =>
        {
            Assert.Equal("RespondAsync", method.Name);
            Assert.Equal(typeof(Response), Assert.Single(method.GetGenericArguments()));
            Assert.Same(response, Assert.Single(args));
            calls.Add("respond");
            responseCalls++;
            responseStarted.SetResult();
            return responseCompletion.Task;
        });
        IConsumer<Request> consumer = Consumer(dependencies, messageOnly, context, request,
            () => { calls.Add("handler"); return handlerCompletion.Task; });

        Task running = consumer.ConsumeAsync(context);
        Assert.False(running.IsCompleted);
        Assert.Equal(["handler"], calls);
        Assert.Equal(0, responseCalls);

        handlerCompletion.SetResult(response);
        await responseStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.False(running.IsCompleted);
        Assert.Equal(["handler", "respond"], calls);
        Assert.Equal(1, responseCalls);

        responseCompletion.SetResult();
        await running;
        Assert.Equal(["handler", "respond"], calls);
        Assert.Equal(1, responseCalls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [RequirementCoverage("REQ-VSB-DI-HANDLER", "t96-request-handler-null-response-sends-nothing")]
    public async Task Consumer_NullHandlerResponseDoesNotSendAcrossEveryDependencyCountAsync(int dependencies)
    {
        var request = new Request(Guid.NewGuid());
        var calls = new List<string>();
        ConsumeContext<Request> context = Context(request, (method, _) =>
        {
            calls.Add(method.Name);
            throw new InvalidOperationException("A null response must not be sent.");
        });
        IConsumer<Request> consumer = Consumer(dependencies, false, context, request,
            () => { calls.Add("handler"); return Task.FromResult<Response>(null!); });

        await consumer.ConsumeAsync(context);

        Assert.Equal(["handler"], calls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [RequirementCoverage("REQ-VSB-DI-HANDLER", "t96-request-handler-failure-preserves-cause-and-skips-response")]
    public async Task Consumer_HandlerFailurePreservesCauseAndNeverRespondsAsync(int dependencies)
    {
        var request = new Request(Guid.NewGuid());
        var failure = new InvalidOperationException("handler failed");
        int responseCalls = 0;
        ConsumeContext<Request> context = Context(request, (_, _) =>
        {
            responseCalls++;
            return Task.CompletedTask;
        });
        IConsumer<Request> consumer = Consumer(dependencies, false, context, request,
            () => Task.FromException<Response>(failure));

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.ConsumeAsync(context)));
        Assert.Equal(0, responseCalls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [RequirementCoverage("REQ-VSB-DI-HANDLER", "t96-request-handler-response-failure-preserves-cause-once")]
    public async Task Consumer_ResponseFailurePreservesCauseAfterOneSendAttemptAsync(int dependencies)
    {
        var request = new Request(Guid.NewGuid());
        var response = new Response(Guid.NewGuid());
        var failure = new InvalidOperationException("response delivery failed");
        int responseCalls = 0;
        ConsumeContext<Request> context = Context(request, (method, args) =>
        {
            Assert.Equal("RespondAsync", method.Name);
            Assert.Equal(typeof(Response), Assert.Single(method.GetGenericArguments()));
            Assert.Same(response, Assert.Single(args));
            responseCalls++;
            return Task.FromException(failure);
        });
        IConsumer<Request> consumer = Consumer(dependencies, false, context, request,
            () => Task.FromResult(response));

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.ConsumeAsync(context)));
        Assert.Equal(1, responseCalls);
    }

    private static IConsumer<Request> Consumer(int count, bool messageOnly, ConsumeContext<Request> expectedContext,
        Request expectedRequest, Func<Task<Response>> execute)
    {
        var first = new Dependency("first");
        var second = new Dependency("second");
        var third = new Dependency("third");
        Task<Response> Invoke(ConsumeContext<Request> context, params Dependency[] dependencies)
        {
            Assert.Same(expectedContext, context);
            Assert.Equal(Enumerable.Range(0, count).Select(index => new[] { first, second, third }[index]), dependencies);
            return execute();
        }
        Task<Response> InvokeMessage(Request message, params Dependency[] dependencies)
        {
            Assert.Same(expectedRequest, message);
            Assert.Equal(Enumerable.Range(0, count).Select(index => new[] { first, second, third }[index]), dependencies);
            return execute();
        }

        return (count, messageOnly) switch
        {
            (0, false) => new RequestHandlerConsumer<Request, Response>(
                new RequestHandlerMethod<Request, Response>((ConsumeContext<Request> context) => Invoke(context))),
            (0, true) => new RequestHandlerConsumer<Request, Response>(
                new RequestHandlerMethod<Request, Response>((Request message) => InvokeMessage(message))),
            (1, false) => new RequestHandlerConsumer<Request, Dependency, Response>(
                new RequestHandlerMethod<Request, Dependency, Response>(
                    (ConsumeContext<Request> context, Dependency arg1) => Invoke(context, arg1)), first),
            (1, true) => new RequestHandlerConsumer<Request, Dependency, Response>(
                new RequestHandlerMethod<Request, Dependency, Response>(
                    (Request message, Dependency arg1) => InvokeMessage(message, arg1)), first),
            (2, false) => new RequestHandlerConsumer<Request, Dependency, Dependency, Response>(
                new RequestHandlerMethod<Request, Dependency, Dependency, Response>(
                    (ConsumeContext<Request> context, Dependency arg1, Dependency arg2) => Invoke(context, arg1, arg2)), first, second),
            (2, true) => new RequestHandlerConsumer<Request, Dependency, Dependency, Response>(
                new RequestHandlerMethod<Request, Dependency, Dependency, Response>(
                    (Request message, Dependency arg1, Dependency arg2) => InvokeMessage(message, arg1, arg2)), first, second),
            (3, false) => new RequestHandlerConsumer<Request, Dependency, Dependency, Dependency, Response>(
                new RequestHandlerMethod<Request, Dependency, Dependency, Dependency, Response>(
                    (ConsumeContext<Request> context, Dependency arg1, Dependency arg2, Dependency arg3) =>
                        Invoke(context, arg1, arg2, arg3)), first, second, third),
            (3, true) => new RequestHandlerConsumer<Request, Dependency, Dependency, Dependency, Response>(
                new RequestHandlerMethod<Request, Dependency, Dependency, Dependency, Response>(
                    (Request message, Dependency arg1, Dependency arg2, Dependency arg3) =>
                        InvokeMessage(message, arg1, arg2, arg3)), first, second, third),
            _ => throw new ArgumentOutOfRangeException(nameof(count)),
        };
    }

    private static ConsumeContext<Request> Context(Request request, Func<MethodInfo, object?[], object?> respond)
    {
        ConsumeContext<Request> context = DispatchProxy.Create<ConsumeContext<Request>, CallProxy>();
        ((CallProxy)(object)context).Handler = (method, args) => method.Name switch
        {
            "get_Message" => request,
            "RespondAsync" => respond(method, args),
            _ => throw new NotSupportedException(method.Name),
        };
        return context;
    }

    public class CallProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[], object?> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args) =>
            Handler(method ?? throw new InvalidOperationException("Missing method."), args ?? []);
    }

    public sealed record Dependency(string Name);
    public sealed record Request(Guid Id);
    public sealed record Response(Guid Id);
}
