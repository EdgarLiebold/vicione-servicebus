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

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-QUEUE-CONFIGURATION", "duration-arguments-preserve-wide-broker-integers")]
    public void DurationArguments_PreserveIntAndLongValuesAcrossQueueAndExchange()
    {
        const long extendedMilliseconds = (long)int.MaxValue + 1;
        RabbitMqReceiveSettings? settings = null;
        RabbitMqQueueReceiveEndpointContext? context = null;
        var topology = new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology());
        var busConfiguration = new RabbitMqBusConfiguration(topology);

        _ = busConfiguration.HostConfiguration.CreateReceiveEndpointConfiguration("duration-arguments", endpoint =>
        {
            endpoint.SetQueueArgument(RabbitMQ.Client.Headers.XExpires, TimeSpan.FromMilliseconds(int.MaxValue));
            endpoint.SetExchangeArgument("x-plugin-window-small", TimeSpan.FromMilliseconds(int.MaxValue));
            settings = Assert.IsType<RabbitMqReceiveSettings>(
                Assert.IsType<RabbitMqReceiveEndpointConfiguration>(endpoint).Settings);
            Assert.Equal(int.MaxValue, Assert.IsType<int>(settings.QueueArguments[RabbitMQ.Client.Headers.XExpires]));
            Assert.Equal(int.MaxValue, Assert.IsType<int>(settings.ExchangeArguments["x-plugin-window-small"]));
            Assert.Equal(TimeSpan.FromMilliseconds(int.MaxValue), settings.QueueExpiration);

            TimeSpan extended = TimeSpan.FromTicks(extendedMilliseconds * TimeSpan.TicksPerMillisecond);
            endpoint.SetQueueArgument("x-message-ttl", extended);
            endpoint.SetExchangeArgument("x-plugin-window-large", extended);
            Assert.Equal(extendedMilliseconds, Assert.IsType<long>(settings.QueueArguments["x-message-ttl"]));
            Assert.Equal(extendedMilliseconds, Assert.IsType<long>(settings.ExchangeArguments["x-plugin-window-large"]));

            context = Assert.IsType<RabbitMqQueueReceiveEndpointContext>(
                Assert.IsType<RabbitMqReceiveEndpointConfiguration>(endpoint).CreateReceiveEndpointContext());
        });

        Assert.NotNull(settings);
        RabbitMqQueueReceiveEndpointContext actual = Assert.IsType<RabbitMqQueueReceiveEndpointContext>(context);
        var queue = Assert.Single(actual.BrokerTopology.Queues);
        var exchange = Assert.Single(actual.BrokerTopology.Exchanges);
        Assert.Equal(int.MaxValue, Assert.IsType<int>(queue.QueueArguments[RabbitMQ.Client.Headers.XExpires]));
        Assert.Equal(extendedMilliseconds, Assert.IsType<long>(queue.QueueArguments["x-message-ttl"]));
        Assert.Equal(int.MaxValue, Assert.IsType<int>(exchange.ExchangeArguments["x-plugin-window-small"]));
        Assert.Equal(extendedMilliseconds, Assert.IsType<long>(exchange.ExchangeArguments["x-plugin-window-large"]));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-QUEUE-CONFIGURATION", "fractional-duration-arguments-preserve-existing-values")]
    public void FractionalDurationArguments_RejectBeforeChangingQueueOrExchange()
    {
        RabbitMqReceiveSettings? settings = null;

        _ = Bus.Factory.CreateUsingRabbitMq(configurator => configurator.ReceiveEndpoint("fractional-arguments", endpoint =>
        {
            endpoint.SetQueueArgument("x-message-ttl", TimeSpan.FromMinutes(2));
            endpoint.SetExchangeArgument("x-plugin-window", TimeSpan.FromMinutes(3));
            settings = Assert.IsType<RabbitMqReceiveSettings>(
                Assert.IsType<RabbitMqReceiveEndpointConfiguration>(endpoint).Settings);

            ArgumentOutOfRangeException queueFailure = Assert.Throws<ArgumentOutOfRangeException>(() =>
                endpoint.SetQueueArgument("x-message-ttl", TimeSpan.FromMinutes(1) + TimeSpan.FromTicks(1)));
            Assert.Equal("value", queueFailure.ParamName);
            Assert.Equal(120000, settings.QueueArguments["x-message-ttl"]);
            Assert.Equal(180000, settings.ExchangeArguments["x-plugin-window"]);

            ArgumentOutOfRangeException negativeTtl = Assert.Throws<ArgumentOutOfRangeException>(() =>
                endpoint.SetQueueArgument("x-message-ttl", TimeSpan.FromMilliseconds(-1)));
            ArgumentOutOfRangeException negativeExchange = Assert.Throws<ArgumentOutOfRangeException>(() =>
                endpoint.SetExchangeArgument("x-plugin-window", TimeSpan.FromMilliseconds(-1)));
            Assert.Equal("value", negativeTtl.ParamName);
            Assert.Equal("value", negativeExchange.ParamName);
            Assert.Equal(120000, settings.QueueArguments["x-message-ttl"]);
            Assert.Equal(180000, settings.ExchangeArguments["x-plugin-window"]);

            endpoint.SetQueueArgument(RabbitMQ.Client.Headers.XExpires, TimeSpan.FromMinutes(4));
            ArgumentOutOfRangeException zeroExpiration = Assert.Throws<ArgumentOutOfRangeException>(() =>
                endpoint.SetQueueArgument(RabbitMQ.Client.Headers.XExpires, TimeSpan.Zero));
            Assert.Equal("value", zeroExpiration.ParamName);
            Assert.Equal(TimeSpan.FromMinutes(4), settings.QueueExpiration);
            Assert.Equal(240000, settings.QueueArguments[RabbitMQ.Client.Headers.XExpires]);
            ArgumentOutOfRangeException negativeExpiration = Assert.Throws<ArgumentOutOfRangeException>(() =>
                endpoint.SetQueueArgument(RabbitMQ.Client.Headers.XExpires, TimeSpan.FromMilliseconds(-1)));
            Assert.Equal("value", negativeExpiration.ParamName);
            Assert.Equal(TimeSpan.FromMinutes(4), settings.QueueExpiration);
            Assert.Equal(240000, settings.QueueArguments[RabbitMQ.Client.Headers.XExpires]);

            ArgumentNullException queueKey = Assert.Throws<ArgumentNullException>(() =>
                endpoint.SetQueueArgument(null!, TimeSpan.FromTicks(1)));
            ArgumentNullException exchangeKey = Assert.Throws<ArgumentNullException>(() =>
                endpoint.SetExchangeArgument(null!, TimeSpan.FromTicks(1)));
            Assert.Equal("key", queueKey.ParamName);
            Assert.Equal("key", exchangeKey.ParamName);
            Assert.Equal(120000, settings.QueueArguments["x-message-ttl"]);
            Assert.Equal(180000, settings.ExchangeArguments["x-plugin-window"]);

            ArgumentOutOfRangeException exchangeFailure = Assert.Throws<ArgumentOutOfRangeException>(() =>
                endpoint.SetExchangeArgument("x-plugin-window", TimeSpan.FromTicks(1)));
            Assert.Equal("value", exchangeFailure.ParamName);
            Assert.Equal(120000, settings.QueueArguments["x-message-ttl"]);
            Assert.Equal(180000, settings.ExchangeArguments["x-plugin-window"]);

            endpoint.SetQueueArgument("x-message-ttl", TimeSpan.Zero);
            Assert.Equal(0, settings.QueueArguments["x-message-ttl"]);
        }));

        Assert.NotNull(settings);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-QUEUE-CONFIGURATION", "expiration-validation-getter-and-topology-agree")]
    public void QueueExpiration_RejectsFractionalValueAndProjectsExactTopology()
    {
        const long milliseconds = 500_000_000_000_007;
        RabbitMqReceiveSettings? settings = null;
        var topology = new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology());
        var busConfiguration = new RabbitMqBusConfiguration(topology);

        _ = busConfiguration.HostConfiguration.CreateReceiveEndpointConfiguration("expiration-boundary", endpoint =>
        {
            endpoint.QueueExpiration = TimeSpan.FromMinutes(3);
            RabbitMqReceiveEndpointConfiguration concrete = Assert.IsType<RabbitMqReceiveEndpointConfiguration>(endpoint);
            settings = Assert.IsType<RabbitMqReceiveSettings>(concrete.Settings);

            ArgumentOutOfRangeException failure = Assert.Throws<ArgumentOutOfRangeException>(() =>
                endpoint.QueueExpiration = TimeSpan.FromTicks(1));
            Assert.Equal("value", failure.ParamName);
            Assert.Equal(TimeSpan.FromMinutes(3), settings.QueueExpiration);
            Assert.Equal(180000L, settings.QueueArguments[RabbitMQ.Client.Headers.XExpires]);

            TimeSpan large = TimeSpan.FromTicks(milliseconds * TimeSpan.TicksPerMillisecond);
            endpoint.QueueExpiration = large;
            Assert.Equal(large, settings.QueueExpiration);
            Assert.Equal(milliseconds, settings.QueueArguments[RabbitMQ.Client.Headers.XExpires]);
            RabbitMqQueueReceiveEndpointContext context = Assert.IsType<RabbitMqQueueReceiveEndpointContext>(
                concrete.CreateReceiveEndpointContext());
            Assert.Equal(milliseconds, Assert.Single(context.BrokerTopology.Queues).QueueArguments[RabbitMQ.Client.Headers.XExpires]);

            endpoint.SetQueueArgument(RabbitMQ.Client.Headers.XExpires, TimeSpan.FromSeconds(2));
            Assert.Equal(TimeSpan.FromSeconds(2), settings.QueueExpiration);
            Assert.Equal(2000, settings.QueueArguments[RabbitMQ.Client.Headers.XExpires]);

            endpoint.QueueExpiration = null;
            Assert.Null(settings.QueueExpiration);
            Assert.False(settings.QueueArguments.ContainsKey(RabbitMQ.Client.Headers.XExpires));

            endpoint.QueueExpiration = TimeSpan.FromSeconds(2);
            endpoint.QueueExpiration = TimeSpan.Zero;
            Assert.Null(settings.QueueExpiration);
            Assert.False(settings.QueueArguments.ContainsKey(RabbitMQ.Client.Headers.XExpires));

            endpoint.QueueExpiration = TimeSpan.FromSeconds(2);
            endpoint.QueueExpiration = TimeSpan.FromMilliseconds(-1);
            Assert.Null(settings.QueueExpiration);
            Assert.False(settings.QueueArguments.ContainsKey(RabbitMQ.Client.Headers.XExpires));
        });

        Assert.NotNull(settings);
    }
}
