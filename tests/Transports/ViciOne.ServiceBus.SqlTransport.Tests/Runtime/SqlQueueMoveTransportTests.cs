using System.Reflection;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Runtime;

public sealed class SqlQueueMoveTransportTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-MOVE-CANCELLATION", "dead-letter-move-forwards-caller-token")]
    public async Task DeadLetterMove_ForwardsCallerCancellationToTheDatabaseOperationAsync()
    {
        Guid lockId = NewId.NextGuid();
        var message = new SqlTransportMessage
        {
            LockId = lockId,
            MessageDeliveryId = 42,
            TransportHeaders = "[]",
        };
        SqlMessageContext messageContext = DispatchProxy.Create<SqlMessageContext, MessageContextProxy>();
        var messageProxy = (MessageContextProxy)(object)messageContext;
        messageProxy.Message = message;

        ClientContext clientContext = DispatchProxy.Create<ClientContext, ClientContextProxy>();
        var clientProxy = (ClientContextProxy)(object)clientContext;
        var lockContext = new SqlReceiveLockContext(
            new Uri("db://localhost/transport/input"),
            message,
            new TestReceiveSettings(),
            clientContext,
            TimeProvider.System);

        ReceiveContext receiveContext = DispatchProxy.Create<ReceiveContext, PayloadContextProxy>();
        var receiveProxy = (PayloadContextProxy)(object)receiveContext;
        receiveProxy.Payloads[typeof(SqlMessageContext)] = messageContext;
        receiveProxy.Payloads[typeof(SqlReceiveLockContext)] = lockContext;

        var transport = new SqlQueueDeadLetterTransport("input", SqlQueueType.DeadLetterQueue);
        using var cancellation = new CancellationTokenSource();

        await transport.SendAsync(receiveContext, "expired", cancellation.Token);

        Assert.Equal(cancellation.Token, clientProxy.ObservedCancellationToken);
        Assert.Equal(lockId, clientProxy.ObservedLockId);
        Assert.Equal(42, clientProxy.ObservedMessageDeliveryId);
        Assert.Equal("input", clientProxy.ObservedQueueName);
        Assert.Equal(SqlQueueType.DeadLetterQueue, clientProxy.ObservedQueueType);
        Assert.Equal("expired", clientProxy.ObservedHeaders!.Get(MessageHeaders.Reason, string.Empty));

        await lockContext.CompleteAsync(cancellation.Token);

        Assert.Equal(0, clientProxy.DeleteCallCount);
    }

    private class PayloadContextProxy : DispatchProxy
    {
        public Dictionary<Type, object> Payloads { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == "TryGetPayload" && targetMethod.IsGenericMethod)
            {
                Type payloadType = targetMethod.GetGenericArguments()[0];
                bool found = Payloads.TryGetValue(payloadType, out object? payload);
                args![0] = payload;
                return found;
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private class MessageContextProxy : DispatchProxy
    {
        public SqlTransportMessage Message { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            return targetMethod.Name switch
            {
                "get_TransportMessage" => Message,
                "get_LockId" => Message.LockId,
                "get_DeliveryMessageId" => Message.MessageDeliveryId,
                _ => throw new NotSupportedException(targetMethod.Name),
            };
        }
    }

    private class ClientContextProxy : DispatchProxy
    {
        public CancellationToken ObservedCancellationToken { get; private set; }
        public Guid ObservedLockId { get; private set; }
        public long ObservedMessageDeliveryId { get; private set; }
        public string? ObservedQueueName { get; private set; }
        public SqlQueueType ObservedQueueType { get; private set; }
        public SendHeaders? ObservedHeaders { get; private set; }
        public int DeleteCallCount { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == "get_CancellationToken")
                return CancellationToken.None;

            if (targetMethod.Name == "MoveMessageAsync")
            {
                ObservedLockId = (Guid)args![0]!;
                ObservedMessageDeliveryId = (long)args[1]!;
                ObservedQueueName = (string)args[2]!;
                ObservedQueueType = (SqlQueueType)args[3]!;
                ObservedHeaders = (SendHeaders)args[5]!;
                ObservedCancellationToken = (CancellationToken)args[6]!;
                return Task.FromResult(true);
            }

            if (targetMethod.Name == "DeleteMessageAsync")
            {
                DeleteCallCount++;
                return Task.FromResult(true);
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private sealed class TestReceiveSettings : ReceiveSettings
    {
        public string QueueName => "input";
        public TimeSpan? AutoDeleteOnIdle => null;
        public int? MaxDeliveryCount => 10;
        public long? QueueId => 1;
        public int PrefetchCount => 1;
        public int ConcurrentMessageLimit => 1;
        public int ConcurrentDeliveryLimit => 1;
        public SqlReceiveMode ReceiveMode => SqlReceiveMode.Normal;
        public bool PurgeOnStartup => false;
        public TimeSpan LockDuration => TimeSpan.FromHours(1);
        public TimeSpan PollingInterval => TimeSpan.FromSeconds(1);
        public TimeSpan? UnlockDelay => null;
        public TimeSpan MaxLockDuration => TimeSpan.FromHours(2);
        public string EntityName => QueueName;
        public int MaintenanceBatchSize => 100;
        public bool DeadLetterExpiredMessages => false;
    }
}
