using ViciOne.ServiceBus.Tests.InternalAccess.DurableSend;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DurableSend;

public sealed class InMemoryConsumerCommitTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 25, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(1);

    [Theory]
    [InlineData(1, 4)]
    [InlineData(2, 3)]
    [RequirementCoverage("REQ-VSB-RELIABLE-INBOX", "consumer-commit-count-and-byte-capacity-rollback")]
    public async Task CapacityFailureLeavesInboxOwnedAndOutboxEmptySoSameAttemptCanCommitAsync(
        int maximumStoredCount,
        long maximumStoredBytes)
    {
        var driver = new InMemoryConsumerCommitTestDriver<ITestBus>();
        ReliableInboxKey key = Key(1);
        ReliableInboxLease lease = await AcquireLeaseAsync(driver, key);
        SerializedDurableSend first = Message(1, [1, 2]);
        SerializedDurableSend second = Message(2, [3, 4]);
        var limits = new DurableSendStoreLimits(maximumStoredCount, maximumStoredBytes);

        DurableSendCapacityExceededException failure = Assert.Throws<DurableSendCapacityExceededException>(
            () => driver.CompleteConsumer(key, lease, [first, second], limits, Now));

        Assert.Equal(0, failure.StoredCount);
        Assert.Equal(0, failure.StoredBytes);
        Assert.Equal(0, (await driver.Outbox.GetSnapshotAsync(TestContext.Current.CancellationToken)).StoredCount);
        Assert.Equal(ReliableInboxAcquireDisposition.Busy,
            (await driver.Inbox.AcquireAsync(key, Now.AddSeconds(1), LeaseDuration, TestContext.Current.CancellationToken)).Disposition);

        driver.CompleteConsumer(key, lease, [first], limits, Now.AddSeconds(2));

        Assert.Equal(ReliableInboxAcquireDisposition.AlreadyConsumed,
            (await driver.Inbox.AcquireAsync(key, Now.AddMinutes(2), LeaseDuration, TestContext.Current.CancellationToken)).Disposition);
        DurableSendDelivery saved = Assert.Single(await driver.Outbox.ClaimDueAsync(
            Now.AddSeconds(2), 10, LeaseDuration, TestContext.Current.CancellationToken));
        Assert.Equal(first.Id, saved.Message.Id);
        Assert.Equal(first.Body.ToArray(), saved.Message.Body.ToArray());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-INBOX", "consumer-commit-existing-intent-conflict-rollback")]
    public async Task ConflictInLaterMessageRollsBackEarlierAdditionAndAllowsCorrectedCommitAsync()
    {
        var driver = new InMemoryConsumerCommitTestDriver<ITestBus>();
        ReliableInboxKey key = Key(2);
        ReliableInboxLease lease = await AcquireLeaseAsync(driver, key);
        SerializedDurableSend existing = Message(2, [7]);
        SerializedDurableSend staged = Message(1, [1]);
        var limits = new DurableSendStoreLimits(3, 10);
        await driver.Outbox.AdmitAsync(existing, limits, Now, TestContext.Current.CancellationToken);

        DurableSendIdentityConflictException conflict = Assert.Throws<DurableSendIdentityConflictException>(
            () => driver.CompleteConsumer(key, lease, [staged, existing with { Body = new byte[] { 9 } }], limits, Now));

        Assert.Equal(existing.Id, conflict.Id);
        Assert.Equal(1, (await driver.Outbox.GetSnapshotAsync(TestContext.Current.CancellationToken)).StoredCount);
        Assert.Equal(ReliableInboxAcquireDisposition.Busy,
            (await driver.Inbox.AcquireAsync(key, Now.AddSeconds(1), LeaseDuration, TestContext.Current.CancellationToken)).Disposition);

        driver.CompleteConsumer(key, lease, [staged, existing], limits, Now.AddSeconds(2));

        Assert.Equal(2, (await driver.Outbox.GetSnapshotAsync(TestContext.Current.CancellationToken)).StoredCount);
        Assert.Equal(ReliableInboxAcquireDisposition.AlreadyConsumed,
            (await driver.Inbox.AcquireAsync(key, Now.AddMinutes(2), LeaseDuration, TestContext.Current.CancellationToken)).Disposition);
        IReadOnlyList<DurableSendDelivery> saved = await driver.Outbox.ClaimDueAsync(
            Now.AddSeconds(2), 10, LeaseDuration, TestContext.Current.CancellationToken);
        Assert.Equal([existing.Id, staged.Id], saved.Select(item => item.Message.Id));
        Assert.Equal(new byte[] { 7 }, saved.Single(item => item.Message.Id == existing.Id).Message.Body.ToArray());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-INBOX", "consumer-commit-fenced-lease-and-idempotent-batch")]
    public async Task DuplicateIntentUsesOneCapacitySlotAndExpiredLeaseCannotCommitAsync()
    {
        var driver = new InMemoryConsumerCommitTestDriver<ITestBus>();
        ReliableInboxKey key = Key(3);
        ReliableInboxLease oldLease = await AcquireLeaseAsync(driver, key);
        SerializedDurableSend message = Message(3, [5]);
        var limits = new DurableSendStoreLimits(1, 1);
        ReliableInboxAcquireResult takeover = await driver.Inbox.AcquireAsync(
            key, Now.AddMinutes(1), LeaseDuration, TestContext.Current.CancellationToken);
        ReliableInboxLease newLease = Assert.IsType<ReliableInboxLease>(takeover.Lease);

        Assert.Throws<InvalidOperationException>(() =>
            driver.CompleteConsumer(key, oldLease, [message], limits, Now.AddMinutes(1)));
        Assert.Equal(0, (await driver.Outbox.GetSnapshotAsync(TestContext.Current.CancellationToken)).StoredCount);

        driver.CompleteConsumer(key, newLease, [message, message with { Body = new byte[] { 5 } }], limits, Now.AddMinutes(1));

        DurableSendStoreSnapshot snapshot = await driver.Outbox.GetSnapshotAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, snapshot.StoredCount);
        Assert.Equal(1, snapshot.StoredBytes);
        Assert.Equal(ReliableInboxAcquireDisposition.AlreadyConsumed,
            (await driver.Inbox.AcquireAsync(key, Now.AddMinutes(3), LeaseDuration, TestContext.Current.CancellationToken)).Disposition);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-INBOX", "consumer-commit-batch-intent-conflict-rollback")]
    public async Task ConflictingNewIdWithinBatchDoesNotCommitAnyMessageOrConsumeInboxAsync()
    {
        var driver = new InMemoryConsumerCommitTestDriver<ITestBus>();
        ReliableInboxKey key = Key(4);
        ReliableInboxLease lease = await AcquireLeaseAsync(driver, key);
        SerializedDurableSend first = Message(4, [4]);
        SerializedDurableSend second = Message(5, [5]);
        var limits = new DurableSendStoreLimits(2, 2);

        DurableSendIdentityConflictException conflict = Assert.Throws<DurableSendIdentityConflictException>(
            () => driver.CompleteConsumer(
                key, lease, [first, second, first with { Body = new byte[] { 9 } }], limits, Now));

        Assert.Equal(first.Id, conflict.Id);
        Assert.Equal(0, (await driver.Outbox.GetSnapshotAsync(TestContext.Current.CancellationToken)).StoredCount);
        Assert.Equal(ReliableInboxAcquireDisposition.Busy,
            (await driver.Inbox.AcquireAsync(key, Now.AddSeconds(1), LeaseDuration, TestContext.Current.CancellationToken)).Disposition);

        driver.CompleteConsumer(key, lease, [first, second], limits, Now.AddSeconds(2));
        Assert.Equal(2, (await driver.Outbox.GetSnapshotAsync(TestContext.Current.CancellationToken)).StoredCount);
        Assert.Equal(ReliableInboxAcquireDisposition.AlreadyConsumed,
            (await driver.Inbox.AcquireAsync(key, Now.AddMinutes(2), LeaseDuration, TestContext.Current.CancellationToken)).Disposition);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-INBOX", "consumer-commit-missing-inbox-has-no-outbox-side-effect")]
    public async Task MissingInboxCannotCreateOutboxRecordAsync()
    {
        var driver = new InMemoryConsumerCommitTestDriver<ITestBus>();
        ReliableInboxKey key = Key(5);
        ReliableInboxLease lease = new(Guid.NewGuid(), Now.Add(LeaseDuration));

        Assert.Throws<InvalidOperationException>(() => driver.CompleteConsumer(
            key, lease, [Message(6, [6])], new DurableSendStoreLimits(1, 1), Now));

        Assert.Equal(0, (await driver.Outbox.GetSnapshotAsync(TestContext.Current.CancellationToken)).StoredCount);
        Assert.Equal(ReliableInboxAcquireDisposition.Acquired,
            (await driver.Inbox.AcquireAsync(key, Now, LeaseDuration, TestContext.Current.CancellationToken)).Disposition);
    }

    private static async Task<ReliableInboxLease> AcquireLeaseAsync(
        InMemoryConsumerCommitTestDriver<ITestBus> driver,
        ReliableInboxKey key)
    {
        ReliableInboxAcquireResult result = await driver.Inbox.AcquireAsync(
            key, Now, LeaseDuration, TestContext.Current.CancellationToken);
        Assert.Equal(ReliableInboxAcquireDisposition.Acquired, result.Disposition);
        return Assert.IsType<ReliableInboxLease>(result.Lease);
    }

    private static ReliableInboxKey Key(int value) => new(GuidFrom(value + 100), GuidFrom(200));

    private static SerializedDurableSend Message(int value, byte[] body) => new()
    {
        Id = new DurableSendId(GuidFrom(value)),
        ContractIdentity = new MessageContractIdentity("vicione.tests.consumer-commit", 1),
        DestinationAddress = new Uri("loopback://consumer-commit"),
        ContentType = "application/octet-stream",
        Body = body,
    };

    private static Guid GuidFrom(int value) => new(value, 0, 0, new byte[8]);

    private interface ITestBus : IBus;
}
