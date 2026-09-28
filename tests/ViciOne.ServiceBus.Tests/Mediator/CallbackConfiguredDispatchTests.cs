using System.Collections.Concurrent;
using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Mediator;

public sealed class CallbackConfiguredDispatchTests
{
    private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "async-send-callback-completes-before-delivery")]
    public async Task DirectSend_AwaitsAsyncConfigurationAndDeliversItsHeaderAsync()
    {
        var received = NewSignal<Snapshot>();
        await using IMediator mediator = MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.Handler<DispatchMessage>(context =>
            {
                received.TrySetResult(new Snapshot(context.Message.Value, context.Headers.Get<string>("route")));
                return Task.CompletedTask;
            });
        });
        var entered = NewSignal();
        var release = NewSignal();
        var message = new DispatchMessage("direct");

        Task send = mediator.SendAsync(message, async context =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(TestContext.Current.CancellationToken);
            context.Headers.Set("route", "direct-configured");
        }, TestContext.Current.CancellationToken);

        try
        {
            await entered.Task.WaitAsync(CompletionTimeout, TestContext.Current.CancellationToken);
            Assert.False(send.IsCompleted);
            Assert.False(received.Task.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
        }

        await send.WaitAsync(CompletionTimeout, TestContext.Current.CancellationToken);
        Assert.Equal(new Snapshot("direct", "direct-configured"),
            await received.Task.WaitAsync(CompletionTimeout, TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "runtime-publish-callback-failure-and-recovery")]
    public async Task RuntimePublish_FailedCallbackPreventsDeliveryAndNextPublishRecoversAsync()
    {
        var received = new ConcurrentQueue<Snapshot>();
        var concreteReceived = new ConcurrentQueue<Snapshot>();
        await using IMediator mediator = MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.Handler<IDispatchContract>(context =>
            {
                received.Enqueue(new Snapshot(context.Message.Value, context.Headers.Get<string>("route")));
                return Task.CompletedTask;
            });
            configuration.Handler<DispatchMessage>(context =>
            {
                concreteReceived.Enqueue(new Snapshot(context.Message.Value, context.Headers.Get<string>("route")));
                return Task.CompletedTask;
            });
        });
        var failure = new InvalidOperationException("publish configuration rejected");

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.PublishAsync((object)new DispatchMessage("rejected"), typeof(IDispatchContract),
                (Func<PublishContext, Task>)(_ => Task.FromException(failure)),
                TestContext.Current.CancellationToken));
        Assert.Same(failure, actual);
        Assert.Empty(received);
        Assert.Empty(concreteReceived);

        await mediator.PublishAsync((object)new DispatchMessage("accepted"), typeof(IDispatchContract),
            (Func<PublishContext, Task>)(context =>
            {
                context.Headers.Set("route", "runtime-configured");
                return Task.CompletedTask;
            }), TestContext.Current.CancellationToken);

        Assert.Equal([new Snapshot("accepted", "runtime-configured")], received.ToArray());
        Assert.Equal([new Snapshot("accepted", "runtime-configured")], concreteReceived.ToArray());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "runtime-publish-preserves-explicit-interface-contract")]
    public async Task RuntimePublish_ForwardsExplicitInterfaceContractAndOriginalMessageOnceAsync()
    {
        IPublishEndpoint endpoint = DispatchProxy.Create<IAdvancedPublishEndpoint, RecordingPublishEndpoint>();
        var recorder = (RecordingPublishEndpoint)(object)endpoint;
        var message = new DispatchMessage("explicit");
        using var cancellation = new CancellationTokenSource();

        await PublishExecuteExtensions.PublishAsync(endpoint, (object)message, typeof(IDispatchContract),
            (Func<PublishContext, Task>)(_ => Task.CompletedTask), cancellation.Token);

        (MethodInfo method, object?[] arguments) = Assert.Single(recorder.Invocations);
        Assert.Equal(nameof(IPublishEndpoint.PublishAsync), method.Name);
        Assert.Equal(
            [typeof(object), typeof(Type), typeof(IPipe<PublishContext>), typeof(CancellationToken)],
            method.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Same(message, arguments[0]);
        Assert.Same(typeof(IDispatchContract), arguments[1]);
        Assert.IsAssignableFrom<IPipe<PublishContext>>(arguments[2]);
        Assert.Equal(cancellation.Token, arguments[3]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-SEND", "consume-scope-callback-configures-only-forwarded-message")]
    public async Task ConsumeScopeSend_ConfiguresForwardedMessageWithoutLeakingHeaderToNextSendAsync()
    {
        var received = new ConcurrentQueue<Snapshot>();
        var destination = new Uri("loopback://localhost/callback-forwarded");
        await using IMediator mediator = MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.Handler<TriggerMessage>(context =>
                context.Advanced().SendAsync(destination, new DispatchMessage("forwarded"),
                    (Action<SendContext<DispatchMessage>>)(send => send.Headers.Set("route", "from-consume"))));
            configuration.Handler<DispatchMessage>(context =>
            {
                received.Enqueue(new Snapshot(context.Message.Value, context.Headers.Get<string>("route")));
                return Task.CompletedTask;
            });
        });

        await mediator.SendAsync(new TriggerMessage(), TestContext.Current.CancellationToken);
        await mediator.SendAsync(new DispatchMessage("plain"), TestContext.Current.CancellationToken);

        Assert.Equal(
            [new Snapshot("forwarded", "from-consume"), new Snapshot("plain", null)],
            received.ToArray());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-REQUEST", "async-response-callback-completes-before-response")]
    public async Task Response_AwaitsAsyncConfigurationAndExposesResponseHeaderAsync()
    {
        var entered = NewSignal();
        var release = NewSignal();
        await using IMediator mediator = MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.Handler<RequestMessage>(context =>
                context.Advanced().RespondAsync(new ReplyMessage(context.Message.Value), async send =>
                {
                    entered.TrySetResult();
                    await release.Task.WaitAsync(TestContext.Current.CancellationToken);
                    send.Headers.Set("route", "response-configured");
                }));
        });
        IRequestClient<RequestMessage> client = mediator.CreateRequestClient<RequestMessage>(
            new RequestTimeout(TimeSpan.FromMinutes(1)));
        Task<Response<ReplyMessage>> response = client.GetResponseAsync<ReplyMessage>(
            new RequestMessage("requested"), TestContext.Current.CancellationToken);

        try
        {
            await entered.Task.WaitAsync(CompletionTimeout, TestContext.Current.CancellationToken);
            Assert.False(response.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
        }

        Response<ReplyMessage> reply = await response.WaitAsync(CompletionTimeout, TestContext.Current.CancellationToken);
        Assert.Equal("requested", reply.Message.Value);
        Assert.Equal("response-configured", reply.Headers.Get<string>("route"));
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TaskCompletionSource<T> NewSignal<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed record Snapshot(string Value, string? Route);
    private interface IDispatchContract
    {
        string Value { get; }
    }

    private sealed record DispatchMessage(string Value) : IDispatchContract;
    private sealed record TriggerMessage;
    private sealed record RequestMessage(string Value);
    private sealed record ReplyMessage(string Value);

    private class RecordingPublishEndpoint : DispatchProxy
    {
        public List<(MethodInfo Method, object?[] Arguments)> Invocations { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            Invocations.Add((targetMethod, [.. args!]));
            return Task.CompletedTask;
        }
    }
}
