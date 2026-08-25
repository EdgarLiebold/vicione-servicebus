using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Contexts;

public sealed class SentMessageMetadataTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SENT-TIME", "exact-utc-new-id-timestamp")]
    public async Task ConsumedSentTime_IsTheExactUtcTimestampOfItsMessageId()
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

        await harness.Start(cancellationToken);
        try
        {
            await harness.Bus.Publish(new TimedMessage("value"), cancellationToken);
            ConsumeContext<TimedMessage> context =
                (await handler.Consumed.SelectAsync(cancellationToken).First()).Context;

            Guid messageId = Assert.IsType<Guid>(context.MessageId);
            DateTime sentTime = Assert.IsType<DateTime>(context.SentTime);
            Assert.Equal(DateTimeKind.Utc, sentTime.Kind);
            Assert.Equal(messageId.ToNewId().Timestamp, sentTime);
        }
        finally
        {
            await harness.Stop();
        }
    }

    private sealed record TimedMessage(string Value);
}
