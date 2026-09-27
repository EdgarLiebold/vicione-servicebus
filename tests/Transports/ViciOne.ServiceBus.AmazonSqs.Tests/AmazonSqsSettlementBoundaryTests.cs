using Amazon.SQS.Model;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsSettlementBoundaryTests
{
    private static readonly Uri InputAddress = new("amazonsqs://eu-central-1/orders");
    private const string QueueUrl = "https://sqs.eu-central-1.amazonaws.com/123456789012/orders";
    private static TimeSpan Timeout => TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-VISIBILITY", "renewal-failure-stops-and-retains-redelivery-identity")]
    public async Task RenewalFailure_StopsRenewalAndRetainsFaultSettlementIdentityAsync(int failureKind)
    {
        var clock = new FakeTimeProvider();
        QueueReceiveSettings settings = CreateSettings();
        settings.VisibilityTimeout = 0;
        settings.RedeliverVisibilityTimeout = 17;
        var visibility = new List<(string Queue, string Receipt, int Seconds)>();
        var deletions = 0;
        Exception failure = failureKind switch
        {
            0 => new MessageNotInflightException("no longer in flight"),
            1 => new ReceiptHandleIsInvalidException("stale receipt"),
            _ => new InvalidOperationException("provider renewal failure")
        };
        var context = new AmazonSqsReceiveLockContext(InputAddress,
            new Message { ReceiptHandle = "original-receipt" }, settings, CancellationToken.None, clock,
            (queue, receipt, seconds, _) =>
            {
                visibility.Add((queue, receipt, seconds));
                return visibility.Count == 1 ? Task.FromException(failure) : Task.CompletedTask;
            },
            (_, _, _) => { deletions++; return Task.CompletedTask; }, () => false);

        TransportException lost = await Assert.ThrowsAsync<TransportException>(() => context.ValidateLockStatusAsync(TestContext.Current.CancellationToken));
        Assert.Contains("original-receipt", lost.Message, StringComparison.Ordinal);
        Assert.Single(visibility);
        Assert.Equal(QueueUrl, visibility[0].Queue);
        Assert.Equal("original-receipt", visibility[0].Receipt);
        Assert.Equal(settings.MaxVisibilityTimeoutRenewal, visibility[0].Seconds);

        settings.QueueUrl = "https://mutated.invalid/queue";
        settings.RedeliverVisibilityTimeout = 99;
        clock.Advance(TimeSpan.FromHours(1));
        Assert.Single(visibility);
        await context.FaultedAsync(new InvalidOperationException("consumer failure"), TestContext.Current.CancellationToken);
        clock.Advance(TimeSpan.FromHours(1));

        Assert.Equal(2, visibility.Count);
        Assert.Equal((QueueUrl, "original-receipt", 17), visibility[1]);
        Assert.Equal(0, deletions);
        await Assert.ThrowsAsync<TransportException>(() => context.ValidateLockStatusAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-VISIBILITY", "pre-canceled-settlement-retains-active-delivery")]
    public async Task CanceledSettlement_LeavesTheActiveDeliveryAvailableForAnotherCallerAsync(bool faulted)
    {
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        var clock = new FakeTimeProvider();
        QueueReceiveSettings settings = CreateSettings();
        settings.VisibilityTimeout = 30;
        var visibility = 0;
        var deleted = new List<(string Entity, string Receipt)>();
        var context = new AmazonSqsReceiveLockContext(InputAddress,
            new Message { ReceiptHandle = "retained-receipt" }, settings, CancellationToken.None, clock,
            (_, _, _, _) => { visibility++; return Task.CompletedTask; },
            (entity, receipt, _) => { deleted.Add((entity, receipt)); return Task.CompletedTask; }, () => false);

        try
        {
            Task operation = faulted
                ? context.FaultedAsync(new InvalidOperationException("consumer failed"), canceled.Token)
                : context.CompleteAsync(canceled.Token);
            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
            Assert.Equal(canceled.Token, actual.CancellationToken);
            Assert.Empty(deleted);
            Assert.Equal(0, visibility);
            await context.ValidateLockStatusAsync(TestContext.Current.CancellationToken);
        }
        finally
        {
            await context.CompleteAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }

        clock.Advance(TimeSpan.FromHours(1));
        Assert.Equal(new[] { ("orders", "retained-receipt") }, deleted);
        Assert.Equal(0, visibility);
        await Assert.ThrowsAsync<TransportException>(() => context.ValidateLockStatusAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-VISIBILITY", "settlement-awaits-canceled-renewal-drain")]
    public async Task Settlement_DrainsCanceledRenewalBeforeAnyTerminalProviderEffectAsync(bool faulted)
    {
        var clock = new FakeTimeProvider();
        QueueReceiveSettings settings = CreateSettings();
        settings.VisibilityTimeout = 0;
        settings.RedeliverVisibilityTimeout = 13;
        var releaseRenewal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var renewalCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var events = new List<string>();
        var visibilityCount = 0;
        var context = new AmazonSqsReceiveLockContext(InputAddress,
            new Message { ReceiptHandle = "draining-receipt" }, settings, CancellationToken.None, clock,
            async (queue, receipt, seconds, token) =>
            {
                Assert.Equal(QueueUrl, queue);
                Assert.Equal("draining-receipt", receipt);
                if (++visibilityCount == 1)
                {
                    events.Add("renewal-entered");
                    using var registration = token.Register(() => renewalCanceled.TrySetResult());
                    await releaseRenewal.Task;
                    events.Add("renewal-ended");
                    token.ThrowIfCancellationRequested();
                }
                else
                {
                    Assert.Equal(13, seconds);
                    events.Add("redelivery");
                }
            },
            (entity, receipt, _) =>
            {
                Assert.Equal("orders", entity);
                Assert.Equal("draining-receipt", receipt);
                events.Add("delete");
                return Task.CompletedTask;
            }, () => false);

        Task settlement = faulted
            ? context.FaultedAsync(new InvalidOperationException("consumer failed"), TestContext.Current.CancellationToken)
            : context.CompleteAsync(TestContext.Current.CancellationToken);
        try
        {
            await renewalCanceled.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            Assert.False(settlement.IsCompleted);
            Assert.Equal(new[] { "renewal-entered" }, events);
        }
        finally
        {
            releaseRenewal.TrySetResult();
            await settlement.WaitAsync(Timeout, CancellationToken.None);
        }

        clock.Advance(TimeSpan.FromHours(1));
        Assert.Equal(new[] { "renewal-entered", "renewal-ended", faulted ? "redelivery" : "delete" }, events);
        Assert.Equal(faulted ? 2 : 1, visibilityCount);
        await Assert.ThrowsAsync<TransportException>(() => context.ValidateLockStatusAsync(TestContext.Current.CancellationToken));
    }

    private static QueueReceiveSettings CreateSettings()
    {
        var topology = new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology());
        var parent = new AmazonSqsEndpointConfiguration(topology);
        return new QueueReceiveSettings(parent.CreateEndpointConfiguration(false), "orders", true, false)
        {
            QueueUrl = QueueUrl,
            MaxVisibilityTimeoutRenewal = 60,
            MaxVisibilityTimeout = TimeSpan.FromHours(12)
        };
    }
}
