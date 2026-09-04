using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware.Outbox;

public sealed class InMemoryOutboxMessageRepositoryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-INBOX", "held-message-lock-honors-requested-cancellation")]
    public async Task HeldMessageLock_HonorsAnAlreadyRequestedCancellationAsync()
    {
        var repository = new InMemoryOutboxMessageRepository();
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
}
