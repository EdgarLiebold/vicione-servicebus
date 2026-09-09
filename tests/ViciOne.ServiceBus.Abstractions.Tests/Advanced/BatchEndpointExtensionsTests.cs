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

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-ENDPOINT", "send-atomic-admission")]
    public void SendBatch_RejectsANullElementBeforeStartingAnySend()
    {
        ISendEndpoint endpoint = CreateProxy<ISendEndpoint>(out RecordingEndpointProxy proxy);
        BatchMessage[] messages = [new("accepted"), null!];

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = endpoint.SendBatchAsync(messages, TestContext.Current.CancellationToken);
        });

        Assert.Equal("messages", exception.ParamName);
        Assert.Equal(0, proxy.InvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-ENDPOINT", "publish-atomic-admission")]
    public void PublishBatch_RejectsANullElementBeforeStartingAnyPublication()
    {
        IPublishEndpoint endpoint = CreateProxy<IPublishEndpoint>(out RecordingEndpointProxy proxy);
        BatchMessage[] messages = [new("accepted"), null!];

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = endpoint.PublishBatchAsync(messages, TestContext.Current.CancellationToken);
        });

        Assert.Equal("messages", exception.ParamName);
        Assert.Equal(0, proxy.InvocationCount);
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

    class RecordingEndpointProxy : DispatchProxy
    {
        public int InvocationCount { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            InvocationCount++;

            if (args is { Length: > 0 } && args[0] is null)
                throw new ArgumentNullException("message");

            if (targetMethod?.ReturnType == typeof(Task))
                return Task.CompletedTask;

            throw new NotSupportedException($"Unexpected endpoint member: {targetMethod?.Name}");
        }
    }
}
