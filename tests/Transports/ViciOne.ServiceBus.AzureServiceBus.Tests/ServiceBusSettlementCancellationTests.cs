using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusSettlementCancellationTests
{
    [Theory]
    [InlineData(SettlementOperation.Complete)]
    [InlineData(SettlementOperation.Abandon)]
    [InlineData(SettlementOperation.DeadLetter)]
    [InlineData(SettlementOperation.DeadLetterWithException)]
    [RequirementCoverage("REQ-VSB-ASB-SETTLEMENT-CANCELLATION", "session-settlement-pre-canceled")]
    public async Task SessionSettlement_PreCanceledCallerToken_DoesNotInvokeTheSdkAsync(SettlementOperation operation)
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var context = new ServiceBusSessionMessageLockContext(
            session: null!,
            message: null!,
            cancellationToken: CancellationToken.None);

        Task settlement = operation switch
        {
            SettlementOperation.Complete => context.CompleteAsync(cancellationSource.Token),
            SettlementOperation.Abandon => context.AbandonAsync(new InvalidOperationException("failure"), cancellationSource.Token),
            SettlementOperation.DeadLetter => context.DeadLetterAsync(cancellationSource.Token),
            SettlementOperation.DeadLetterWithException => context.DeadLetterAsync(
                new InvalidOperationException("failure"),
                cancellationSource.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => settlement);

        Assert.Equal(cancellationSource.Token, exception.CancellationToken);
        Assert.Equal(TaskStatus.Canceled, settlement.Status);
    }

    public enum SettlementOperation
    {
        Complete,
        Abandon,
        DeadLetter,
        DeadLetterWithException
    }
}
