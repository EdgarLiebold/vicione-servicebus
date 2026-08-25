using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class FilterObserverTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-FILTER-OBSERVERS", "typed-and-untyped-success-order")]
    public async Task Observers_ReceiveTheSameContextInExactSuccessOrder()
    {
        var trace = new List<string>();
        var router = new PipeRouter();
        CommandContext<SetConcurrencyLimit>? bodyContext = null;
        router.ConnectPipe(Pipe.Execute<CommandContext<SetConcurrencyLimit>>(context =>
        {
            bodyContext = context;
            trace.Add("body");
        }));
        var typed = new TypedObserver(trace);
        var untyped = new UntypedObserver(trace);
        var observerConnector = (IFilterObserverConnector)router;
        observerConnector.ConnectObserver(typed);
        observerConnector.ConnectObserver(untyped);

        await router.SetConcurrencyLimit(32);

        Assert.Equal(["typed-pre", "untyped-pre", "body", "typed-post", "untyped-post"], trace);
        Assert.Same(bodyContext, typed.Context);
        Assert.Same(bodyContext, untyped.Context);
        Assert.Null(typed.Failure);
        Assert.Null(untyped.Failure);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FILTER-OBSERVERS", "typed-and-untyped-fault-order")]
    public async Task Observers_ReceiveTheExactFailureAndNeverReportPostSend()
    {
        var trace = new List<string>();
        var expected = new DispatchException("dispatch failed");
        var router = new PipeRouter();
        CommandContext<SetConcurrencyLimit>? bodyContext = null;
        router.ConnectPipe(Pipe.Execute<CommandContext<SetConcurrencyLimit>>(context =>
        {
            bodyContext = context;
            trace.Add("body");
            throw expected;
        }));
        var typed = new TypedObserver(trace);
        var untyped = new UntypedObserver(trace);
        var observerConnector = (IFilterObserverConnector)router;
        observerConnector.ConnectObserver(typed);
        observerConnector.ConnectObserver(untyped);

        DispatchException actual = await Assert.ThrowsAsync<DispatchException>(() => router.SetConcurrencyLimit(32));

        Assert.Equal(["typed-pre", "untyped-pre", "body", "typed-fault", "untyped-fault"], trace);
        Assert.Same(expected, actual);
        Assert.Same(expected, typed.Failure);
        Assert.Same(expected, untyped.Failure);
        Assert.Same(bodyContext, typed.Context);
        Assert.Same(bodyContext, untyped.Context);
    }

    private sealed class TypedObserver(List<string> trace) : IFilterObserver<CommandContext<SetConcurrencyLimit>>
    {
        public CommandContext<SetConcurrencyLimit>? Context { get; private set; }

        public Exception? Failure { get; private set; }

        public Task PreSend(CommandContext<SetConcurrencyLimit> context)
        {
            Context = context;
            trace.Add("typed-pre");
            return Task.CompletedTask;
        }

        public Task PostSend(CommandContext<SetConcurrencyLimit> context)
        {
            Assert.Same(Context, context);
            trace.Add("typed-post");
            return Task.CompletedTask;
        }

        public Task SendFault(CommandContext<SetConcurrencyLimit> context, Exception exception)
        {
            Assert.Same(Context, context);
            Failure = exception;
            trace.Add("typed-fault");
            return Task.CompletedTask;
        }
    }

    private sealed class UntypedObserver(List<string> trace) : IFilterObserver
    {
        public CommandContext? Context { get; private set; }

        public Exception? Failure { get; private set; }

        public Task PreSend<T>(T context)
            where T : class, PipeContext
        {
            Context = Assert.IsAssignableFrom<CommandContext>(context);
            trace.Add("untyped-pre");
            return Task.CompletedTask;
        }

        public Task PostSend<T>(T context)
            where T : class, PipeContext
        {
            Assert.Same(Context, context);
            trace.Add("untyped-post");
            return Task.CompletedTask;
        }

        public Task SendFault<T>(T context, Exception exception)
            where T : class, PipeContext
        {
            Assert.Same(Context, context);
            Failure = exception;
            trace.Add("untyped-fault");
            return Task.CompletedTask;
        }
    }

    private sealed class DispatchException(string message) : Exception(message);
}
