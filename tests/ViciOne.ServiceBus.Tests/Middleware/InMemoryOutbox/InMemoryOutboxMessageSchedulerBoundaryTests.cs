using ViciOne.ServiceBus.Middleware.InMemoryOutbox;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware.InMemoryOutbox;

public sealed class InMemoryOutboxMessageSchedulerBoundaryTests
{
    private static readonly DateTimeOffset DueAt = new(2044, 5, 6, 7, 8, 9, TimeSpan.Zero);
    private static readonly Uri Destination = new("loopback://localhost/scheduler-boundary");

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-SCHEDULER", "every-overload-validates-required-inputs")]
    public async Task EveryScheduleOverload_RejectsEachMissingRequiredInputBeforeSchedulerUseAsync()
    {
        ConsumeContext consumeContext = InMemoryOutboxTestContextFactory.Create(
            new SchedulerMessage(),
            TestContext.Current.CancellationToken).Advanced();
        MessageSchedulerFactory rejectingFactory = _ =>
            throw new InvalidOperationException("Input validation must complete before scheduler resolution.");
        Task clearToSend = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task;
        var scheduler = new InMemoryOutboxMessageSchedulerContext(consumeContext, rejectingFactory, clearToSend);
        var message = new SchedulerMessage();
        IPipe<SendContext<SchedulerMessage>> typedPipe = Pipe.Empty<SendContext<SchedulerMessage>>();
        IPipe<SendContext> untypedPipe = Pipe.Empty<SendContext>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        Assert.Equal("consumeContext", Assert.Throws<ArgumentNullException>(() =>
            new InMemoryOutboxMessageSchedulerContext(null!, rejectingFactory, clearToSend)).ParamName);
        Assert.Equal("schedulerFactory", Assert.Throws<ArgumentNullException>(() =>
            new InMemoryOutboxMessageSchedulerContext(consumeContext, null!, clearToSend)).ParamName);
        Assert.Equal("clearToSend", Assert.Throws<ArgumentNullException>(() =>
            new InMemoryOutboxMessageSchedulerContext(consumeContext, rejectingFactory, null!)).ParamName);

        await RejectsAsync("destinationAddress", () => scheduler.ScheduleSendAsync(null!, DueAt, message, cancellationToken));
        await RejectsAsync("destinationAddress", () => scheduler.ScheduleSendAsync(null!, DueAt, message, typedPipe, cancellationToken));
        await RejectsAsync("destinationAddress", () => scheduler.ScheduleSendAsync(null!, DueAt, message, untypedPipe, cancellationToken));
        await RejectsAsync("destinationAddress", () => scheduler.ScheduleSendAsync(null!, DueAt, (object)message, cancellationToken));
        await RejectsAsync("destinationAddress", () => scheduler.ScheduleSendAsync(null!, DueAt, message, typeof(SchedulerMessage), cancellationToken));
        await RejectsAsync("destinationAddress", () => scheduler.ScheduleSendAsync(null!, DueAt, (object)message, untypedPipe, cancellationToken));
        await RejectsAsync("destinationAddress", () => scheduler.ScheduleSendAsync(null!, DueAt, message, typeof(SchedulerMessage), untypedPipe, cancellationToken));
        await RejectsAsync("destinationAddress", () => scheduler.ScheduleSendAsync<SchedulerMessage>(null!, DueAt, (object)new { }, cancellationToken));
        await RejectsAsync("destinationAddress", () => scheduler.ScheduleSendAsync<SchedulerMessage>(null!, DueAt, new { }, typedPipe, cancellationToken));
        await RejectsAsync("destinationAddress", () => scheduler.ScheduleSendAsync<SchedulerMessage>(null!, DueAt, new { }, untypedPipe, cancellationToken));

        await RejectsAsync("message", () => scheduler.ScheduleSendAsync(Destination, DueAt, (SchedulerMessage)null!, cancellationToken));
        await RejectsAsync("message", () => scheduler.ScheduleSendAsync(Destination, DueAt, (SchedulerMessage)null!, typedPipe, cancellationToken));
        await RejectsAsync("message", () => scheduler.ScheduleSendAsync(Destination, DueAt, (SchedulerMessage)null!, untypedPipe, cancellationToken));
        await RejectsAsync("message", () => scheduler.ScheduleSendAsync(Destination, DueAt, (object)null!, cancellationToken));
        await RejectsAsync("message", () => scheduler.ScheduleSendAsync(Destination, DueAt, null!, typeof(SchedulerMessage), cancellationToken));
        await RejectsAsync("message", () => scheduler.ScheduleSendAsync(Destination, DueAt, (object)null!, untypedPipe, cancellationToken));
        await RejectsAsync("message", () => scheduler.ScheduleSendAsync(Destination, DueAt, null!, typeof(SchedulerMessage), untypedPipe, cancellationToken));
        await RejectsAsync("values", () => scheduler.ScheduleSendAsync<SchedulerMessage>(Destination, DueAt, (object)null!, cancellationToken));
        await RejectsAsync("values", () => scheduler.ScheduleSendAsync<SchedulerMessage>(Destination, DueAt, (object)null!, typedPipe, cancellationToken));
        await RejectsAsync("values", () => scheduler.ScheduleSendAsync<SchedulerMessage>(Destination, DueAt, (object)null!, untypedPipe, cancellationToken));
        await RejectsAsync("messageType", () => scheduler.ScheduleSendAsync(Destination, DueAt, message, (Type)null!, cancellationToken));
        await RejectsAsync("messageType", () => scheduler.ScheduleSendAsync(Destination, DueAt, message, (Type)null!, untypedPipe, cancellationToken));
        await RejectsAsync("pipe", () => scheduler.ScheduleSendAsync(Destination, DueAt, message, (IPipe<SendContext<SchedulerMessage>>)null!, cancellationToken));
        await RejectsAsync("pipe", () => scheduler.ScheduleSendAsync(Destination, DueAt, message, (IPipe<SendContext>)null!, cancellationToken));
        await RejectsAsync("pipe", () => scheduler.ScheduleSendAsync(Destination, DueAt, (object)message, (IPipe<SendContext>)null!, cancellationToken));
        await RejectsAsync("pipe", () => scheduler.ScheduleSendAsync(Destination, DueAt, message, typeof(SchedulerMessage), null!, cancellationToken));
        await RejectsAsync("pipe", () => scheduler.ScheduleSendAsync<SchedulerMessage>(Destination, DueAt, new { }, (IPipe<SendContext<SchedulerMessage>>)null!, cancellationToken));
        await RejectsAsync("pipe", () => scheduler.ScheduleSendAsync<SchedulerMessage>(Destination, DueAt, new { }, (IPipe<SendContext>)null!, cancellationToken));

        await RejectsAsync("message", () => scheduler.ScheduleSendAsync(DueAt, (SchedulerMessage)null!, cancellationToken));
        await RejectsAsync("message", () => scheduler.ScheduleSendAsync(DueAt, (SchedulerMessage)null!, typedPipe, cancellationToken));
        await RejectsAsync("message", () => scheduler.ScheduleSendAsync(DueAt, (SchedulerMessage)null!, untypedPipe, cancellationToken));
        await RejectsAsync("message", () => scheduler.ScheduleSendAsync(DueAt, (object)null!, cancellationToken));
        await RejectsAsync("message", () => scheduler.ScheduleSendAsync(DueAt, null!, typeof(SchedulerMessage), cancellationToken));
        await RejectsAsync("message", () => scheduler.ScheduleSendAsync(DueAt, (object)null!, untypedPipe, cancellationToken));
        await RejectsAsync("message", () => scheduler.ScheduleSendAsync(DueAt, null!, typeof(SchedulerMessage), untypedPipe, cancellationToken));
        await RejectsAsync("values", () => scheduler.ScheduleSendAsync<SchedulerMessage>(DueAt, (object)null!, cancellationToken));
        await RejectsAsync("values", () => scheduler.ScheduleSendAsync<SchedulerMessage>(DueAt, (object)null!, typedPipe, cancellationToken));
        await RejectsAsync("values", () => scheduler.ScheduleSendAsync<SchedulerMessage>(DueAt, (object)null!, untypedPipe, cancellationToken));
        await RejectsAsync("messageType", () => scheduler.ScheduleSendAsync(DueAt, message, (Type)null!, cancellationToken));
        await RejectsAsync("messageType", () => scheduler.ScheduleSendAsync(DueAt, message, (Type)null!, untypedPipe, cancellationToken));
        await RejectsAsync("pipe", () => scheduler.ScheduleSendAsync(DueAt, message, (IPipe<SendContext<SchedulerMessage>>)null!, cancellationToken));
        await RejectsAsync("pipe", () => scheduler.ScheduleSendAsync(DueAt, message, (IPipe<SendContext>)null!, cancellationToken));
        await RejectsAsync("pipe", () => scheduler.ScheduleSendAsync(DueAt, (object)message, (IPipe<SendContext>)null!, cancellationToken));
        await RejectsAsync("pipe", () => scheduler.ScheduleSendAsync(DueAt, message, typeof(SchedulerMessage), null!, cancellationToken));
        await RejectsAsync("pipe", () => scheduler.ScheduleSendAsync<SchedulerMessage>(DueAt, new { }, (IPipe<SendContext<SchedulerMessage>>)null!, cancellationToken));
        await RejectsAsync("pipe", () => scheduler.ScheduleSendAsync<SchedulerMessage>(DueAt, new { }, (IPipe<SendContext>)null!, cancellationToken));

        await RejectsAsync("message", () => scheduler.SchedulePublishAsync(DueAt, (SchedulerMessage)null!, cancellationToken));
        await RejectsAsync("message", () => scheduler.SchedulePublishAsync(DueAt, (SchedulerMessage)null!, typedPipe, cancellationToken));
        await RejectsAsync("message", () => scheduler.SchedulePublishAsync(DueAt, (SchedulerMessage)null!, untypedPipe, cancellationToken));
        await RejectsAsync("message", () => scheduler.SchedulePublishAsync(DueAt, (object)null!, cancellationToken));
        await RejectsAsync("message", () => scheduler.SchedulePublishAsync(DueAt, null!, typeof(SchedulerMessage), cancellationToken));
        await RejectsAsync("message", () => scheduler.SchedulePublishAsync(DueAt, (object)null!, untypedPipe, cancellationToken));
        await RejectsAsync("message", () => scheduler.SchedulePublishAsync(DueAt, null!, typeof(SchedulerMessage), untypedPipe, cancellationToken));
        await RejectsAsync("values", () => scheduler.SchedulePublishAsync<SchedulerMessage>(DueAt, (object)null!, cancellationToken));
        await RejectsAsync("values", () => scheduler.SchedulePublishAsync<SchedulerMessage>(DueAt, (object)null!, typedPipe, cancellationToken));
        await RejectsAsync("values", () => scheduler.SchedulePublishAsync<SchedulerMessage>(DueAt, (object)null!, untypedPipe, cancellationToken));
        await RejectsAsync("messageType", () => scheduler.SchedulePublishAsync(DueAt, message, (Type)null!, cancellationToken));
        await RejectsAsync("messageType", () => scheduler.SchedulePublishAsync(DueAt, message, (Type)null!, untypedPipe, cancellationToken));
        await RejectsAsync("pipe", () => scheduler.SchedulePublishAsync(DueAt, message, (IPipe<SendContext<SchedulerMessage>>)null!, cancellationToken));
        await RejectsAsync("pipe", () => scheduler.SchedulePublishAsync(DueAt, message, (IPipe<SendContext>)null!, cancellationToken));
        await RejectsAsync("pipe", () => scheduler.SchedulePublishAsync(DueAt, (object)message, (IPipe<SendContext>)null!, cancellationToken));
        await RejectsAsync("pipe", () => scheduler.SchedulePublishAsync(DueAt, message, typeof(SchedulerMessage), null!, cancellationToken));
        await RejectsAsync("pipe", () => scheduler.SchedulePublishAsync<SchedulerMessage>(DueAt, new { }, (IPipe<SendContext<SchedulerMessage>>)null!, cancellationToken));
        await RejectsAsync("pipe", () => scheduler.SchedulePublishAsync<SchedulerMessage>(DueAt, new { }, (IPipe<SendContext>)null!, cancellationToken));

        Assert.Equal("tokenId", (await Assert.ThrowsAsync<ArgumentException>(() =>
            scheduler.CancelScheduledPublishAsync<SchedulerMessage>(Guid.Empty, cancellationToken))).ParamName);
        Assert.Equal("messageType", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scheduler.CancelScheduledPublishAsync(null!, NewId.NextGuid(), cancellationToken))).ParamName);
        Assert.Equal("tokenId", (await Assert.ThrowsAsync<ArgumentException>(() =>
            scheduler.CancelScheduledPublishAsync(typeof(SchedulerMessage), Guid.Empty, cancellationToken))).ParamName);
        Assert.Equal("destinationAddress", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            scheduler.CancelScheduledSendAsync(null!, NewId.NextGuid(), cancellationToken))).ParamName);
        Assert.Equal("tokenId", (await Assert.ThrowsAsync<ArgumentException>(() =>
            scheduler.CancelScheduledSendAsync(Destination, Guid.Empty, cancellationToken))).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-SCHEDULER", "cancellation-is-honored-before-queueing")]
    public async Task PreCanceledOperations_DoNotResolveTheSchedulerOrQueueWorkAsync()
    {
        ConsumeContext consumeContext = InMemoryOutboxTestContextFactory.Create(
            new SchedulerMessage(),
            TestContext.Current.CancellationToken).Advanced();
        MessageSchedulerFactory rejectingFactory = _ =>
            throw new InvalidOperationException("A pre-canceled operation must not resolve the scheduler.");
        var scheduler = new InMemoryOutboxMessageSchedulerContext(
            consumeContext,
            rejectingFactory,
            new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        OperationCanceledException one = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            scheduler.CancelScheduledPublishAsync<SchedulerMessage>(NewId.NextGuid(), cancellation.Token));
        OperationCanceledException all = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            scheduler.CancelAllScheduledMessagesAsync(cancellation.Token));

        Assert.Equal(cancellation.Token, one.CancellationToken);
        Assert.Equal(cancellation.Token, all.CancellationToken);
    }

    private static async Task RejectsAsync(string parameterName, Func<Task> operation)
    {
        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(operation);
        Assert.Equal(parameterName, exception.ParamName);
    }

    public sealed record SchedulerMessage;
}
