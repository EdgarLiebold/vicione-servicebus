using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Advanced.Contexts;

public sealed class SendConsumeCallbackExecutionTests
{
    private static readonly Uri Destination = new("loopback://localhost/callback-destination");

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [RequirementCoverage("REQ-VSB-CONSUME-SEND", "all-callback-forms-configure-the-accepted-context")]
    public async Task EveryCallbackForm_ConfiguresTheAcceptedContextExactlyOnceAsync(int form)
    {
        var fixture = new SendFixture();
        var message = new ProbeMessage("original");
        object values = new { Value = "initialized" };
        Guid correlationId = Guid.NewGuid();
        int calls = 0;
        SendContext? callbackContext = null;
        CancellationToken token = TestContext.Current.CancellationToken;

        void Configure(SendContext context)
        {
            calls++;
            callbackContext = context;
            context.CorrelationId = correlationId;
        }

        Task ConfigureAsync(SendContext context)
        {
            Configure(context);
            return Task.CompletedTask;
        }

        await SendFormAsync(form, fixture.Consume, message, values,
            context => Configure(context), context => ConfigureAsync(context),
            Configure, ConfigureAsync, token);

        Assert.Equal(1, calls);
        Assert.Same(fixture.SendContext, callbackContext);
        Assert.Equal(correlationId, fixture.ContextProxy.CorrelationId);
        Assert.Equal(1, fixture.EndpointProxy.AcceptedCount);
        Assert.Equal(Destination, Assert.Single(fixture.ConsumeProxy.Addresses));
        Assert.Equal(token, Assert.Single(fixture.ConsumeProxy.ResolutionTokens));
        EndpointInvocation invocation = Assert.Single(fixture.EndpointProxy.Invocations);
        Assert.Equal(token, invocation.CancellationToken);
        Assert.Same(form is 6 or 7 ? values : message, invocation.Arguments[0]);
        Assert.Equal(form is 0 or 1 or 6 or 7, invocation.Method.IsGenericMethod);
        if (invocation.Method.IsGenericMethod)
            Assert.Equal(typeof(ProbeMessage), Assert.Single(invocation.Method.GetGenericArguments()));
        Type pipeType = form is 0 or 1 or 6 or 7
            ? typeof(IPipe<SendContext<ProbeMessage>>)
            : typeof(IPipe<SendContext>);
        Type firstParameterType = form is 0 or 1 ? typeof(ProbeMessage) : typeof(object);
        Type[] expectedParameters = form is 4 or 5
            ? [typeof(object), typeof(Type), pipeType, typeof(CancellationToken)]
            : [firstParameterType, pipeType, typeof(CancellationToken)];
        Assert.Equal(expectedParameters, invocation.Method.GetParameters().Select(parameter => parameter.ParameterType));
        if (form is 4 or 5)
            Assert.Same(typeof(ProbeMessage), invocation.Arguments[1]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    [RequirementCoverage("REQ-VSB-CONSUME-SEND", "async-callback-must-finish-before-transport-acceptance")]
    public async Task AsyncCallback_IsAwaitedBeforeTheEndpointAcceptsTheSendAsync(int form)
    {
        var fixture = new SendFixture();
        var message = new ProbeMessage("pending");
        object values = new { Value = "initialized-pending" };
        Guid correlationId = Guid.NewGuid();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task ConfigureAsync(SendContext context)
        {
            entered.TrySetResult();
            await release.Task;
            context.CorrelationId = correlationId;
        }

        Task send = SendFormAsync(form, fixture.Consume, message, values,
            _ => throw new InvalidOperationException("Wrong synchronous callback"),
            context => ConfigureAsync(context),
            _ => throw new InvalidOperationException("Wrong synchronous callback"),
            ConfigureAsync, TestContext.Current.CancellationToken);

        await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.False(send.IsCompleted);
        Assert.Equal(0, fixture.EndpointProxy.AcceptedCount);
        Assert.Null(fixture.ContextProxy.CorrelationId);

        release.TrySetResult();
        await send;

        Assert.Equal(1, fixture.EndpointProxy.AcceptedCount);
        Assert.Equal(correlationId, fixture.ContextProxy.CorrelationId);
        Assert.Single(fixture.EndpointProxy.Invocations);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-CONSUME-SEND", "callback-failure-prevents-transport-acceptance")]
    public async Task CallbackFailure_PropagatesAndPreventsTransportAcceptanceAsync(bool asynchronous)
    {
        var fixture = new SendFixture();
        var failure = new InvalidOperationException("callback refused outgoing send");
        var message = new ProbeMessage("failed");

        Task SendAsync() => asynchronous
            ? SendConsumeContextExecuteExtensions.SendAsync(fixture.Consume, Destination, (object)message,
                (Func<SendContext, Task>)(_ => Task.FromException(failure)), TestContext.Current.CancellationToken)
            : SendConsumeContextExecuteExtensions.SendAsync(fixture.Consume, Destination, (object)message,
                (Action<SendContext>)(_ => throw failure), TestContext.Current.CancellationToken);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(SendAsync);

        Assert.Same(failure, actual);
        Assert.Single(fixture.EndpointProxy.Invocations);
        Assert.Equal(0, fixture.EndpointProxy.AcceptedCount);
    }

    private static Task SendFormAsync(int form, ConsumeContext consume, ProbeMessage message, object values,
        Action<SendContext<ProbeMessage>> typedAction, Func<SendContext<ProbeMessage>, Task> typedAsync,
        Action<SendContext> action, Func<SendContext, Task> asyncCallback, CancellationToken token) => form switch
    {
        0 => SendConsumeContextExecuteExtensions.SendAsync(consume, Destination, message, typedAction, token),
        1 => SendConsumeContextExecuteExtensions.SendAsync(consume, Destination, message, typedAsync, token),
        2 => SendConsumeContextExecuteExtensions.SendAsync(consume, Destination, (object)message, action, token),
        3 => SendConsumeContextExecuteExtensions.SendAsync(consume, Destination, (object)message, asyncCallback, token),
        4 => SendConsumeContextExecuteExtensions.SendAsync(consume, Destination, (object)message, typeof(ProbeMessage), action, token),
        5 => SendConsumeContextExecuteExtensions.SendAsync(consume, Destination, (object)message, typeof(ProbeMessage), asyncCallback, token),
        6 => SendConsumeContextExecuteExtensions.SendAsync<ProbeMessage>(consume, Destination, values, typedAction, token),
        7 => SendConsumeContextExecuteExtensions.SendAsync<ProbeMessage>(consume, Destination, values, typedAsync, token),
        _ => throw new ArgumentOutOfRangeException(nameof(form)),
    };

    private sealed record ProbeMessage(string Value);

    private sealed record EndpointInvocation(MethodInfo Method, object?[] Arguments)
    {
        public CancellationToken CancellationToken => (CancellationToken)Arguments[^1]!;
    }

    private sealed class SendFixture
    {
        public SendFixture()
        {
            SendContext = DispatchProxy.Create<SendContext<ProbeMessage>, SendContextProxy>();
            Endpoint = DispatchProxy.Create<IAdvancedSendEndpoint, EndpointProxy>();
            Consume = DispatchProxy.Create<ConsumeContext, ConsumeProxy>();
            EndpointProxy.SendContext = SendContext;
            ConsumeProxy.Endpoint = Endpoint;
        }

        public ConsumeContext Consume { get; }
        public ConsumeProxy ConsumeProxy => (ConsumeProxy)(object)Consume;
        public IAdvancedSendEndpoint Endpoint { get; }
        public EndpointProxy EndpointProxy => (EndpointProxy)(object)Endpoint;
        public SendContext<ProbeMessage> SendContext { get; }
        public SendContextProxy ContextProxy => (SendContextProxy)(object)SendContext;
    }

    private class ConsumeProxy : DispatchProxy
    {
        public ISendEndpoint Endpoint { get; set; } = null!;
        public List<Uri> Addresses { get; } = [];
        public List<CancellationToken> ResolutionTokens { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_CancellationToken")
                return CancellationToken.None;
            if (targetMethod?.Name == "GetSendEndpointAsync")
            {
                Addresses.Add((Uri)args![0]!);
                ResolutionTokens.Add((CancellationToken)args[1]!);
                return Task.FromResult(Endpoint);
            }
            throw new NotSupportedException(targetMethod?.Name);
        }
    }

    private class EndpointProxy : DispatchProxy
    {
        public int AcceptedCount { get; private set; }
        public List<EndpointInvocation> Invocations { get; } = [];
        public SendContext<ProbeMessage> SendContext { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != "SendAsync")
                throw new NotSupportedException(targetMethod?.Name);

            Invocations.Add(new EndpointInvocation(targetMethod, [.. args!]));
            if (args!.OfType<IPipe<SendContext<ProbeMessage>>>().FirstOrDefault() is { } typedPipe)
                return ExecuteAsync(typedPipe.SendAsync(SendContext));
            if (args.OfType<IPipe<SendContext>>().FirstOrDefault() is { } pipe)
                return ExecuteAsync(pipe.SendAsync(SendContext));
            throw new InvalidOperationException("The callback pipe was not sent to the endpoint.");
        }

        private async Task ExecuteAsync(Task pipeTask)
        {
            await pipeTask;
            AcceptedCount++;
        }
    }

    private class SendContextProxy : DispatchProxy
    {
        public Guid? CorrelationId { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_CorrelationId" => CorrelationId,
            "set_CorrelationId" => SetCorrelationId(args),
            _ => throw new NotSupportedException(targetMethod?.Name),
        };

        private object? SetCorrelationId(object?[]? args)
        {
            CorrelationId = (Guid?)args![0];
            return null;
        }
    }
}
