using System.Reflection;
using ViciOne.ServiceBus.Batching.Contexts;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers.Batching;

public sealed class BatchConsumeContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CONSUME-CONTEXT", "snapshot-metadata-message-and-construction-boundaries")]
    public void Context_ExposesTheExactBatchAndRejectsMissingCollaborators()
    {
        ConsumeContext<BatchItem> first = CreateContext("first");
        ConsumeContext<BatchItem> second = CreateContext("second");
        var receivedAt = new DateTimeOffset(2042, 3, 4, 5, 6, 7, TimeSpan.Zero);
        ConsumeContext<BatchItem>[] source = [first, second];
        var batch = new MessageBatch<BatchItem>(
            receivedAt,
            receivedAt.AddSeconds(1),
            BatchCompletionMode.Size,
            source);
        source[0] = second;

        var context = new BatchConsumeContext<BatchItem>((ConsumeContext)(object)first, batch);

        Assert.Same(batch, context.Message);
        Assert.Equal(BatchCompletionMode.Size, context.Message.Mode);
        Assert.Equal(receivedAt, context.Message.FirstMessageReceived);
        Assert.Equal(receivedAt.AddSeconds(1), context.Message.LastMessageReceived);
        Assert.Equal(2, context.Message.Count);
        Assert.Same(first, context.Message[0]);
        Assert.Same(second, context.Message[1]);
        Assert.Equal(new[] { "first", "second" }, context.Message.Select(item => item.Message.Value));
        Assert.Equal(
            new[] { "first", "second" },
            ((System.Collections.IEnumerable)context.Message)
                .Cast<ConsumeContext<BatchItem>>()
                .Select(item => item.Message.Value));
        System.Collections.IEnumerator untypedEnumerator =
            ((System.Collections.IEnumerable)context.Message).GetEnumerator();
        Assert.True(untypedEnumerator.MoveNext());
        Assert.Same(first, untypedEnumerator.Current);
        Assert.True(untypedEnumerator.MoveNext());
        Assert.Same(second, untypedEnumerator.Current);
        Assert.False(untypedEnumerator.MoveNext());
        (untypedEnumerator as IDisposable)?.Dispose();
        Assert.Equal(
            "messages",
            Assert.Throws<ArgumentNullException>(() => new MessageBatch<BatchItem>(receivedAt, receivedAt, BatchCompletionMode.Size, null!))
                .ParamName);
        Assert.Equal(
            "mode",
            Assert.Throws<ArgumentOutOfRangeException>(() => new MessageBatch<BatchItem>(receivedAt, receivedAt, (BatchCompletionMode)42, []))
                .ParamName);
        Assert.Equal(
            "messages",
            Assert.Throws<ArgumentException>(() => new MessageBatch<BatchItem>(receivedAt, receivedAt, BatchCompletionMode.Size, []))
                .ParamName);
        Assert.Equal(
            "messages",
            Assert.Throws<ArgumentException>(() =>
                new MessageBatch<BatchItem>(receivedAt, receivedAt, BatchCompletionMode.Size, [null!]))
                .ParamName);
        Assert.Equal(
            "lastMessageReceived",
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new MessageBatch<BatchItem>(receivedAt, receivedAt.AddTicks(-1), BatchCompletionMode.Size, [first]))
                .ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => new BatchConsumeContext<BatchItem>(null!, batch)).ParamName);
        Assert.Equal(
            "batch",
            Assert.Throws<ArgumentNullException>(() => new BatchConsumeContext<BatchItem>((ConsumeContext)(object)first, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CONSUME-CONTEXT", "fault-suppression-validation-and-cancellation")]
    public async Task FaultNotification_ValidatesInputsAndPreservesCancellationAsync()
    {
        ConsumeContext<BatchItem> itemContext = CreateRecordingContext(new BatchItem("item"), out RecordingConsumeContext proxy);
        var batch = new MessageBatch<BatchItem>(
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            BatchCompletionMode.Size,
            [itemContext]);
        var context = new BatchConsumeContext<BatchItem>((ConsumeContext)(object)itemContext, batch);
        var failure = new InvalidOperationException("consumer failed");
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await context.NotifyConsumedAsync(TimeSpan.FromSeconds(1), "batch-consumer", cancellationToken);
        await context.NotifyConsumedAsync(itemContext, TimeSpan.FromSeconds(1), "item-consumer", cancellationToken);
        await context.NotifyFaultedAsync(TimeSpan.FromSeconds(1), "batch-consumer", failure, cancellationToken);
        await context.NotifyFaultedAsync(itemContext, TimeSpan.FromSeconds(1), "item-consumer", failure, cancellationToken);

        Assert.Collection(
            proxy.ConsumedNotifications,
            notification =>
            {
                Assert.Same(context, notification.Context);
                Assert.Equal(typeof(IMessageBatch<BatchItem>), notification.MessageType);
                Assert.Equal(TimeSpan.FromSeconds(1), notification.Duration);
                Assert.Equal("batch-consumer", notification.ConsumerType);
                Assert.Equal(cancellationToken, notification.CancellationToken);
            },
            notification =>
            {
                Assert.Same(itemContext, notification.Context);
                Assert.Equal(typeof(BatchItem), notification.MessageType);
                Assert.Equal(TimeSpan.FromSeconds(1), notification.Duration);
                Assert.Equal("item-consumer", notification.ConsumerType);
                Assert.Equal(cancellationToken, notification.CancellationToken);
            });

        Assert.Equal(
            "consumerType",
            (await Assert.ThrowsAsync<ArgumentException>(() =>
                context.NotifyConsumedAsync(TimeSpan.Zero, " ", cancellationToken))).ParamName);
        Assert.Equal(
            "context",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                context.NotifyConsumedAsync<BatchItem>(null!, TimeSpan.Zero, "consumer", cancellationToken))).ParamName);
        Assert.Equal(
            "consumerType",
            (await Assert.ThrowsAsync<ArgumentException>(() =>
                context.NotifyFaultedAsync(TimeSpan.Zero, " ", failure, cancellationToken))).ParamName);
        Assert.Equal(
            "exception",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                context.NotifyFaultedAsync(TimeSpan.Zero, "consumer", null!, cancellationToken))).ParamName);
        Assert.Equal(
            "context",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                context.NotifyFaultedAsync<BatchItem>(null!, TimeSpan.Zero, "consumer", failure, cancellationToken))).ParamName);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Task canceledBatch = context.NotifyFaultedAsync(TimeSpan.Zero, "consumer", failure, cancellation.Token);
        Task canceledItem = context.NotifyFaultedAsync(itemContext, TimeSpan.Zero, "consumer", failure, cancellation.Token);
        OperationCanceledException batchException = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledBatch);
        OperationCanceledException itemException = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledItem);
        Assert.Equal(cancellation.Token, batchException.CancellationToken);
        Assert.Equal(cancellation.Token, itemException.CancellationToken);
    }

    private static ConsumeContext<BatchItem> CreateContext(string value) =>
        InMemoryOutboxTestContextFactory.Create(new BatchItem(value));

    private static ConsumeContext<BatchItem> CreateRecordingContext(
        BatchItem message,
        out RecordingConsumeContext proxy)
    {
        IRecordingConsumeContext context = DispatchProxy.Create<IRecordingConsumeContext, RecordingConsumeContext>();
        proxy = (RecordingConsumeContext)(object)context;
        proxy.Inner = InMemoryOutboxTestContextFactory.Create(message);
        return context;
    }

    private interface IRecordingConsumeContext :
        ConsumeContext,
        ConsumeContext<BatchItem>
    {
    }

    private class RecordingConsumeContext : DispatchProxy
    {
        public ConsumeContext<BatchItem> Inner { get; set; } = null!;

        public List<ConsumedNotification> ConsumedNotifications { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == "NotifyConsumedAsync" && targetMethod.IsGenericMethod)
            {
                ArgumentNullException.ThrowIfNull(args);
                ConsumedNotifications.Add(new ConsumedNotification(
                    args[0]!,
                    targetMethod.GetGenericArguments()[0],
                    (TimeSpan)args[1]!,
                    (string)args[2]!,
                    (CancellationToken)args[3]!));
                return Task.CompletedTask;
            }

            return targetMethod.Invoke(Inner, args);
        }
    }

    private sealed record ConsumedNotification(
        object Context,
        Type MessageType,
        TimeSpan Duration,
        string ConsumerType,
        CancellationToken CancellationToken);

    public sealed record BatchItem(string Value);
}
