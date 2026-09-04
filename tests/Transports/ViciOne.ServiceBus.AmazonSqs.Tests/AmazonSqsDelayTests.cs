using ViciOne.ServiceBus.AmazonSqs;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsDelayTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-DELAY", "fractional-delay-rounds-up-within-service-boundary")]
    public void MessageDelay_RoundsUpWithoutDeliveringEarlyAndEnforcesTheSqsRange()
    {
        var context = new AmazonSqsMessageSendContext<Message>(new Message(), TestContext.Current.CancellationToken);

        context.DelaySeconds = 900;
        Assert.Equal(TimeSpan.FromSeconds(900), context.Delay);
        context.DelaySeconds = null;
        Assert.Null(context.Delay);

        Assert.Throws<ArgumentOutOfRangeException>(() => context.DelaySeconds = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => context.DelaySeconds = 901);
        Assert.Equal(1, AmazonSqsDelay.FromTimeSpan(TimeSpan.FromTicks(1)));
        Assert.Equal(2, AmazonSqsDelay.FromTimeSpan(TimeSpan.FromMilliseconds(1500)));
        Assert.Equal(900, AmazonSqsDelay.FromTimeSpan(TimeSpan.FromMilliseconds(899_001)));
        Assert.Throws<ArgumentOutOfRangeException>(() => AmazonSqsDelay.FromTimeSpan(TimeSpan.FromSeconds(900) + TimeSpan.FromTicks(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => AmazonSqsDelay.FromTimeSpan(TimeSpan.MaxValue));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-DELAY", "destination-capability-boundary")]
    public void FifoQueuesAndSnsTopics_RejectUnsupportedPerMessageDelay()
    {
        Assert.Equal(30, AmazonSqsDelay.ForQueue(TimeSpan.FromSeconds(30), isFifo: false));
        Assert.Null(AmazonSqsDelay.ForQueue(TimeSpan.Zero, isFifo: true));
        Assert.Throws<NotSupportedException>(() => AmazonSqsDelay.ForQueue(TimeSpan.FromSeconds(1), isFifo: true));
        Assert.Throws<NotSupportedException>(() => AmazonSqsDelay.EnsureNotSetForTopic(TimeSpan.FromSeconds(1)));
        AmazonSqsDelay.EnsureNotSetForTopic(null);
        AmazonSqsDelay.EnsureNotSetForTopic(TimeSpan.Zero);
    }

    private sealed record Message;
}
