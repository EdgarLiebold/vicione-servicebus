using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport.Configuration;

public sealed class RabbitMqQueueConfigurationBoundaryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-QUEUE-CONFIGURATION", "invalid-quorum-factor-preserves-classic-queue")]
    public void InvalidQuorumFactor_PreservesClassicQueueAndAllowsSubsequentValidConfiguration()
    {
        RabbitMqReceiveSettings? settings = null;

        _ = Bus.Factory.CreateUsingRabbitMq(configurator => configurator.ReceiveEndpoint("quorum-boundary", endpoint =>
        {
            endpoint.Exclusive = true;
            endpoint.EnablePriority(7);
            endpoint.SetQueueArgument(RabbitMQ.Client.Headers.XQueueType, "classic");

            ArgumentOutOfRangeException failure = Assert.Throws<ArgumentOutOfRangeException>(
                () => endpoint.SetQuorumQueue(0));
            Assert.Equal("replicationFactor", failure.ParamName);

            settings = Assert.IsType<RabbitMqReceiveSettings>(
                Assert.IsType<RabbitMqReceiveEndpointConfiguration>(endpoint).Settings);
            Assert.True(settings.Exclusive);
            Assert.Equal("classic", settings.QueueArguments[RabbitMQ.Client.Headers.XQueueType]);
            Assert.Equal(7, settings.QueueArguments[RabbitMQ.Client.Headers.XMaxPriority]);
            Assert.False(settings.QueueArguments.ContainsKey(RabbitMQ.Client.Headers.XQuorumInitialGroupSize));

            Assert.Throws<ArgumentOutOfRangeException>(() => endpoint.SetQuorumQueue(-1));
            Assert.True(settings.Exclusive);
            Assert.Equal("classic", settings.QueueArguments[RabbitMQ.Client.Headers.XQueueType]);
            Assert.Equal(7, settings.QueueArguments[RabbitMQ.Client.Headers.XMaxPriority]);

            endpoint.SetQuorumQueue();
            Assert.False(settings.Exclusive);
            Assert.Equal("quorum", settings.QueueArguments[RabbitMQ.Client.Headers.XQueueType]);
            Assert.False(settings.QueueArguments.ContainsKey(RabbitMQ.Client.Headers.XQuorumInitialGroupSize));
            Assert.False(settings.QueueArguments.ContainsKey(RabbitMQ.Client.Headers.XMaxPriority));

            endpoint.SetQuorumQueue(3);
        }));

        RabbitMqReceiveSettings actual = Assert.IsType<RabbitMqReceiveSettings>(settings);
        Assert.False(actual.Exclusive);
        Assert.Equal("quorum", actual.QueueArguments[RabbitMQ.Client.Headers.XQueueType]);
        Assert.Equal(3, actual.QueueArguments[RabbitMQ.Client.Headers.XQuorumInitialGroupSize]);
        Assert.False(actual.QueueArguments.ContainsKey(RabbitMQ.Client.Headers.XMaxPriority));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-QUEUE-CONFIGURATION", "submillisecond-acknowledgement-timeout-preserves-existing-value")]
    public void SubmillisecondAcknowledgementTimeout_RejectsWithoutReplacingAValidValue()
    {
        RabbitMqReceiveSettings? settings = null;

        _ = Bus.Factory.CreateUsingRabbitMq(configurator => configurator.ReceiveEndpoint("timeout-boundary", endpoint =>
        {
            endpoint.SetDeliveryAcknowledgementTimeout(TimeSpan.FromMinutes(2));

            ArgumentOutOfRangeException submillisecond = Assert.Throws<ArgumentOutOfRangeException>(
                () => endpoint.SetDeliveryAcknowledgementTimeout(TimeSpan.FromTicks(1)));
            Assert.Equal("timeSpan", submillisecond.ParamName);
            Assert.Contains("whole milliseconds", submillisecond.Message, StringComparison.Ordinal);

            settings = Assert.IsType<RabbitMqReceiveSettings>(
                Assert.IsType<RabbitMqReceiveEndpointConfiguration>(endpoint).Settings);
            Assert.Equal(120000L, settings.QueueArguments["x-consumer-timeout"]);

            endpoint.SetDeliveryAcknowledgementTimeout(TimeSpan.FromMinutes(3));
            ArgumentOutOfRangeException fractional = Assert.Throws<ArgumentOutOfRangeException>(
                () => endpoint.SetDeliveryAcknowledgementTimeout(TimeSpan.FromMinutes(2) + TimeSpan.FromTicks(1)));
            Assert.Equal("timeSpan", fractional.ParamName);
            Assert.Contains("whole milliseconds", fractional.Message, StringComparison.Ordinal);

            Assert.Equal(180000L, settings.QueueArguments["x-consumer-timeout"]);
        }));

        Assert.NotNull(settings);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-QUEUE-CONFIGURATION", "large-whole-millisecond-timeout-projects-exactly")]
    public void LargeWholeMillisecondAcknowledgementTimeout_DoesNotLosePrecision()
    {
        const long milliseconds = 500_000_000_000_007;
        RabbitMqReceiveSettings? settings = null;

        _ = Bus.Factory.CreateUsingRabbitMq(configurator => configurator.ReceiveEndpoint("large-timeout", endpoint =>
        {
            endpoint.SetDeliveryAcknowledgementTimeout(TimeSpan.FromTicks(milliseconds * TimeSpan.TicksPerMillisecond));
            settings = Assert.IsType<RabbitMqReceiveSettings>(
                Assert.IsType<RabbitMqReceiveEndpointConfiguration>(endpoint).Settings);
        }));

        Assert.Equal(milliseconds, Assert.IsType<RabbitMqReceiveSettings>(settings).QueueArguments["x-consumer-timeout"]);
    }
}
