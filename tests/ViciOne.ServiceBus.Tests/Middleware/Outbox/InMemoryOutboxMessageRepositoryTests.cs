using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Middleware.Outbox.InMemory;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware.Outbox;

public sealed class InMemoryOutboxMessageRepositoryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-INBOX", "held-message-lock-honors-requested-cancellation")]
    public async Task HeldMessageLock_HonorsAnAlreadyRequestedCancellationAsync()
    {
        using var repository = new InMemoryOutboxMessageRepository();
        Guid messageId = NewId.NextGuid();
        Guid consumerId = NewId.NextGuid();
        InMemoryInboxMessage held = await repository.LockAsync(messageId, consumerId, CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        try
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => repository.LockAsync(messageId, consumerId, cancellation.Token));
        }
        finally
        {
            held.Release();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-INBOX", "repository-key-rejects-empty-identifiers")]
    public async Task Lock_RejectsEachEmptyIdentifierBeforeObservingCancellationAsync()
    {
        using var repository = new InMemoryOutboxMessageRepository();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        ArgumentException messageId = await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.LockAsync(Guid.Empty, NewId.NextGuid(), cancellation.Token));
        ArgumentException consumerId = await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.LockAsync(NewId.NextGuid(), Guid.Empty, cancellation.Token));

        Assert.Equal("messageId", messageId.ParamName);
        Assert.Equal("consumerId", consumerId.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-INBOX", "new-entry-uses-injected-clock")]
    public async Task NewInboxEntry_UsesTheInjectedClockAsync()
    {
        DateTimeOffset now = new(2043, 4, 5, 6, 7, 8, TimeSpan.Zero);
        using var repository = new InMemoryOutboxMessageRepository(new FakeTimeProvider(now));

        InMemoryInboxMessage entry = await repository.LockAsync(
            NewId.NextGuid(),
            NewId.NextGuid(),
            TestContext.Current.CancellationToken);

        try
        {
            Assert.Equal(now, entry.Received);
        }
        finally
        {
            entry.Release();
        }
    }
}
