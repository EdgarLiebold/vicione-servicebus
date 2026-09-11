using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Advanced.Contexts;

public sealed class SendConsumeContextExtensionsTests
{
    private static readonly Uri Destination = new("loopback://localhost/advanced-context-send");

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-SEND", "typed-overload-forwarding")]
    public async Task TypedOverloads_ForwardTheExactMessagePipeAndTokenOnceAsync()
    {
        using var callerCancellation = new CancellationTokenSource();
        var message = new TestMessage("typed");
        IPipe<SendContext<TestMessage>> typedPipe = Pipe.Empty<SendContext<TestMessage>>();
        IPipe<SendContext> untypedPipe = Pipe.Empty<SendContext>();

        await AssertForwardedAsync(
            context => context.SendAsync(Destination, message, callerCancellation.Token),
            callerCancellation.Token,
            invocation =>
            {
                AssertGenericContract(invocation.Method, typeof(TestMessage), typeof(TestMessage), typeof(CancellationToken));
                Assert.Same(message, invocation.Arguments[0]);
            });
        await AssertForwardedAsync(
            context => context.SendAsync(Destination, message, typedPipe, callerCancellation.Token),
            callerCancellation.Token,
            invocation =>
            {
                AssertGenericContract(
                    invocation.Method,
                    typeof(TestMessage),
                    typeof(TestMessage),
                    typeof(IPipe<SendContext<TestMessage>>),
                    typeof(CancellationToken));
                Assert.Same(message, invocation.Arguments[0]);
                Assert.Same(typedPipe, invocation.Arguments[1]);
            });
        await AssertForwardedAsync(
            context => context.SendAsync(Destination, message, untypedPipe, callerCancellation.Token),
            callerCancellation.Token,
            invocation =>
            {
                AssertGenericContract(
                    invocation.Method,
                    typeof(TestMessage),
                    typeof(TestMessage),
                    typeof(IPipe<SendContext>),
                    typeof(CancellationToken));
                Assert.Same(message, invocation.Arguments[0]);
                Assert.Same(untypedPipe, invocation.Arguments[1]);
            });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-SEND", "runtime-overload-forwarding")]
    public async Task RuntimeOverloads_UseTheAdvancedRuntimeContractAndForwardEveryArgumentAsync()
    {
        using var callerCancellation = new CancellationTokenSource();
        object message = new TestMessage("runtime");
        IPipe<SendContext> pipe = Pipe.Empty<SendContext>();

        await AssertForwardedAsync(
            context => context.SendAsync(Destination, message, callerCancellation.Token),
            callerCancellation.Token,
            invocation =>
            {
                Assert.False(invocation.Method.IsGenericMethod);
                AssertParameters(invocation.Method, typeof(object), typeof(CancellationToken));
                Assert.Same(message, invocation.Arguments[0]);
            });
        await AssertForwardedAsync(
            context => context.SendAsync(Destination, message, typeof(TestMessage), callerCancellation.Token),
            callerCancellation.Token,
            invocation =>
            {
                Assert.False(invocation.Method.IsGenericMethod);
                AssertParameters(invocation.Method, typeof(object), typeof(Type), typeof(CancellationToken));
                Assert.Same(message, invocation.Arguments[0]);
                Assert.Same(typeof(TestMessage), invocation.Arguments[1]);
            });
        await AssertForwardedAsync(
            context => context.SendAsync(Destination, message, pipe, callerCancellation.Token),
            callerCancellation.Token,
            invocation =>
            {
                Assert.False(invocation.Method.IsGenericMethod);
                AssertParameters(invocation.Method, typeof(object), typeof(IPipe<SendContext>), typeof(CancellationToken));
                Assert.Same(message, invocation.Arguments[0]);
                Assert.Same(pipe, invocation.Arguments[1]);
            });
        await AssertForwardedAsync(
            context => context.SendAsync(Destination, message, typeof(TestMessage), pipe, callerCancellation.Token),
            callerCancellation.Token,
            invocation =>
            {
                Assert.False(invocation.Method.IsGenericMethod);
                AssertParameters(
                    invocation.Method,
                    typeof(object),
                    typeof(Type),
                    typeof(IPipe<SendContext>),
                    typeof(CancellationToken));
                Assert.Same(message, invocation.Arguments[0]);
                Assert.Same(typeof(TestMessage), invocation.Arguments[1]);
                Assert.Same(pipe, invocation.Arguments[2]);
            });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-SEND", "initializer-overload-forwarding")]
    public async Task InitializerOverloads_ForwardTheExactValuesPipeAndContractOnceAsync()
    {
        using var callerCancellation = new CancellationTokenSource();
        object values = new { Value = "initialized" };
        IPipe<SendContext<TestMessage>> typedPipe = Pipe.Empty<SendContext<TestMessage>>();
        IPipe<SendContext> untypedPipe = Pipe.Empty<SendContext>();

        await AssertForwardedAsync(
            context => context.SendAsync<TestMessage>(Destination, values, callerCancellation.Token),
            callerCancellation.Token,
            invocation =>
            {
                AssertGenericContract(invocation.Method, typeof(TestMessage), typeof(object), typeof(CancellationToken));
                Assert.Same(values, invocation.Arguments[0]);
            });
        await AssertForwardedAsync(
            context => context.SendAsync<TestMessage>(Destination, values, typedPipe, callerCancellation.Token),
            callerCancellation.Token,
            invocation =>
            {
                AssertGenericContract(
                    invocation.Method,
                    typeof(TestMessage),
                    typeof(object),
                    typeof(IPipe<SendContext<TestMessage>>),
                    typeof(CancellationToken));
                Assert.Same(values, invocation.Arguments[0]);
                Assert.Same(typedPipe, invocation.Arguments[1]);
            });
        await AssertForwardedAsync(
            context => context.SendAsync<TestMessage>(Destination, values, untypedPipe, callerCancellation.Token),
            callerCancellation.Token,
            invocation =>
            {
                AssertGenericContract(
                    invocation.Method,
                    typeof(TestMessage),
                    typeof(object),
                    typeof(IPipe<SendContext>),
                    typeof(CancellationToken));
                Assert.Same(values, invocation.Arguments[0]);
                Assert.Same(untypedPipe, invocation.Arguments[1]);
            });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-SEND", "declaration-order-required-boundaries")]
    public void EveryOverload_RejectsTheFirstMissingParameterBeforeResolvingAnEndpoint()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var message = new TestMessage("boundary");
        object values = new { Value = "boundary" };
        IPipe<SendContext<TestMessage>> typedPipe = Pipe.Empty<SendContext<TestMessage>>();
        IPipe<SendContext> pipe = Pipe.Empty<SendContext>();

        AssertNull("context", () => SendConsumeContextExtensions.SendAsync<TestMessage>(null!, null!, null!, token));
        AssertNull("context", () => SendConsumeContextExtensions.SendAsync<TestMessage>(null!, null!, null!, typedPipe, token));
        AssertNull("context", () => SendConsumeContextExtensions.SendAsync<TestMessage>(null!, null!, null!, pipe, token));
        AssertNull("context", () => SendConsumeContextExtensions.SendAsync(null!, null!, null!, token));
        AssertNull("context", () => SendConsumeContextExtensions.SendAsync(null!, null!, null!, (Type)null!, token));
        AssertNull("context", () => SendConsumeContextExtensions.SendAsync(null!, null!, null!, (IPipe<SendContext>)null!, token));
        AssertNull(
            "context",
            () => SendConsumeContextExtensions.SendAsync(
                null!,
                null!,
                null!,
                null!,
                (IPipe<SendContext>)null!,
                token));
        AssertNull("context", () => SendConsumeContextExtensions.SendAsync<TestMessage>(null!, null!, (object)null!, token));
        AssertNull("context", () => SendConsumeContextExtensions.SendAsync<TestMessage>(null!, null!, (object)null!, typedPipe, token));
        AssertNull("context", () => SendConsumeContextExtensions.SendAsync<TestMessage>(null!, null!, (object)null!, pipe, token));

        ConsumeContext context = CreateContext(new ContextToken(default), CreateEndpoint(out _), out RecordingConsumeContextProxy contextRecorder);
        AssertNull("destinationAddress", () => context.SendAsync<TestMessage>(null!, null!, token));
        AssertNull("destinationAddress", () => context.SendAsync<TestMessage>(null!, null!, typedPipe, token));
        AssertNull("destinationAddress", () => context.SendAsync<TestMessage>(null!, null!, pipe, token));
        AssertNull("destinationAddress", () => context.SendAsync(null!, (object)null!, token));
        AssertNull("destinationAddress", () => context.SendAsync(null!, (object)null!, (Type)null!, token));
        AssertNull("destinationAddress", () => context.SendAsync(null!, (object)null!, (IPipe<SendContext>)null!, token));
        AssertNull(
            "destinationAddress",
            () => context.SendAsync(null!, (object)null!, null!, (IPipe<SendContext>)null!, token));
        AssertNull("destinationAddress", () => context.SendAsync<TestMessage>(null!, (object)null!, token));
        AssertNull("destinationAddress", () => context.SendAsync<TestMessage>(null!, (object)null!, typedPipe, token));
        AssertNull("destinationAddress", () => context.SendAsync<TestMessage>(null!, (object)null!, pipe, token));

        AssertNull("message", () => context.SendAsync<TestMessage>(Destination, null!, token));
        AssertNull("message", () => context.SendAsync<TestMessage>(Destination, null!, typedPipe, token));
        AssertNull("pipe", () => context.SendAsync(Destination, message, (IPipe<SendContext<TestMessage>>)null!, token));
        AssertNull("message", () => context.SendAsync<TestMessage>(Destination, null!, pipe, token));
        AssertNull("pipe", () => context.SendAsync(Destination, message, (IPipe<SendContext>)null!, token));
        AssertNull("message", () => context.SendAsync(Destination, (object)null!, token));
        AssertNull("message", () => context.SendAsync(Destination, (object)null!, typeof(TestMessage), token));
        AssertNull("messageType", () => context.SendAsync(Destination, message, (Type)null!, token));
        AssertNull("message", () => context.SendAsync(Destination, (object)null!, pipe, token));
        AssertNull("pipe", () => context.SendAsync(Destination, message, (IPipe<SendContext>)null!, token));
        AssertNull("message", () => context.SendAsync(Destination, (object)null!, typeof(TestMessage), pipe, token));
        AssertNull("messageType", () => context.SendAsync(Destination, message, (Type)null!, pipe, token));
        AssertNull(
            "pipe",
            () => context.SendAsync(Destination, message, typeof(TestMessage), (IPipe<SendContext>)null!, token));
        AssertNull("values", () => context.SendAsync<TestMessage>(Destination, (object)null!, token));
        AssertNull("values", () => context.SendAsync<TestMessage>(Destination, (object)null!, typedPipe, token));
        AssertNull("pipe", () => context.SendAsync<TestMessage>(Destination, values, (IPipe<SendContext<TestMessage>>)null!, token));
        AssertNull("values", () => context.SendAsync<TestMessage>(Destination, (object)null!, pipe, token));
        AssertNull("pipe", () => context.SendAsync<TestMessage>(Destination, values, (IPipe<SendContext>)null!, token));

        Assert.Equal(0, contextRecorder.EndpointResolutionCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-SEND", "callback-overloads-own-required-boundaries")]
    public void CallbackOverloads_RejectRequiredInputsInDeclarationOrderBeforeEndpointResolution()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var message = new TestMessage("callback-boundary");
        object values = new { Value = "callback-boundary" };
        Action<SendContext<TestMessage>> typedAction = _ => { };
        Func<SendContext<TestMessage>, Task> typedCallback = _ => Task.CompletedTask;
        Action<SendContext> action = _ => { };
        Func<SendContext, Task> callback = _ => Task.CompletedTask;

        AssertNull(
            "context",
            () => SendConsumeContextExecuteExtensions.SendAsync<TestMessage>(null!, null!, null!, typedAction, token));
        AssertNull(
            "context",
            () => SendConsumeContextExecuteExtensions.SendAsync<TestMessage>(null!, null!, null!, typedCallback, token));
        AssertNull("context", () => SendConsumeContextExecuteExtensions.SendAsync(null!, null!, null!, action, token));
        AssertNull("context", () => SendConsumeContextExecuteExtensions.SendAsync(null!, null!, null!, callback, token));
        AssertNull(
            "context",
            () => SendConsumeContextExecuteExtensions.SendAsync(null!, null!, null!, null!, action, token));
        AssertNull(
            "context",
            () => SendConsumeContextExecuteExtensions.SendAsync(null!, null!, null!, null!, callback, token));
        AssertNull(
            "context",
            () => SendConsumeContextExecuteExtensions.SendAsync<TestMessage>(null!, null!, (object)null!, typedAction, token));
        AssertNull(
            "context",
            () => SendConsumeContextExecuteExtensions.SendAsync<TestMessage>(null!, null!, (object)null!, typedCallback, token));

        ConsumeContext context = CreateContext(new ContextToken(default), CreateEndpoint(out _), out RecordingConsumeContextProxy recorder);
        AssertNull("destinationAddress", () => context.SendAsync<TestMessage>(null!, null!, typedAction, token));
        AssertNull("destinationAddress", () => context.SendAsync<TestMessage>(null!, null!, typedCallback, token));
        AssertNull("destinationAddress", () => context.SendAsync(null!, (object)null!, action, token));
        AssertNull("destinationAddress", () => context.SendAsync(null!, (object)null!, callback, token));
        AssertNull("destinationAddress", () => context.SendAsync(null!, (object)null!, null!, action, token));
        AssertNull("destinationAddress", () => context.SendAsync(null!, (object)null!, null!, callback, token));
        AssertNull("destinationAddress", () => context.SendAsync<TestMessage>(null!, (object)null!, typedAction, token));
        AssertNull("destinationAddress", () => context.SendAsync<TestMessage>(null!, (object)null!, typedCallback, token));

        AssertNull("message", () => context.SendAsync<TestMessage>(Destination, null!, typedAction, token));
        AssertNull("callback", () => context.SendAsync(Destination, message, (Action<SendContext<TestMessage>>)null!, token));
        AssertNull("message", () => context.SendAsync<TestMessage>(Destination, null!, typedCallback, token));
        AssertNull("callback", () => context.SendAsync(Destination, message, (Func<SendContext<TestMessage>, Task>)null!, token));
        AssertNull("message", () => context.SendAsync(Destination, (object)null!, action, token));
        AssertNull("callback", () => context.SendAsync(Destination, (object)message, (Action<SendContext>)null!, token));
        AssertNull("message", () => context.SendAsync(Destination, (object)null!, callback, token));
        AssertNull("callback", () => context.SendAsync(Destination, (object)message, (Func<SendContext, Task>)null!, token));
        AssertNull("message", () => context.SendAsync(Destination, (object)null!, typeof(TestMessage), action, token));
        AssertNull("messageType", () => context.SendAsync(Destination, (object)message, null!, action, token));
        AssertNull("callback", () => context.SendAsync(Destination, (object)message, typeof(TestMessage), (Action<SendContext>)null!, token));
        AssertNull("message", () => context.SendAsync(Destination, (object)null!, typeof(TestMessage), callback, token));
        AssertNull("messageType", () => context.SendAsync(Destination, (object)message, null!, callback, token));
        AssertNull("callback", () => context.SendAsync(Destination, (object)message, typeof(TestMessage), (Func<SendContext, Task>)null!, token));
        AssertNull("values", () => context.SendAsync<TestMessage>(Destination, (object)null!, typedAction, token));
        AssertNull("callback", () => context.SendAsync<TestMessage>(Destination, values, (Action<SendContext<TestMessage>>)null!, token));
        AssertNull("values", () => context.SendAsync<TestMessage>(Destination, (object)null!, typedCallback, token));
        AssertNull("callback", () => context.SendAsync<TestMessage>(Destination, values, (Func<SendContext<TestMessage>, Task>)null!, token));

        Assert.Equal(0, recorder.EndpointResolutionCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-SEND", "single-source-and-shared-cancellation-token-identity")]
    public async Task SingleOrSharedCancellationSource_PreservesTheOriginalTokenIdentityAsync()
    {
        using var contextCancellation = new CancellationTokenSource();
        using var callerCancellation = new CancellationTokenSource();

        await AssertEffectiveTokenAsync(new TokenScenario(default, default, default));
        await AssertEffectiveTokenAsync(new TokenScenario(contextCancellation.Token, default, contextCancellation.Token));
        await AssertEffectiveTokenAsync(new TokenScenario(default, callerCancellation.Token, callerCancellation.Token));
        await AssertEffectiveTokenAsync(
            new TokenScenario(contextCancellation.Token, contextCancellation.Token, contextCancellation.Token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-CONSUME-SEND", "distinct-cancellation-sources-are-linked")]
    public async Task DistinctCancellationSources_CancelTheInFlightSendFromEitherOwnerAsync(bool cancelCaller)
    {
        using var contextCancellation = new CancellationTokenSource();
        using var callerCancellation = new CancellationTokenSource();
        var endpoint = CreateEndpoint(out RecordingEndpointProxy endpointRecorder);
        endpointRecorder.BlockSend = true;
        ConsumeContext context = CreateContext(
            new ContextToken(contextCancellation.Token),
            endpoint,
            out RecordingConsumeContextProxy contextRecorder);

        Task send = context.SendAsync(Destination, new TestMessage("linked"), callerCancellation.Token);
        await endpointRecorder.SendStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        CancellationToken effectiveToken = Assert.Single(endpointRecorder.Invocations).CancellationToken;

        Assert.True(effectiveToken.CanBeCanceled);
        Assert.NotEqual(contextCancellation.Token, effectiveToken);
        Assert.NotEqual(callerCancellation.Token, effectiveToken);
        Assert.Equal(effectiveToken, Assert.Single(contextRecorder.ResolutionTokens));

        if (cancelCaller)
            callerCancellation.Cancel();
        else
            contextCancellation.Cancel();

        Assert.True(effectiveToken.IsCancellationRequested);
        endpointRecorder.CompleteSend();
        await send;
    }

    private static async Task AssertForwardedAsync(
        Func<ConsumeContext, Task> send,
        CancellationToken expectedToken,
        Action<EndpointInvocation> assertInvocation)
    {
        IAdvancedSendEndpoint endpoint = CreateEndpoint(out RecordingEndpointProxy endpointRecorder);
        ConsumeContext context = CreateContext(new ContextToken(default), endpoint, out RecordingConsumeContextProxy contextRecorder);

        await send(context);

        Assert.Equal(1, contextRecorder.EndpointResolutionCalls);
        Assert.Equal(Destination, Assert.Single(contextRecorder.ResolutionAddresses));
        Assert.Equal(expectedToken, Assert.Single(contextRecorder.ResolutionTokens));
        EndpointInvocation invocation = Assert.Single(endpointRecorder.Invocations);
        Assert.Equal(expectedToken, invocation.CancellationToken);
        assertInvocation(invocation);
    }

    private static async Task AssertEffectiveTokenAsync(TokenScenario scenario)
    {
        IAdvancedSendEndpoint endpoint = CreateEndpoint(out RecordingEndpointProxy endpointRecorder);
        ConsumeContext context = CreateContext(
            new ContextToken(scenario.ContextToken),
            endpoint,
            out RecordingConsumeContextProxy contextRecorder);

        await context.SendAsync(Destination, new TestMessage("token"), scenario.CallerToken);

        Assert.Equal(scenario.ExpectedToken, Assert.Single(contextRecorder.ResolutionTokens));
        Assert.Equal(scenario.ExpectedToken, Assert.Single(endpointRecorder.Invocations).CancellationToken);
    }

    private static ConsumeContext CreateContext(
        ContextToken contextToken,
        ISendEndpoint endpoint,
        out RecordingConsumeContextProxy recorder)
    {
        ConsumeContext context = DispatchProxy.Create<ConsumeContext, RecordingConsumeContextProxy>();
        recorder = (RecordingConsumeContextProxy)(object)context;
        recorder.Configure(contextToken.Value, endpoint);
        return context;
    }

    private static IAdvancedSendEndpoint CreateEndpoint(out RecordingEndpointProxy recorder)
    {
        IAdvancedSendEndpoint endpoint = DispatchProxy.Create<IAdvancedSendEndpoint, RecordingEndpointProxy>();
        recorder = (RecordingEndpointProxy)(object)endpoint;
        return endpoint;
    }

    private static void AssertGenericContract(MethodInfo method, Type genericArgument, params Type[] parameterTypes)
    {
        Assert.True(method.IsGenericMethod);
        Assert.Equal(genericArgument, Assert.Single(method.GetGenericArguments()));
        AssertParameters(method, parameterTypes);
    }

    private static void AssertParameters(MethodInfo method, params Type[] parameterTypes) =>
        Assert.Equal(parameterTypes, method.GetParameters().Select(parameter => parameter.ParameterType));

    private static void AssertNull(string parameterName, Action operation) =>
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(operation).ParamName);

    private sealed record TestMessage(string Value);

    private readonly record struct ContextToken(CancellationToken Value);

    private readonly record struct TokenScenario(
        CancellationToken ContextToken,
        CancellationToken CallerToken,
        CancellationToken ExpectedToken);

    private sealed record EndpointInvocation(MethodInfo Method, object?[] Arguments)
    {
        public CancellationToken CancellationToken => (CancellationToken)Arguments[^1]!;
    }

    private class RecordingConsumeContextProxy : DispatchProxy
    {
        private CancellationToken _cancellationToken;
        private ISendEndpoint _endpoint = null!;

        public int EndpointResolutionCalls { get; private set; }
        public List<Uri> ResolutionAddresses { get; } = [];
        public List<CancellationToken> ResolutionTokens { get; } = [];

        public void Configure(CancellationToken cancellationToken, ISendEndpoint endpoint)
        {
            _cancellationToken = cancellationToken;
            _endpoint = endpoint;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == "get_CancellationToken")
                return _cancellationToken;
            if (targetMethod.Name == nameof(ISendEndpointProvider.GetSendEndpointAsync))
            {
                EndpointResolutionCalls++;
                ResolutionAddresses.Add((Uri)args![0]!);
                ResolutionTokens.Add((CancellationToken)args[1]!);
                return Task.FromResult(_endpoint);
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private class RecordingEndpointProxy : DispatchProxy
    {
        private readonly TaskCompletionSource _sendCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool BlockSend { get; set; }
        public List<EndpointInvocation> Invocations { get; } = [];
        public TaskCompletionSource SendStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void CompleteSend() => _sendCompletion.TrySetResult();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name != nameof(ISendEndpoint.SendAsync))
                throw new NotSupportedException(targetMethod.Name);

            Invocations.Add(new EndpointInvocation(targetMethod, [.. args!]));
            SendStarted.TrySetResult();
            return BlockSend ? _sendCompletion.Task : Task.CompletedTask;
        }
    }
}
