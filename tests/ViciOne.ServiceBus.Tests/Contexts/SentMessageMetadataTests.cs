using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Contexts;

public sealed class SentMessageMetadataTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SENT-TIME", "exact-utc-new-id-timestamp")]
    public async Task ConsumedSentTime_IsTheExactUtcTimestampOfItsMessageIdAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        using var harness = new InMemoryTestHarness($"sent-time-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        HandlerTestHarness<TimedMessage> handler = harness.Handler<TimedMessage>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.Bus.PublishAsync(new TimedMessage("value"), cancellationToken);
            ConsumeContext<TimedMessage> context =
                (await handler.Consumed.SelectAsync(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context;

            Guid messageId = Assert.IsType<Guid>(context.MessageId);
            DateTimeOffset sentTime = Assert.IsType<DateTimeOffset>(context.SentTime);
            Assert.Equal(TimeSpan.Zero, sentTime.Offset);
            Assert.Equal(messageId.ToNewId().Timestamp, sentTime.UtcDateTime);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private sealed record TimedMessage(string Value);
}
