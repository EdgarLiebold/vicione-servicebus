using ViciOne.ServiceBus.Batching;
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
        var batch = new MessageBatch<BatchItem>(
            receivedAt,
            receivedAt.AddSeconds(1),
            BatchCompletionMode.Size,
            [first, second]);

        var context = new BatchConsumeContext<BatchItem>((ConsumeContext)(object)first, batch);

        Assert.Same(batch, context.Message);
        Assert.Equal(BatchCompletionMode.Size, context.Message.Mode);
        Assert.Equal(receivedAt, context.Message.FirstMessageReceived);
        Assert.Equal(receivedAt.AddSeconds(1), context.Message.LastMessageReceived);
        Assert.Equal(new[] { "first", "second" }, context.Message.Select(item => item.Message.Value));
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => new BatchConsumeContext<BatchItem>(null!, batch)).ParamName);
        Assert.Equal(
            "batch",
            Assert.Throws<ArgumentNullException>(() => new BatchConsumeContext<BatchItem>((ConsumeContext)(object)first, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CONSUME-CONTEXT", "fault-suppression-validation-and-cancellation")]
    public async Task FaultNotification_ValidatesInputsAndPreservesCancellationAsync()
    {
        ConsumeContext<BatchItem> itemContext = CreateContext("item");
        var batch = new MessageBatch<BatchItem>(
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            BatchCompletionMode.Size,
            [itemContext]);
        var context = new BatchConsumeContext<BatchItem>((ConsumeContext)(object)itemContext, batch);
        var failure = new InvalidOperationException("consumer failed");
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await context.NotifyFaultedAsync(TimeSpan.FromSeconds(1), "batch-consumer", failure, cancellationToken);
        await context.NotifyFaultedAsync(itemContext, TimeSpan.FromSeconds(1), "item-consumer", failure, cancellationToken);

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

    public sealed record BatchItem(string Value);
}
