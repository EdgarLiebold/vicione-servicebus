using System.Net.Mime;
using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports.Receiving;

public sealed class ReceiveContextProxyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-CONTEXT-PROXY", "all-state-and-endpoint-properties-forward")]
    public void Properties_ExposeTheExactUnderlyingReceiveState()
    {
        ReceiveContext source = DispatchProxy.Create<ReceiveContext, RecordingReceiveContextProxy>();
        var recording = (RecordingReceiveContextProxy)(object)source;
        var context = new ConcreteReceiveContextProxy(source);

        Assert.Equal(recording.CancellationToken, context.CancellationToken);
        Assert.Equal(recording.PublishFaults, context.PublishFaults);
        Assert.Same(recording.Body, context.Body);
        Assert.Equal(recording.ElapsedTime, context.ElapsedTime);
        Assert.Equal(recording.InputAddress, context.InputAddress);
        Assert.Equal(recording.ContentType, context.ContentType);
        Assert.Equal(recording.Redelivered, context.Redelivered);
        Assert.Same(recording.TransportHeaders, context.TransportHeaders);
        Assert.Same(recording.ReceiveCompleted, context.ReceiveCompleted);
        Assert.Equal(recording.IsDelivered, context.IsDelivered);
        Assert.Equal(recording.IsFaulted, context.IsFaulted);
        Assert.Same(recording.SendEndpointProvider, context.SendEndpointProvider);
        Assert.Same(recording.PublishEndpointProvider, context.PublishEndpointProvider);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => new ConcreteReceiveContextProxy(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-CONTEXT-PROXY", "payload-notification-and-task-operations-forward")]
    public async Task Operations_DelegateOnceWithTheirExactArgumentsAsync()
    {
        ReceiveContext source = DispatchProxy.Create<ReceiveContext, RecordingReceiveContextProxy>();
        var recording = (RecordingReceiveContextProxy)(object)source;
        var context = new ConcreteReceiveContextProxy(source);

        Assert.True(context.HasPayloadType(typeof(Payload)));
        Assert.True(context.TryGetPayload(out Payload? selected));
        Assert.Same(recording.Payload, selected);
        Assert.Same(recording.Payload, context.GetOrAddPayload(() => new Payload("new")));
        Assert.Same(
            recording.Payload,
            ((PipeContext)context).AddOrUpdatePayload(() => new Payload("new"), current => new Payload(current.Value + "-updated")));

        ConsumeContext<ProbeMessage> consumeContext = DispatchProxy.Create<ConsumeContext<ProbeMessage>, UnexpectedInvocationProxy>();
        using var cancellation = new CancellationTokenSource();
        var failure = new InvalidOperationException("receive failed");
        var attachedTask = Task.Delay(1, TestContext.Current.CancellationToken);

        Assert.Same(recording.NotificationTask, context.NotifyConsumedAsync(consumeContext, TimeSpan.FromSeconds(2), "consumer", cancellation.Token));
        Assert.Same(recording.NotificationTask, context.NotifyFaultedAsync(consumeContext, TimeSpan.FromSeconds(3), "consumer", failure, cancellation.Token));
        Assert.Same(recording.NotificationTask, context.NotifyFaultedAsync(failure, cancellation.Token));
        context.AddReceiveTask(attachedTask);

        Assert.Same(attachedTask, recording.AttachedTask);
        Assert.Equal(
            [
                nameof(ReceiveContext.HasPayloadType),
                nameof(ReceiveContext.TryGetPayload),
                nameof(ReceiveContext.GetOrAddPayload),
                nameof(PipeContext.AddOrUpdatePayload),
                nameof(ReceiveContext.NotifyConsumedAsync),
                nameof(ReceiveContext.NotifyFaultedAsync),
                nameof(ReceiveContext.NotifyFaultedAsync),
                nameof(ReceiveContext.AddReceiveTask),
            ],
            recording.OperationNames);
        await recording.NotificationTask;
    }

    private sealed record ProbeMessage;

    private sealed record Payload(string Value);

    private sealed class ConcreteReceiveContextProxy(ReceiveContext context) : ReceiveContextProxy(context)
    {
    }

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The receive proxy test unexpectedly invoked {targetMethod?.Name}.");
    }

    private class RecordingReceiveContextProxy : DispatchProxy
    {
        private readonly TaskCompletionSource _notification = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public RecordingReceiveContextProxy()
        {
            _notification.SetResult();
        }

        public Task? AttachedTask { get; private set; }

        public MessageBody Body { get; } = new MemoryMessageBody(new byte[] { 1, 2, 3 });

        public CancellationToken CancellationToken { get; } = new CancellationTokenSource().Token;

        public ContentType ContentType { get; } = new("application/vnd.vicione.receive-test");

        public TimeSpan ElapsedTime { get; } = TimeSpan.FromMilliseconds(125);

        public Uri InputAddress { get; } = new("loopback://localhost/receive-proxy");

        public bool IsDelivered => true;

        public bool IsFaulted => false;

        public Task NotificationTask => _notification.Task;

        public List<string> OperationNames { get; } = [];

        public Payload Payload { get; } = new("source");

        public IPublishEndpointProvider PublishEndpointProvider { get; } =
            DispatchProxy.Create<IPublishEndpointProvider, UnexpectedInvocationProxy>();

        public bool PublishFaults => true;

        public Task ReceiveCompleted => Task.CompletedTask;

        public bool Redelivered => true;

        public ISendEndpointProvider SendEndpointProvider { get; } =
            DispatchProxy.Create<ISendEndpointProvider, UnexpectedInvocationProxy>();

        public Headers TransportHeaders => EmptyHeaders.Instance;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            MethodInfo method = targetMethod ?? throw new InvalidOperationException("The receive proxy supplied no method metadata.");
            if (method.Name.StartsWith("get_", StringComparison.Ordinal))
            {
                return method.Name switch
                {
                    "get_Body" => Body,
                    "get_CancellationToken" => CancellationToken,
                    "get_ContentType" => ContentType,
                    "get_ElapsedTime" => ElapsedTime,
                    "get_InputAddress" => InputAddress,
                    "get_IsDelivered" => IsDelivered,
                    "get_IsFaulted" => IsFaulted,
                    "get_PublishEndpointProvider" => PublishEndpointProvider,
                    "get_PublishFaults" => PublishFaults,
                    "get_ReceiveCompleted" => ReceiveCompleted,
                    "get_Redelivered" => Redelivered,
                    "get_SendEndpointProvider" => SendEndpointProvider,
                    "get_TransportHeaders" => TransportHeaders,
                    _ => throw new InvalidOperationException($"The receive proxy test has no property behavior for {method.Name}."),
                };
            }

            OperationNames.Add(method.Name);
            switch (method.Name)
            {
                case nameof(ReceiveContext.HasPayloadType):
                    return ((Type)args![0]!).IsInstanceOfType(Payload);
                case nameof(ReceiveContext.TryGetPayload):
                    args![0] = Payload;
                    return true;
                case nameof(ReceiveContext.GetOrAddPayload):
                case nameof(PipeContext.AddOrUpdatePayload):
                    return Payload;
                case nameof(ReceiveContext.NotifyConsumedAsync):
                case nameof(ReceiveContext.NotifyFaultedAsync):
                    return NotificationTask;
                case nameof(ReceiveContext.AddReceiveTask):
                    AttachedTask = (Task)args![0]!;
                    return null;
                default:
                    throw new InvalidOperationException($"The receive proxy test has no behavior for {method.Name}.");
            }
        }
    }
}
