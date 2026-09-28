using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport.Configuration;

public sealed class RabbitMqStreamConfigurationTests
{
    [Theory]
    [InlineData(129600, "36h")]
    [InlineData(5400, "90m")]
    [InlineData(90, "90s")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-STREAM", "fractional-large-units-project-exact-age")]
    public void MaxAge_UsesAnExactBrokerUnitWithoutRoundingTheRetention(int seconds, string expected)
    {
        var topology = new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology());
        var busConfiguration = new RabbitMqBusConfiguration(topology);
        var endpoint = Assert.IsType<RabbitMqReceiveEndpointConfiguration>(
            busConfiguration.HostConfiguration.CreateReceiveEndpointConfiguration("stream-exact-age", receive =>
                receive.Stream("native-reader", stream => stream.MaxAge = TimeSpan.FromSeconds(seconds))));

        var settings = Assert.IsType<RabbitMqReceiveSettings>(endpoint.Settings);
        var context = Assert.IsType<RabbitMqQueueReceiveEndpointContext>(endpoint.CreateReceiveEndpointContext());
        var queue = Assert.Single(context.BrokerTopology.Queues);

        Assert.Equal("stream", settings.QueueArguments[RabbitMQ.Client.Headers.XQueueType]);
        Assert.Equal(expected, settings.QueueArguments[RabbitMQ.Client.Headers.XMaxAge]);
        Assert.Equal(expected, queue.QueueArguments[RabbitMQ.Client.Headers.XMaxAge]);
        Assert.Equal("native-reader", settings.ConsumerTag);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-STREAM", "invalid-age-preserves-prior-retention")]
    public void InvalidMaxAge_DoesNotEraseOrRoundAnExistingRetentionLimit()
    {
        RabbitMqReceiveSettings? settings = null;

        _ = Bus.Factory.CreateUsingRabbitMq(configurator => configurator.ReceiveEndpoint("stream-invalid-age", endpoint =>
        {
            endpoint.Stream("native-reader", stream =>
            {
                stream.MaxAge = TimeSpan.FromHours(2);
                settings = Assert.IsType<RabbitMqReceiveSettings>(
                    Assert.IsType<RabbitMqReceiveEndpointConfiguration>(endpoint).Settings);

                ArgumentOutOfRangeException negative = Assert.Throws<ArgumentOutOfRangeException>(
                    () => stream.MaxAge = TimeSpan.FromSeconds(-1));
                Assert.Equal("value", negative.ParamName);
                Assert.Equal("2h", settings.QueueArguments[RabbitMQ.Client.Headers.XMaxAge]);

                ArgumentOutOfRangeException negativeTick = Assert.Throws<ArgumentOutOfRangeException>(
                    () => stream.MaxAge = TimeSpan.FromTicks(-1));
                Assert.Equal("value", negativeTick.ParamName);
                Assert.Equal("2h", settings.QueueArguments[RabbitMQ.Client.Headers.XMaxAge]);

                ArgumentOutOfRangeException fractional = Assert.Throws<ArgumentOutOfRangeException>(
                    () => stream.MaxAge = TimeSpan.FromSeconds(1) + TimeSpan.FromTicks(1));
                Assert.Equal("value", fractional.ParamName);
                Assert.Equal("2h", settings.QueueArguments[RabbitMQ.Client.Headers.XMaxAge]);

                stream.MaxAge = TimeSpan.Zero;
                Assert.False(settings.QueueArguments.ContainsKey(RabbitMQ.Client.Headers.XMaxAge));
                stream.MaxAge = TimeSpan.FromMinutes(3);
            });
        }));

        Assert.Equal("3m", Assert.IsType<RabbitMqReceiveSettings>(settings).QueueArguments[RabbitMQ.Client.Headers.XMaxAge]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-STREAM", "retention-size-and-offset-project-to-queue-and-consumer")]
    public void Stream_ProjectsRetentionSizesAndNumericOffsetToTheirBrokerArguments()
    {
        const long maxLength = 20_000_000_000;
        const long segmentSize = 100_000_000;
        var topology = new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology());
        var busConfiguration = new RabbitMqBusConfiguration(topology);
        var endpoint = Assert.IsType<RabbitMqReceiveEndpointConfiguration>(
            busConfiguration.HostConfiguration.CreateReceiveEndpointConfiguration("stream-size-offset", receive =>
                receive.Stream("native-reader", stream =>
                {
                    stream.MaxLength = maxLength;
                    stream.MaxSegmentSize = segmentSize;
                    stream.FromOffset(5000);
                    stream.Reference = "subscriber-one";
                    stream.Filter = "tenant-one";
                })));

        var settings = Assert.IsType<RabbitMqReceiveSettings>(endpoint.Settings);
        var context = Assert.IsType<RabbitMqQueueReceiveEndpointContext>(endpoint.CreateReceiveEndpointContext());
        var queue = Assert.Single(context.BrokerTopology.Queues);

        Assert.Equal("stream", queue.QueueArguments[RabbitMQ.Client.Headers.XQueueType]);
        Assert.Equal(maxLength, Assert.IsType<long>(queue.QueueArguments[RabbitMQ.Client.Headers.XMaxLengthInBytes]));
        Assert.Equal(segmentSize, Assert.IsType<long>(queue.QueueArguments[RabbitMQ.Client.Headers.XStreamMaxSegmentSizeInBytes]));
        Assert.Equal(5000L, settings.ConsumeArguments[RabbitMQ.Client.Headers.XStreamOffset]);
        Assert.Equal("subscriber-one", settings.ConsumeArguments["name"]);
        Assert.Equal("tenant-one", settings.ConsumeArguments["x-stream-filter"]);
        Assert.Equal("native-reader", settings.ConsumerTag);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-STREAM", "retention-size-boundaries-preserve-prior-settings")]
    public void RetentionSizeBoundaries_RejectInvalidValuesBeforeChangingValidLimits()
    {
        RabbitMqReceiveSettings? settings = null;

        _ = Bus.Factory.CreateUsingRabbitMq(configurator => configurator.ReceiveEndpoint("stream-size-boundary", endpoint =>
        {
            endpoint.Stream("native-reader", stream =>
            {
                stream.MaxLength = 20_000_000_000;
                stream.MaxSegmentSize = 100_000_000;
                settings = Assert.IsType<RabbitMqReceiveSettings>(
                    Assert.IsType<RabbitMqReceiveEndpointConfiguration>(endpoint).Settings);

                ArgumentOutOfRangeException negativeLength = Assert.Throws<ArgumentOutOfRangeException>(
                    () => stream.MaxLength = -1);
                Assert.Equal("value", negativeLength.ParamName);
                Assert.Equal(20_000_000_000L, settings.QueueArguments[RabbitMQ.Client.Headers.XMaxLengthInBytes]);
                Assert.Equal(100_000_000L, settings.QueueArguments[RabbitMQ.Client.Headers.XStreamMaxSegmentSizeInBytes]);

                stream.MaxLength = 0;
                Assert.Equal(0L, settings.QueueArguments[RabbitMQ.Client.Headers.XMaxLengthInBytes]);
                stream.MaxSegmentSize = 3_000_000_000;
                Assert.Equal(3_000_000_000L, settings.QueueArguments[RabbitMQ.Client.Headers.XStreamMaxSegmentSizeInBytes]);

                ArgumentOutOfRangeException oversizedSegment = Assert.Throws<ArgumentOutOfRangeException>(
                    () => stream.MaxSegmentSize = 3_000_000_001);
                Assert.Equal("value", oversizedSegment.ParamName);
                Assert.Equal(3_000_000_000L, settings.QueueArguments[RabbitMQ.Client.Headers.XStreamMaxSegmentSizeInBytes]);
                Assert.Equal(0L, settings.QueueArguments[RabbitMQ.Client.Headers.XMaxLengthInBytes]);
            });
        }));

        Assert.NotNull(settings);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-STREAM", "stream-timestamp-offset-uses-amqp-utc-seconds")]
    public void FromTimestamp_ProjectsUtcUnixSecondsRatherThanLocalClockTicks()
    {
        DateTimeOffset timestamp = new(2026, 9, 28, 14, 30, 5, TimeSpan.FromHours(2));
        RabbitMqReceiveSettings? settings = null;

        _ = Bus.Factory.CreateUsingRabbitMq(configurator => configurator.ReceiveEndpoint("stream-timestamp", endpoint =>
        {
            endpoint.Stream("native-reader", stream =>
            {
                stream.FromFirst();
                stream.FromTimestamp(timestamp);
            });
            settings = Assert.IsType<RabbitMqReceiveSettings>(
                Assert.IsType<RabbitMqReceiveEndpointConfiguration>(endpoint).Settings);
        }));

        var offset = Assert.IsType<AmqpTimestamp>(
            Assert.IsType<RabbitMqReceiveSettings>(settings).ConsumeArguments[RabbitMQ.Client.Headers.XStreamOffset]);
        Assert.Equal(timestamp.ToUnixTimeSeconds(), offset.UnixTime);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-STREAM", "invalid-stream-offset-preserves-existing-position")]
    public void InvalidStreamOffsets_LeaveThePriorConsumerPositionUntouched()
    {
        RabbitMqReceiveSettings? settings = null;

        _ = Bus.Factory.CreateUsingRabbitMq(configurator => configurator.ReceiveEndpoint("stream-invalid-offset", endpoint =>
        {
            endpoint.Stream("native-reader", stream =>
            {
                stream.FromLast();
                settings = Assert.IsType<RabbitMqReceiveSettings>(
                    Assert.IsType<RabbitMqReceiveEndpointConfiguration>(endpoint).Settings);

                ArgumentOutOfRangeException negativeOffset = Assert.Throws<ArgumentOutOfRangeException>(
                    () => stream.FromOffset(-1));
                Assert.Equal("offset", negativeOffset.ParamName);
                Assert.Equal("last", settings.ConsumeArguments[RabbitMQ.Client.Headers.XStreamOffset]);

                ArgumentOutOfRangeException preEpoch = Assert.Throws<ArgumentOutOfRangeException>(
                    () => stream.FromTimestamp(DateTimeOffset.UnixEpoch.AddTicks(-1)));
                Assert.Equal("timestamp", preEpoch.ParamName);
                Assert.Equal("last", settings.ConsumeArguments[RabbitMQ.Client.Headers.XStreamOffset]);

                stream.FromTimestamp(DateTimeOffset.UnixEpoch);
                Assert.Equal(0L, Assert.IsType<AmqpTimestamp>(
                    settings.ConsumeArguments[RabbitMQ.Client.Headers.XStreamOffset]).UnixTime);
                stream.FromOffset(0);
            });
        }));

        Assert.Equal(0L, Assert.IsType<RabbitMqReceiveSettings>(settings).ConsumeArguments[RabbitMQ.Client.Headers.XStreamOffset]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-STREAM", "subsecond-max-age-removes-invalid-argument")]
    public void MaxAgeBelowBrokerGranularity_RemovesAnExistingQueueArgument()
    {
        global::ViciOne.ServiceBus.RabbitMq.Configuration.RabbitMqReceiveSettings? settings = null;

        _ = Bus.Factory.CreateUsingRabbitMq(configurator => configurator.ReceiveEndpoint("stream-max-age", endpoint =>
        {
            endpoint.Stream("native-reader", stream =>
            {
                stream.MaxAge = TimeSpan.FromDays(14);
                stream.MaxAge = TimeSpan.FromMilliseconds(999);
            });
            settings = Assert.IsType<global::ViciOne.ServiceBus.RabbitMq.Configuration.RabbitMqReceiveSettings>(
                Assert.IsType<global::ViciOne.ServiceBus.RabbitMq.Configuration.RabbitMqReceiveEndpointConfiguration>(endpoint).Settings);
        }));

        global::ViciOne.ServiceBus.RabbitMq.Configuration.RabbitMqReceiveSettings actual =
            Assert.IsType<global::ViciOne.ServiceBus.RabbitMq.Configuration.RabbitMqReceiveSettings>(settings);
        Assert.False(actual.QueueArguments.ContainsKey("x-max-age"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-STREAM", "stream-settings-project-before-provider-start")]
    public void Stream_ProjectsTheExactQueueAndConsumerArgumentsBeforeTheBrokerStarts()
    {
        global::ViciOne.ServiceBus.RabbitMq.Configuration.RabbitMqReceiveSettings? settings = null;

        _ = Bus.Factory.CreateUsingRabbitMq(configurator => configurator.ReceiveEndpoint("stream-settings", endpoint =>
        {
            endpoint.Stream("native-reader", stream =>
            {
                stream.MaxAge = TimeSpan.FromDays(14);
                stream.FromFirst();
            });
            settings = Assert.IsType<global::ViciOne.ServiceBus.RabbitMq.Configuration.RabbitMqReceiveSettings>(
                Assert.IsType<global::ViciOne.ServiceBus.RabbitMq.Configuration.RabbitMqReceiveEndpointConfiguration>(endpoint).Settings);
        }));

        global::ViciOne.ServiceBus.RabbitMq.Configuration.RabbitMqReceiveSettings actual =
            Assert.IsType<global::ViciOne.ServiceBus.RabbitMq.Configuration.RabbitMqReceiveSettings>(settings);
        Assert.Equal("stream", actual.QueueArguments["x-queue-type"]);
        Assert.Equal("14D", actual.QueueArguments["x-max-age"]);
        Assert.Equal("first", actual.ConsumeArguments["x-stream-offset"]);
        Assert.Equal("native-reader", actual.ConsumerTag);
    }
}
