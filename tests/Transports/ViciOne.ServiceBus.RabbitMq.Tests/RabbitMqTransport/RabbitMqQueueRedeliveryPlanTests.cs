using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.RabbitMq.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport;

public sealed class RabbitMqQueueRedeliveryPlanTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-QUEUE-REDELIVERY", "finite-normalized-interval-set")]
    public void Plan_NormalizesIntervalsAndRoutesOnlyDeclaredDelays()
    {
        RabbitMqReceiveSettings settings = CreateSettings("orders");
        var plan = new RabbitMqQueueRedeliveryPlan(
            settings,
            [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5)]);

        Assert.Equal("orders", plan.QueueName);
        Assert.Equal("orders.redelivery", plan.DelayExchangeName);
        Assert.Equal("orders.redelivery.return", plan.ReturnExchangeName);
        Assert.Equal([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5)], plan.Intervals);
        Assert.Equal("1000", plan.GetRoutingKey(TimeSpan.FromSeconds(1)));
        Assert.Equal("5000", plan.GetRoutingKey(TimeSpan.FromSeconds(5)));
        ConfigurationException exception = Assert.Throws<ConfigurationException>(
            () => plan.GetRoutingKey(TimeSpan.FromSeconds(2)));
        Assert.Contains("was not declared", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-QUEUE-REDELIVERY", "interval-validation-boundaries")]
    public void Plan_RejectsEmptyNonpositiveFractionalAndOverLimitIntervals()
    {
        RabbitMqReceiveSettings settings = CreateSettings("orders");

        ConfigurationException empty = Assert.Throws<ConfigurationException>(
            () => new RabbitMqQueueRedeliveryPlan(settings, []));
        ArgumentOutOfRangeException zero = Assert.Throws<ArgumentOutOfRangeException>(
            () => new RabbitMqQueueRedeliveryPlan(settings, [TimeSpan.Zero]));
        ArgumentOutOfRangeException negative = Assert.Throws<ArgumentOutOfRangeException>(
            () => new RabbitMqQueueRedeliveryPlan(settings, [TimeSpan.FromMilliseconds(-1)]));
        ArgumentOutOfRangeException fractional = Assert.Throws<ArgumentOutOfRangeException>(
            () => new RabbitMqQueueRedeliveryPlan(settings, [TimeSpan.FromTicks(TimeSpan.TicksPerMillisecond + 1)]));
        ArgumentOutOfRangeException overLimit = Assert.Throws<ArgumentOutOfRangeException>(
            () => new RabbitMqQueueRedeliveryPlan(
                settings,
                [TimeSpan.FromTicks(((long)int.MaxValue + 1) * TimeSpan.TicksPerMillisecond)]));

        Assert.Contains("at least one", empty.Message, StringComparison.Ordinal);
        Assert.Equal(TimeSpan.Zero, zero.ActualValue);
        Assert.Equal(TimeSpan.FromMilliseconds(-1), negative.ActualValue);
        Assert.Contains("whole number of milliseconds", fractional.Message, StringComparison.Ordinal);
        Assert.Contains(int.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture), overLimit.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-QUEUE-REDELIVERY", "endpoint-shape-validation")]
    public void Plan_RejectsUnnamedExchangeOnlyAndStreamEndpoints()
    {
        RabbitMqReceiveSettings unnamed = CreateSettings(" ");
        RabbitMqReceiveSettings exchangeOnly = CreateSettings("orders");
        exchangeOnly.BindQueue = false;
        RabbitMqReceiveSettings stream = CreateSettings("orders-stream");
        stream.QueueArguments[RabbitMQ.Client.Headers.XQueueType] = "stream";

        ConfigurationException unnamedFailure = Assert.Throws<ConfigurationException>(
            () => new RabbitMqQueueRedeliveryPlan(unnamed, [TimeSpan.FromSeconds(1)]));
        ConfigurationException exchangeOnlyFailure = Assert.Throws<ConfigurationException>(
            () => new RabbitMqQueueRedeliveryPlan(exchangeOnly, [TimeSpan.FromSeconds(1)]));
        ConfigurationException streamFailure = Assert.Throws<ConfigurationException>(
            () => new RabbitMqQueueRedeliveryPlan(stream, [TimeSpan.FromSeconds(1)]));

        Assert.Contains("named receive queue", unnamedFailure.Message, StringComparison.Ordinal);
        Assert.Contains("exchange-only", exchangeOnlyFailure.Message, StringComparison.Ordinal);
        Assert.Contains("stream queues", streamFailure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-QUEUE-REDELIVERY", "generated-topology-name-validation")]
    public void Plan_ValidatesEveryGeneratedTopologyNameBeforeProviderWork()
    {
        RabbitMqReceiveSettings settings = CreateSettings(new string('q', 240));

        RabbitMqAddressException exception = Assert.Throws<RabbitMqAddressException>(
            () => new RabbitMqQueueRedeliveryPlan(settings, [TimeSpan.FromSeconds(1)]));

        Assert.Contains("255 bytes", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-QUEUE-REDELIVERY", "single-endpoint-configuration-owner")]
    public void Extension_AllowsExactlyOneQueueRedeliveryOwnerPerEndpoint()
    {
        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            Bus.Factory.CreateUsingRabbitMq(configurator => configurator.ReceiveEndpoint("orders", endpoint =>
            {
                endpoint.UseQueueRedelivery(TimeSpan.FromSeconds(1));
                endpoint.UseQueueRedelivery(TimeSpan.FromSeconds(5));
            })));

        Assert.Contains("only be configured once", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-QUEUE-REDELIVERY", "canonical-technical-policy-owner")]
    public void TechnicalExtension_RejectsNullAndClaimsTheCanonicalQueueRedeliveryPlan()
    {
        ArgumentNullException nullOwner = Assert.Throws<ArgumentNullException>(
            () => RabbitMqQueueRedeliveryExtensions.UseTechnicalQueueRedelivery(null!));
        Assert.Equal("configurator", nullOwner.ParamName);

        ConfigurationException duplicateOwner = Assert.Throws<ConfigurationException>(() =>
            Bus.Factory.CreateUsingRabbitMq(configurator => configurator.ReceiveEndpoint("orders", endpoint =>
            {
                endpoint.UseTechnicalQueueRedelivery();
                endpoint.UseQueueRedelivery(TimeSpan.FromMinutes(10));
            })));

        Assert.Contains("only be configured once", duplicateOwner.Message, StringComparison.Ordinal);
    }

    private static RabbitMqReceiveSettings CreateSettings(string queueName)
    {
        var topology = new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology());
        var endpoint = new RabbitMqEndpointConfiguration(topology);
        return new RabbitMqReceiveSettings(endpoint, queueName, ExchangeType.Fanout, durable: true, autoDelete: false);
    }
}
