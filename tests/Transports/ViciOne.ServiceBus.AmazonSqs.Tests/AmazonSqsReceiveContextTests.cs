using Amazon.SQS;
using Amazon.SQS.Model;
using System.Reflection;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsReceiveContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-RECEIVE", "nullable-transport-property-contract")]
    public void TransportProperties_ReturnAnnotationMatchesTheNullableInterfaceContract()
    {
        MethodInfo method = typeof(AmazonSqsReceiveContext).GetMethod(nameof(AmazonSqsReceiveContext.GetTransportProperties))!;

        NullabilityInfo nullability = new NullabilityInfoContext().Create(method.ReturnParameter);

        Assert.Equal(NullabilityState.Nullable, nullability.ReadState);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-RECEIVE", "missing-fifo-properties-return-null")]
    public void TransportProperties_WithoutUsableFifoAttributes_ReturnsNull()
    {
        var message = new Message
        {
            Body = "{}",
            Attributes = new Dictionary<string, string>
            {
                [MessageSystemAttributeName.MessageGroupId] = " ",
                [MessageSystemAttributeName.MessageDeduplicationId] = string.Empty,
            },
        };
        using AmazonSqsReceiveContext context = CreateContext(message);

        IDictionary<string, object>? actual = context.GetTransportProperties();

        Assert.Null(actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-RECEIVE", "fifo-properties-use-canonical-keys")]
    public void TransportProperties_WithFifoAttributes_ReturnsExactCanonicalValues()
    {
        var message = new Message
        {
            Body = "{}",
            Attributes = new Dictionary<string, string>
            {
                [MessageSystemAttributeName.MessageGroupId] = "tenant-47",
                [MessageSystemAttributeName.MessageDeduplicationId] = "order-991",
            },
        };
        using AmazonSqsReceiveContext context = CreateContext(message);

        IDictionary<string, object> actual = Assert.IsAssignableFrom<IDictionary<string, object>>(
            context.GetTransportProperties());

        Assert.Equal(2, actual.Count);
        Assert.Equal("tenant-47", actual[AmazonSqsTransportPropertyNames.GroupId]);
        Assert.Equal("order-991", actual[AmazonSqsTransportPropertyNames.DeduplicationId]);
    }

    private static AmazonSqsReceiveContext CreateContext(Message message)
    {
        SqsReceiveEndpointContext endpointContext = InterfaceProxy<SqsReceiveEndpointContext>.Create((method, _) => method.Name switch
        {
            "get_InputAddress" => new Uri("amazonsqs://eu-central-1/orders"),
            _ => Default(method.ReturnType),
        });
        ClientContext clientContext = InterfaceProxy<ClientContext>.Create((method, _) => Default(method.ReturnType));
        ReceiveSettings settings = InterfaceProxy<ReceiveSettings>.Create((method, _) => Default(method.ReturnType));
        ConnectionContext connectionContext = InterfaceProxy<ConnectionContext>.Create((method, _) => Default(method.ReturnType));

        return new AmazonSqsReceiveContext(message, false, endpointContext, clientContext, settings, connectionContext);
    }

    private static object? Default(Type returnType)
    {
        return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
    }
}
