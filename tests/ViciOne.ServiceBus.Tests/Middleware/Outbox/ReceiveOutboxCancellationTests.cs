using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Middleware.Outbox.InMemory;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware.Outbox;

public sealed class ReceiveOutboxCancellationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-CHECKPOINT", "delivery-canceled-during-serialization-does-not-append-outgoing-message")]
    public async Task AddSend_DeliveryCanceledDuringSerialization_DoesNotAppendOutgoingMessageAsync(bool cancelDelivery)
    {
        using var delivery = new CancellationTokenSource();
        using var operation = new CancellationTokenSource();
        using ServiceProvider provider = new ServiceCollection().BuildServiceProvider();
        Guid messageId = Guid.NewGuid();
        Guid consumerId = Guid.NewGuid();
        ConsumeContext<Command> input = InMemoryOutboxTestContextFactory.Create(
            new Command(messageId), delivery.Token, messageId: messageId);
        var options = new OutboxConsumeOptions
        {
            ConsumerId = consumerId,
            ConsumerType = nameof(Command),
            MessageDeliveryLimit = 10,
            MessageDeliveryTimeout = TimeSpan.FromMinutes(1),
        };
        using var inbox = new InMemoryInboxMessage(messageId, consumerId);
        var context = new InMemoryOutboxConsumeContext<Command>(input, options, provider, inbox);
        var outgoing = new MessageSendContext<Command>(new Command(Guid.NewGuid()), operation.Token)
        {
            MessageId = Guid.NewGuid(),
            SupportedMessageTypes = ["urn:message:tests:Command"],
            Serializer = new CancelingSerializer(cancelDelivery ? delivery : operation),
        };

        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.AddSendAsync(outgoing, operation.Token));
        Assert.Equal(cancelDelivery ? delivery.Token : operation.Token, failure.CancellationToken);
        Assert.Equal(cancelDelivery, delivery.IsCancellationRequested);
        Assert.Equal(!cancelDelivery, operation.IsCancellationRequested);
        Assert.Empty(inbox.GetOutboxMessages());
        Assert.Null(inbox.Consumed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-CHECKPOINT", "pre-canceled-and-locked-inbox-report-original-cancellation-source")]
    public async Task Factory_PreCanceledAndLockedInbox_ReportTheOriginalCancellationSourceAsync(bool cancelDelivery)
    {
        CancellationToken testToken = TestContext.Current.CancellationToken;
        using ServiceProvider provider = new ServiceCollection().BuildServiceProvider();
        using var repository = new InMemoryOutboxMessageRepository();
        var factory = new InMemoryOutboxContextFactory(repository, provider);
        Guid messageId = Guid.NewGuid();
        Guid consumerId = Guid.NewGuid();
        var options = new OutboxConsumeOptions
        {
            ConsumerId = consumerId,
            ConsumerType = nameof(Command),
            MessageDeliveryLimit = 10,
            MessageDeliveryTimeout = TimeSpan.FromMinutes(1),
        };
        int callbacks = 0;
        IPipe<OutboxConsumeContext<Command>> next = Pipe.ExecuteAwaited<OutboxConsumeContext<Command>>(_ =>
        {
            callbacks++;
            return Task.CompletedTask;
        });

        using (var delivery = new CancellationTokenSource())
        using (var operation = new CancellationTokenSource())
        {
            if (cancelDelivery)
                delivery.Cancel();
            else
                operation.Cancel();
            ConsumeContext<Command> input = InMemoryOutboxTestContextFactory.Create(
                new Command(messageId), delivery.Token, messageId: messageId);
            OperationCanceledException preFailure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => factory.SendAsync(input, options, next, operation.Token));
            Assert.Equal(cancelDelivery ? delivery.Token : operation.Token, preFailure.CancellationToken);
            Assert.Equal(0, callbacks);
        }

        InMemoryInboxMessage owned = await repository.LockAsync(messageId, consumerId, testToken);
        Assert.Equal(0, owned.ReceiveCount);
        using var waitingDelivery = new CancellationTokenSource();
        using var waitingOperation = new CancellationTokenSource();
        ConsumeContext<Command> waitingInput = InMemoryOutboxTestContextFactory.Create(
            new Command(messageId), waitingDelivery.Token, messageId: messageId);
        try
        {
            Task waiting = factory.SendAsync(waitingInput, options, next, waitingOperation.Token);
            Assert.False(waiting.IsCompleted);
            if (cancelDelivery)
                waitingDelivery.Cancel();
            else
                waitingOperation.Cancel();
            OperationCanceledException waitFailure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => waiting.WaitAsync(TimeSpan.FromSeconds(10), testToken));
            Assert.Equal(cancelDelivery ? waitingDelivery.Token : waitingOperation.Token, waitFailure.CancellationToken);
            Assert.Equal(0, callbacks);
            Assert.Equal(0, owned.ReceiveCount);
            Assert.Null(owned.Consumed);
            Assert.Null(owned.Delivered);
            Assert.Empty(owned.GetOutboxMessages());
        }
        finally
        {
            owned.Release();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-CHECKPOINT", "canceled-delivery-cannot-commit-consumed-fence")]
    public async Task Factory_CanceledDeliveryDoesNotCommitConsumedFenceAsync()
    {
        using var delivery = new CancellationTokenSource();
        using var explicitOperation = new CancellationTokenSource();
        using ServiceProvider provider = new ServiceCollection().BuildServiceProvider();
        using var repository = new InMemoryOutboxMessageRepository();
        Guid messageId = Guid.NewGuid();
        Guid consumerId = Guid.NewGuid();
        ConsumeContext<Command> input = InMemoryOutboxTestContextFactory.Create(
            new Command(messageId), delivery.Token, messageId: messageId);
        var options = new OutboxConsumeOptions
        {
            ConsumerId = consumerId,
            ConsumerType = nameof(Command),
            MessageDeliveryLimit = 10,
            MessageDeliveryTimeout = TimeSpan.FromMinutes(1),
        };
        var factory = new InMemoryOutboxContextFactory(repository, provider);
        int executions = 0;

        OperationCanceledException failure = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            factory.SendAsync(input, options, Pipe.ExecuteAwaited<OutboxConsumeContext<Command>>(async context =>
            {
                executions++;
                delivery.Cancel();
                await context.SetConsumedAsync(explicitOperation.Token);
            }), explicitOperation.Token));
        Assert.Equal(delivery.Token, failure.CancellationToken);
        Assert.Equal(1, executions);
        Assert.False(explicitOperation.IsCancellationRequested);

        InMemoryInboxMessage retained = await repository.LockAsync(
            messageId, consumerId, TestContext.Current.CancellationToken);
        try
        {
            Assert.Null(retained.Consumed);
            Assert.Null(retained.Delivered);
            Assert.Equal(1, retained.ReceiveCount);
            Assert.Empty(retained.GetOutboxMessages());
        }
        finally
        {
            retained.Release();
        }
    }

    [Theory]
    [InlineData("consume")]
    [InlineData("deliver")]
    [InlineData("load")]
    [InlineData("checkpoint")]
    [InlineData("remove")]
    [InlineData("capture")]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-CHECKPOINT", "canceled-delivery-rejects-distinct-active-operation")]
    public async Task CanceledDelivery_RejectsEveryOutboxStateTransitionWithDistinctActiveTokenAsync(string operation)
    {
        using var delivery = new CancellationTokenSource();
        using var explicitOperation = new CancellationTokenSource();
        using ServiceProvider provider = new ServiceCollection().BuildServiceProvider();
        Guid messageId = Guid.NewGuid();
        Guid consumerId = Guid.NewGuid();
        ConsumeContext<Command> input = InMemoryOutboxTestContextFactory.Create(
            new Command(messageId), delivery.Token, messageId: messageId);
        var options = new OutboxConsumeOptions
        {
            ConsumerId = consumerId,
            ConsumerType = nameof(Command),
            MessageDeliveryLimit = 10,
            MessageDeliveryTimeout = TimeSpan.FromMinutes(1),
        };
        using var inbox = new InMemoryInboxMessage(messageId, consumerId);
        inbox.AddOutboxMessage(NewOutgoingMessage());
        var context = new InMemoryOutboxConsumeContext<Command>(input, options, provider, inbox);
        var outgoing = new MessageSendContext<Command>(new Command(Guid.NewGuid()), explicitOperation.Token)
        {
            MessageId = Guid.NewGuid(),
        };
        delivery.Cancel();

        Func<Task> action = operation switch
        {
            "consume" => () => context.SetConsumedAsync(explicitOperation.Token),
            "deliver" => () => context.SetDeliveredAsync(explicitOperation.Token),
            "load" => () => context.LoadOutboxMessagesAsync(explicitOperation.Token),
            "checkpoint" => () => context.NotifyOutboxMessageDeliveredAsync(
                NewOutgoingMessage(), explicitOperation.Token),
            "remove" => () => context.RemoveOutboxMessagesAsync(explicitOperation.Token),
            "capture" => () => context.AddSendAsync(outgoing, explicitOperation.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };
        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(action);
        Assert.Equal(delivery.Token, failure.CancellationToken);
        Assert.False(explicitOperation.IsCancellationRequested);
        Assert.Null(inbox.Consumed);
        Assert.Null(inbox.Delivered);
        Assert.Null(inbox.LastSequenceNumber);
        Assert.Single(inbox.GetOutboxMessages());
    }

    private static InMemoryOutboxMessage NewOutgoingMessage() => new()
    {
        MessageId = Guid.NewGuid(),
        ContentType = "application/json",
        MessageType = "urn:message:tests:Command",
        Body = "{}",
    };

    public sealed record Command(Guid Id);

    private sealed class CancelingSerializer(CancellationTokenSource cancellation) : IMessageSerializer
    {
        public System.Net.Mime.ContentType ContentType => new("application/json");

        public MessageBody GetMessageBody<T>(SendContext<T> context) where T : class
        {
            cancellation.Cancel();
            return new StringMessageBody("{}");
        }
    }
}
