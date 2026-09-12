using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Advanced;

public sealed class BatchEndpointExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-ENDPOINT", "send-required-inputs")]
    public void SendBatchOverloads_RejectEveryMissingRequiredInputBeforeDispatch()
    {
        ISendEndpoint endpoint = CreateProxy<ISendEndpoint>(out _);
        BatchMessage[] typed = [];
        object[] runtime = [];
        IPipe<SendContext> pipe = Pipe.Empty<SendContext>();
        Action<SendContext> callback = static _ => { };
        Func<SendContext, Task> asyncCallback = static _ => Task.CompletedTask;

        AssertNull("endpoint", () => BatchEndpointExtensions.SendBatchAsync<BatchMessage>(null!, typed));
        AssertNull("messages", () => endpoint.SendBatchAsync((IEnumerable<BatchMessage>)null!));
        AssertNull("pipe", () => endpoint.SendBatchAsync(typed, (IPipe<SendContext<BatchMessage>>)null!));
        AssertNull("callback", () => endpoint.SendBatchAsync(typed, (Action<SendContext<BatchMessage>>)null!));
        AssertNull("callback", () => endpoint.SendBatchAsync(typed, (Func<SendContext<BatchMessage>, Task>)null!));

        AssertNull("messages", () => endpoint.SendBatchAsync((IEnumerable<object>)null!));
        AssertNull("pipe", () => endpoint.SendBatchAsync(runtime, (IPipe<SendContext>)null!));
        AssertNull("callback", () => endpoint.SendBatchAsync(runtime, (Action<SendContext>)null!));
        AssertNull("callback", () => endpoint.SendBatchAsync(runtime, (Func<SendContext, Task>)null!));

        AssertNull("messageType", () => endpoint.SendBatchAsync(runtime, (Type)null!));
        AssertNull("messageType", () => endpoint.SendBatchAsync(runtime, (Type)null!, pipe));
        AssertNull("pipe", () => endpoint.SendBatchAsync(runtime, typeof(BatchMessage), (IPipe<SendContext>)null!));
        AssertNull("messageType", () => endpoint.SendBatchAsync(runtime, (Type)null!, callback));
        AssertNull("callback", () => endpoint.SendBatchAsync(runtime, typeof(BatchMessage), (Action<SendContext>)null!));
        AssertNull("messageType", () => endpoint.SendBatchAsync(runtime, (Type)null!, asyncCallback));
        AssertNull("callback", () => endpoint.SendBatchAsync(runtime, typeof(BatchMessage), (Func<SendContext, Task>)null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-ENDPOINT", "publish-required-inputs")]
    public void PublishBatchOverloads_RejectEveryMissingRequiredInputBeforeDispatch()
    {
        IPublishEndpoint endpoint = CreateProxy<IPublishEndpoint>(out _);
        BatchMessage[] typed = [];
        object[] runtime = [];
        IPipe<PublishContext> pipe = Pipe.Empty<PublishContext>();
        Action<PublishContext> callback = static _ => { };
        Func<PublishContext, Task> asyncCallback = static _ => Task.CompletedTask;

        AssertNull("endpoint", () => BatchEndpointExtensions.PublishBatchAsync<BatchMessage>(null!, typed));
        AssertNull("messages", () => endpoint.PublishBatchAsync((IEnumerable<BatchMessage>)null!));
        AssertNull("pipe", () => endpoint.PublishBatchAsync(typed, (IPipe<PublishContext<BatchMessage>>)null!));
        AssertNull("callback", () => endpoint.PublishBatchAsync(typed, (Action<PublishContext<BatchMessage>>)null!));
        AssertNull("callback", () => endpoint.PublishBatchAsync(typed, (Func<PublishContext<BatchMessage>, Task>)null!));

        AssertNull("messages", () => endpoint.PublishBatchAsync((IEnumerable<object>)null!));
        AssertNull("pipe", () => endpoint.PublishBatchAsync(runtime, (IPipe<PublishContext>)null!));
        AssertNull("callback", () => endpoint.PublishBatchAsync(runtime, (Action<PublishContext>)null!));
        AssertNull("callback", () => endpoint.PublishBatchAsync(runtime, (Func<PublishContext, Task>)null!));

        AssertNull("messageType", () => endpoint.PublishBatchAsync(runtime, (Type)null!));
        AssertNull("messageType", () => endpoint.PublishBatchAsync(runtime, (Type)null!, pipe));
        AssertNull("pipe", () => endpoint.PublishBatchAsync(runtime, typeof(BatchMessage), (IPipe<PublishContext>)null!));
        AssertNull("messageType", () => endpoint.PublishBatchAsync(runtime, (Type)null!, callback));
        AssertNull("callback", () => endpoint.PublishBatchAsync(runtime, typeof(BatchMessage), (Action<PublishContext>)null!));
        AssertNull("messageType", () => endpoint.PublishBatchAsync(runtime, (Type)null!, asyncCallback));
        AssertNull("callback", () => endpoint.PublishBatchAsync(runtime, typeof(BatchMessage), (Func<PublishContext, Task>)null!));
    }

    [Theory]
    [InlineData(BatchDispatchMode.Typed)]
    [InlineData(BatchDispatchMode.TypedPipe)]
    [InlineData(BatchDispatchMode.TypedCallback)]
    [InlineData(BatchDispatchMode.TypedAsyncCallback)]
    [InlineData(BatchDispatchMode.Runtime)]
    [InlineData(BatchDispatchMode.RuntimePipe)]
    [InlineData(BatchDispatchMode.RuntimeCallback)]
    [InlineData(BatchDispatchMode.RuntimeAsyncCallback)]
    [InlineData(BatchDispatchMode.ExplicitType)]
    [InlineData(BatchDispatchMode.ExplicitTypePipe)]
    [InlineData(BatchDispatchMode.ExplicitTypeCallback)]
    [InlineData(BatchDispatchMode.ExplicitTypeAsyncCallback)]
    [RequirementCoverage("REQ-VSB-BATCH-ENDPOINT", "send-atomic-admission")]
    public void SendBatch_RejectsANullElementBeforeStartingAnySend(BatchDispatchMode mode)
    {
        ISendEndpoint endpoint = CreateProxy<IAdvancedSendEndpoint>(out RecordingEndpointProxy proxy);
        BatchMessage[] messages = [new("accepted"), null!];

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = SendAsync(endpoint, messages, mode, TestContext.Current.CancellationToken);
        });

        Assert.Equal("messages", exception.ParamName);
        Assert.Equal(0, proxy.InvocationCount);
    }

    [Theory]
    [InlineData(BatchDispatchMode.Typed)]
    [InlineData(BatchDispatchMode.TypedPipe)]
    [InlineData(BatchDispatchMode.TypedCallback)]
    [InlineData(BatchDispatchMode.TypedAsyncCallback)]
    [InlineData(BatchDispatchMode.Runtime)]
    [InlineData(BatchDispatchMode.RuntimePipe)]
    [InlineData(BatchDispatchMode.RuntimeCallback)]
    [InlineData(BatchDispatchMode.RuntimeAsyncCallback)]
    [InlineData(BatchDispatchMode.ExplicitType)]
    [InlineData(BatchDispatchMode.ExplicitTypePipe)]
    [InlineData(BatchDispatchMode.ExplicitTypeCallback)]
    [InlineData(BatchDispatchMode.ExplicitTypeAsyncCallback)]
    [RequirementCoverage("REQ-VSB-BATCH-ENDPOINT", "publish-atomic-admission")]
    public void PublishBatch_RejectsANullElementBeforeStartingAnyPublication(BatchDispatchMode mode)
    {
        IPublishEndpoint endpoint = CreateProxy<IAdvancedPublishEndpoint>(out RecordingEndpointProxy proxy);
        BatchMessage[] messages = [new("accepted"), null!];

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = PublishAsync(endpoint, messages, mode, TestContext.Current.CancellationToken);
        });

        Assert.Equal("messages", exception.ParamName);
        Assert.Equal(0, proxy.InvocationCount);
    }

    [Theory]
    [InlineData(BatchDispatchMode.Typed)]
    [InlineData(BatchDispatchMode.TypedPipe)]
    [InlineData(BatchDispatchMode.TypedCallback)]
    [InlineData(BatchDispatchMode.TypedAsyncCallback)]
    [InlineData(BatchDispatchMode.Runtime)]
    [InlineData(BatchDispatchMode.RuntimePipe)]
    [InlineData(BatchDispatchMode.RuntimeCallback)]
    [InlineData(BatchDispatchMode.RuntimeAsyncCallback)]
    [InlineData(BatchDispatchMode.ExplicitType)]
    [InlineData(BatchDispatchMode.ExplicitTypePipe)]
    [InlineData(BatchDispatchMode.ExplicitTypeCallback)]
    [InlineData(BatchDispatchMode.ExplicitTypeAsyncCallback)]
    [RequirementCoverage("REQ-VSB-BATCH-ENDPOINT", "send-overload-forwarding-matrix")]
    public async Task SendBatchOverloads_ForwardEveryMessageContractPipeAndTokenAsync(BatchDispatchMode mode)
    {
        ISendEndpoint endpoint = CreateProxy<IAdvancedSendEndpoint>(out RecordingEndpointProxy proxy);
        BatchMessage[] messages = [new("first"), new("second")];
        using var cancellation = new CancellationTokenSource();

        await SendAsync(endpoint, messages, mode, cancellation.Token);

        AssertDispatch(proxy, messages, mode, cancellation.Token, "SendAsync");
    }

    [Theory]
    [InlineData(BatchDispatchMode.Typed)]
    [InlineData(BatchDispatchMode.TypedPipe)]
    [InlineData(BatchDispatchMode.TypedCallback)]
    [InlineData(BatchDispatchMode.TypedAsyncCallback)]
    [InlineData(BatchDispatchMode.Runtime)]
    [InlineData(BatchDispatchMode.RuntimePipe)]
    [InlineData(BatchDispatchMode.RuntimeCallback)]
    [InlineData(BatchDispatchMode.RuntimeAsyncCallback)]
    [InlineData(BatchDispatchMode.ExplicitType)]
    [InlineData(BatchDispatchMode.ExplicitTypePipe)]
    [InlineData(BatchDispatchMode.ExplicitTypeCallback)]
    [InlineData(BatchDispatchMode.ExplicitTypeAsyncCallback)]
    [RequirementCoverage("REQ-VSB-BATCH-ENDPOINT", "publish-overload-forwarding-matrix")]
    public async Task PublishBatchOverloads_ForwardEveryMessageContractPipeAndTokenAsync(BatchDispatchMode mode)
    {
        IPublishEndpoint endpoint = CreateProxy<IAdvancedPublishEndpoint>(out RecordingEndpointProxy proxy);
        BatchMessage[] messages = [new("first"), new("second")];
        using var cancellation = new CancellationTokenSource();

        await PublishAsync(endpoint, messages, mode, cancellation.Token);

        AssertDispatch(proxy, messages, mode, cancellation.Token, "PublishAsync");
    }

    static Task SendAsync(
        ISendEndpoint endpoint,
        BatchMessage[] messages,
        BatchDispatchMode mode,
        CancellationToken cancellationToken)
    {
        IEnumerable<object> runtimeMessages = messages;
        return mode switch
        {
            BatchDispatchMode.Typed => endpoint.SendBatchAsync(messages, cancellationToken),
            BatchDispatchMode.TypedPipe => endpoint.SendBatchAsync(messages, Pipe.Empty<SendContext<BatchMessage>>(), cancellationToken),
            BatchDispatchMode.TypedCallback => endpoint.SendBatchAsync(messages, static _ => { }, cancellationToken),
            BatchDispatchMode.TypedAsyncCallback => endpoint.SendBatchAsync(messages, static _ => Task.CompletedTask, cancellationToken),
            BatchDispatchMode.Runtime => endpoint.SendBatchAsync(runtimeMessages, cancellationToken),
            BatchDispatchMode.RuntimePipe => endpoint.SendBatchAsync(runtimeMessages, Pipe.Empty<SendContext>(), cancellationToken),
            BatchDispatchMode.RuntimeCallback => endpoint.SendBatchAsync(runtimeMessages, static _ => { }, cancellationToken),
            BatchDispatchMode.RuntimeAsyncCallback => endpoint.SendBatchAsync(runtimeMessages, static _ => Task.CompletedTask, cancellationToken),
            BatchDispatchMode.ExplicitType => endpoint.SendBatchAsync(runtimeMessages, typeof(BatchMessage), cancellationToken),
            BatchDispatchMode.ExplicitTypePipe => endpoint.SendBatchAsync(
                runtimeMessages, typeof(BatchMessage), Pipe.Empty<SendContext>(), cancellationToken),
            BatchDispatchMode.ExplicitTypeCallback => endpoint.SendBatchAsync(
                runtimeMessages, typeof(BatchMessage), static _ => { }, cancellationToken),
            BatchDispatchMode.ExplicitTypeAsyncCallback => endpoint.SendBatchAsync(
                runtimeMessages, typeof(BatchMessage), static _ => Task.CompletedTask, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown batch dispatch mode."),
        };
    }

    static Task PublishAsync(
        IPublishEndpoint endpoint,
        BatchMessage[] messages,
        BatchDispatchMode mode,
        CancellationToken cancellationToken)
    {
        IEnumerable<object> runtimeMessages = messages;
        return mode switch
        {
            BatchDispatchMode.Typed => endpoint.PublishBatchAsync(messages, cancellationToken),
            BatchDispatchMode.TypedPipe => endpoint.PublishBatchAsync(messages, Pipe.Empty<PublishContext<BatchMessage>>(), cancellationToken),
            BatchDispatchMode.TypedCallback => endpoint.PublishBatchAsync(messages, static _ => { }, cancellationToken),
            BatchDispatchMode.TypedAsyncCallback => endpoint.PublishBatchAsync(messages, static _ => Task.CompletedTask, cancellationToken),
            BatchDispatchMode.Runtime => endpoint.PublishBatchAsync(runtimeMessages, cancellationToken),
            BatchDispatchMode.RuntimePipe => endpoint.PublishBatchAsync(runtimeMessages, Pipe.Empty<PublishContext>(), cancellationToken),
            BatchDispatchMode.RuntimeCallback => endpoint.PublishBatchAsync(runtimeMessages, static _ => { }, cancellationToken),
            BatchDispatchMode.RuntimeAsyncCallback => endpoint.PublishBatchAsync(runtimeMessages, static _ => Task.CompletedTask, cancellationToken),
            BatchDispatchMode.ExplicitType => endpoint.PublishBatchAsync(runtimeMessages, typeof(BatchMessage), cancellationToken),
            BatchDispatchMode.ExplicitTypePipe => endpoint.PublishBatchAsync(
                runtimeMessages, typeof(BatchMessage), Pipe.Empty<PublishContext>(), cancellationToken),
            BatchDispatchMode.ExplicitTypeCallback => endpoint.PublishBatchAsync(
                runtimeMessages, typeof(BatchMessage), static _ => { }, cancellationToken),
            BatchDispatchMode.ExplicitTypeAsyncCallback => endpoint.PublishBatchAsync(
                runtimeMessages, typeof(BatchMessage), static _ => Task.CompletedTask, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown batch dispatch mode."),
        };
    }

    static void AssertDispatch(
        RecordingEndpointProxy proxy,
        BatchMessage[] messages,
        BatchDispatchMode mode,
        CancellationToken cancellationToken,
        string methodName)
    {
        Assert.Equal(messages.Length, proxy.Invocations.Count);
        Assert.Equal(messages, proxy.Invocations.Select(invocation => Assert.IsType<BatchMessage>(invocation.Arguments[0])));
        Assert.All(proxy.Invocations, invocation =>
        {
            Assert.Equal(methodName, invocation.Method.Name);
            Assert.Contains(invocation.Arguments, argument => argument is CancellationToken token && token == cancellationToken);
        });

        bool usesExplicitType = mode >= BatchDispatchMode.ExplicitType;
        Assert.All(proxy.Invocations, invocation =>
            Assert.Equal(usesExplicitType, invocation.Arguments.Contains(typeof(BatchMessage))));

        bool usesPipe = mode is not BatchDispatchMode.Typed
            and not BatchDispatchMode.Runtime
            and not BatchDispatchMode.ExplicitType;
        Assert.All(proxy.Invocations, invocation => Assert.Equal(
            usesPipe,
            invocation.Arguments.Any(argument => argument is IPipe<SendContext>
                or IPipe<SendContext<BatchMessage>>
                or IPipe<PublishContext>
                or IPipe<PublishContext<BatchMessage>>)));
    }

    static TEndpoint CreateProxy<TEndpoint>(out RecordingEndpointProxy proxy)
        where TEndpoint : class
    {
        TEndpoint endpoint = DispatchProxy.Create<TEndpoint, RecordingEndpointProxy>();
        proxy = (RecordingEndpointProxy)(object)endpoint;
        return endpoint;
    }

    static void AssertNull(string parameterName, Action operation)
    {
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(operation).ParamName);
    }

    sealed record BatchMessage(string Value);

    public enum BatchDispatchMode
    {
        Typed,
        TypedPipe,
        TypedCallback,
        TypedAsyncCallback,
        Runtime,
        RuntimePipe,
        RuntimeCallback,
        RuntimeAsyncCallback,
        ExplicitType,
        ExplicitTypePipe,
        ExplicitTypeCallback,
        ExplicitTypeAsyncCallback,
    }

    class RecordingEndpointProxy : DispatchProxy
    {
        public int InvocationCount => Invocations.Count;

        public List<Invocation> Invocations { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            ArgumentNullException.ThrowIfNull(args);
            Invocations.Add(new Invocation(targetMethod, [.. args]));

            if (args is { Length: > 0 } && args[0] is null)
                throw new ArgumentNullException("message");

            if (targetMethod?.ReturnType == typeof(Task))
                return Task.CompletedTask;

            throw new NotSupportedException($"Unexpected endpoint member: {targetMethod?.Name}");
        }
    }

    sealed record Invocation(MethodInfo Method, object?[] Arguments);
}
