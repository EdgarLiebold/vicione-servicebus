using System.Reflection;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport;

public sealed class RabbitMqReceiveContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-RECEIVE-CONTEXT", "complete-transport-property-snapshot")]
    public void GetTransportProperties_CapturesEveryReplayableValueIncludingZeroPriority()
    {
        IReadOnlyBasicProperties properties = CreateProperties(new Dictionary<string, object?>
        {
            [nameof(IReadOnlyBasicProperties.AppId)] = "orders-app",
            [nameof(IReadOnlyBasicProperties.Priority)] = (byte)0,
            [nameof(IReadOnlyBasicProperties.ReplyTo)] = "reply-queue",
            [nameof(IReadOnlyBasicProperties.Type)] = "order-created",
            [nameof(IReadOnlyBasicProperties.UserId)] = "service-user",
        });
        using var context = CreateContext("orders.created", properties);

        IDictionary<string, object>? snapshot = context.GetTransportProperties();

        Assert.NotNull(snapshot);
        Assert.Equal(6, snapshot.Count);
        Assert.Equal("orders.created", snapshot[RabbitMqTransportPropertyNames.RoutingKey]);
        Assert.Equal("orders-app", snapshot[RabbitMqTransportPropertyNames.AppId]);
        Assert.Equal<byte>(0, Assert.IsType<byte>(snapshot[RabbitMqTransportPropertyNames.Priority]));
        Assert.Equal("reply-queue", snapshot[RabbitMqTransportPropertyNames.ReplyTo]);
        Assert.Equal("order-created", snapshot[RabbitMqTransportPropertyNames.Type]);
        Assert.Equal("service-user", snapshot[RabbitMqTransportPropertyNames.UserId]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-RECEIVE-CONTEXT", "empty-values-produce-no-snapshot")]
    public void GetTransportProperties_ReturnsNullWhenRoutingAndStringPropertiesAreBlank()
    {
        var blankProperties = new BasicProperties
        {
            AppId = "",
            ReplyTo = " ",
            Type = "\t",
            UserId = "",
        };
        using var blank = CreateContext(" ", blankProperties);
        using var absent = CreateContext("", new BasicProperties());

        Assert.Null(blank.GetTransportProperties());
        Assert.Null(absent.GetTransportProperties());
    }

    private static RabbitMqReceiveContext CreateContext(string routingKey, IReadOnlyBasicProperties properties)
    {
        RabbitMqReceiveEndpointContext endpoint = DispatchProxy.Create<RabbitMqReceiveEndpointContext, ReceiveEndpointContextProxy>();
        ((ReceiveEndpointContextProxy)(object)endpoint).InputAddress = new Uri("rabbitmq://localhost/orders");

        return new RabbitMqReceiveContext(
            "exchange",
            routingKey,
            "consumer",
            17,
            new byte[] { 1, 2, 3 },
            redelivered: false,
            properties,
            endpoint);
    }

    private static IReadOnlyBasicProperties CreateProperties(Dictionary<string, object?> values)
    {
        IReadOnlyBasicProperties properties = DispatchProxy.Create<IReadOnlyBasicProperties, BasicPropertiesProxy>();
        ((BasicPropertiesProxy)(object)properties).Values = values;
        return properties;
    }

    private class BasicPropertiesProxy : DispatchProxy
    {
        public Dictionary<string, object?> Values { get; set; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name.StartsWith("get_", StringComparison.Ordinal) == true)
            {
                string propertyName = targetMethod.Name[4..];
                return Values.TryGetValue(propertyName, out object? value)
                    ? value
                    : Default(targetMethod.ReturnType);
            }

            if (targetMethod?.Name.StartsWith("Is", StringComparison.Ordinal) == true
                && targetMethod.Name.EndsWith("Present", StringComparison.Ordinal))
            {
                string propertyName = targetMethod.Name[2..^7];
                return Values.ContainsKey(propertyName);
            }

            return targetMethod == null ? null : Default(targetMethod.ReturnType);
        }
    }

    private class ReceiveEndpointContextProxy : DispatchProxy
    {
        public Uri InputAddress { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_InputAddress")
                return InputAddress;
            if (targetMethod?.Name == nameof(PipeContext.TryGetPayload) && targetMethod.IsGenericMethod)
            {
                args![0] = null;
                return false;
            }
            if (targetMethod?.ReturnType == typeof(CancellationToken))
                return CancellationToken.None;
            if (targetMethod?.ReturnType == typeof(void))
                return null;
            return targetMethod?.ReturnType.IsValueType == true
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
        }
    }

    private static object? Default(Type type) =>
        type.IsValueType ? Activator.CreateInstance(type) : null;
}
