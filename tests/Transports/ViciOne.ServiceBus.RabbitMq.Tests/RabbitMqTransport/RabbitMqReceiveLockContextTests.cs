using System.Reflection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.RabbitMq.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport;

public sealed class RabbitMqReceiveLockContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-RECEIVE-LOCK", "successful-acknowledgement-uses-exact-delivery")]
    public async Task CompleteAsync_AcknowledgesOnlyTheSelectedDeliveryAsync()
    {
        var channel = new RecordingChannelContext();
        var context = new RabbitMqReceiveLockContext(channel, 42, CancellationToken.None);

        await context.CompleteAsync(TestContext.Current.CancellationToken);

        Assert.Equal((42UL, false), Assert.Single(channel.Acknowledgements));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-RECEIVE-LOCK", "acknowledgement-failure-is-transport-unavailable")]
    public async Task CompleteAsync_WrapsTheExactAcknowledgementFailureAsync()
    {
        var failure = new IOException("channel write failed");
        var channel = new RecordingChannelContext { AckFailure = failure };
        var context = new RabbitMqReceiveLockContext(channel, 42, CancellationToken.None);

        TransportUnavailableException exception = await Assert.ThrowsAsync<TransportUnavailableException>(
            () => context.CompleteAsync(TestContext.Current.CancellationToken));

        Assert.Same(failure, exception.InnerException);
        Assert.Contains("42", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-RECEIVE-LOCK", "caller-cancellation-precedes-closed-channel")]
    public async Task CompleteAsync_HonorsCallerCancellationBeforeInspectingTheChannelAsync()
    {
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        var channel = new RecordingChannelContext(isClosed: true);
        var context = new RabbitMqReceiveLockContext(channel, 42, CancellationToken.None);

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.CompleteAsync(canceled.Token));

        Assert.Equal(canceled.Token, exception.CancellationToken);
        Assert.Empty(channel.Acknowledgements);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-RECEIVE-LOCK", "closed-channel-preserves-broker-reason")]
    public async Task CompleteAndValidate_PreserveTheBrokerCloseReasonAsync()
    {
        var reason = new ShutdownEventArgs(ShutdownInitiator.Peer, 406, "precondition failed");
        var channel = new RecordingChannelContext(isClosed: true, closeReason: reason);
        var context = new RabbitMqReceiveLockContext(channel, 42, CancellationToken.None);

        OperationInterruptedException complete = await Assert.ThrowsAsync<OperationInterruptedException>(
            () => context.CompleteAsync(TestContext.Current.CancellationToken));
        OperationInterruptedException validate = await Assert.ThrowsAsync<OperationInterruptedException>(
            () => context.ValidateLockStatusAsync(TestContext.Current.CancellationToken));

        Assert.Same(reason, complete.ShutdownReason);
        Assert.Same(reason, validate.ShutdownReason);
        Assert.Empty(channel.Acknowledgements);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-RECEIVE-LOCK", "fault-requeues-once-and-suppresses-nack-failure")]
    public async Task FaultedAsync_RequeuesOnceAndSuppressesBrokerWriteFailureAsync()
    {
        var failure = new IOException("nack failed");
        var channel = new RecordingChannelContext { NackFailure = failure };
        var context = new RabbitMqReceiveLockContext(channel, 42, CancellationToken.None);

        await context.FaultedAsync(new InvalidOperationException("consumer failed"), TestContext.Current.CancellationToken);

        Assert.Equal((42UL, false, true), Assert.Single(channel.NegativeAcknowledgements));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-RECEIVE-LOCK", "fault-skips-unavailable-contexts")]
    public async Task FaultedAsync_SkipsClosedOrCanceledReceiveContextsAndRejectsCallerCancellationAsync()
    {
        var closedChannel = new RecordingChannelContext(isClosed: true);
        var closed = new RabbitMqReceiveLockContext(closedChannel, 1, CancellationToken.None);
        using var receiveCanceled = new CancellationTokenSource();
        receiveCanceled.Cancel();
        var canceledChannel = new RecordingChannelContext();
        var canceledReceive = new RabbitMqReceiveLockContext(canceledChannel, 2, receiveCanceled.Token);
        using var callerCanceled = new CancellationTokenSource();
        callerCanceled.Cancel();

        await closed.FaultedAsync(new InvalidOperationException("failed"), TestContext.Current.CancellationToken);
        await canceledReceive.FaultedAsync(new InvalidOperationException("failed"), TestContext.Current.CancellationToken);
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => closed.FaultedAsync(new InvalidOperationException("failed"), callerCanceled.Token));

        Assert.Equal(callerCanceled.Token, exception.CancellationToken);
        Assert.Empty(closedChannel.NegativeAcknowledgements);
        Assert.Empty(canceledChannel.NegativeAcknowledgements);
    }

    private sealed class RecordingChannelContext : BasePipeContext, ChannelContext
    {
        public RecordingChannelContext(bool isClosed = false, ShutdownEventArgs? closeReason = null)
        {
            Channel = DispatchProxy.Create<IChannel, ChannelProxy>();
            var proxy = (ChannelProxy)(object)Channel;
            proxy.IsClosed = isClosed;
            proxy.CloseReason = closeReason;
        }

        public Exception? AckFailure { get; init; }
        public Exception? NackFailure { get; init; }
        public List<(ulong DeliveryTag, bool Multiple)> Acknowledgements { get; } = [];
        public List<(ulong DeliveryTag, bool Multiple, bool Requeue)> NegativeAcknowledgements { get; } = [];
        public IChannel Channel { get; }
        public ConnectionContext ConnectionContext => null!;

        public ValueTask BasicAckAsync(ulong deliveryTag, bool multiple, CancellationToken cancellationToken)
        {
            Acknowledgements.Add((deliveryTag, multiple));
            return AckFailure == null ? ValueTask.CompletedTask : ValueTask.FromException(AckFailure);
        }

        public Task BasicNackAsync(ulong deliveryTag, bool multiple, bool requeue, CancellationToken cancellationToken)
        {
            NegativeAcknowledgements.Add((deliveryTag, multiple, requeue));
            return NackFailure == null ? Task.CompletedTask : Task.FromException(NackFailure);
        }

        public Task BasicPublishAsync(string exchange, string routingKey, bool mandatory, BasicProperties basicProperties, byte[] body,
            bool awaitAck, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task ExchangeBindAsync(string destination, string source, string routingKey, IDictionary<string, object?> arguments,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task ExchangeDeclareAsync(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object?> arguments,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task ExchangeDeclarePassiveAsync(string exchange, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task QueueBindAsync(string queue, string exchange, string routingKey, IDictionary<string, object?> arguments,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<QueueDeclareOk> QueueDeclareAsync(string queue, bool durable, bool exclusive, bool autoDelete,
            IDictionary<string, object?> arguments, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<QueueDeclareOk> QueueDeclarePassiveAsync(string queue, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<uint> QueuePurgeAsync(string queue, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task BasicQosAsync(uint prefetchSize, ushort prefetchCount, bool global, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<string> BasicConsumeAsync(string queue, bool noAck, bool exclusive, IDictionary<string, object?> arguments,
            IAsyncBasicConsumer consumer, string consumerTag, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task BasicCancelAsync(string consumerTag, CancellationToken cancellationToken) => throw new NotSupportedException();
        public void NotifyFaulted(Exception exception, Uri inputAddress) => throw new NotSupportedException();
    }

    private class ChannelProxy : DispatchProxy
    {
        public bool IsClosed { get; set; }
        public ShutdownEventArgs? CloseReason { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_IsClosed" => IsClosed,
            "get_IsOpen" => !IsClosed,
            "get_CloseReason" => CloseReason,
            _ => Default(targetMethod?.ReturnType),
        };

        private static object? Default(Type? type) => type?.IsValueType == true ? Activator.CreateInstance(type) : null;
    }
}
