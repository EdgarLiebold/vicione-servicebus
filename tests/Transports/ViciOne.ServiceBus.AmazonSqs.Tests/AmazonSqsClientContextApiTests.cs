using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsClientContextApiTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-API", "delete-message-uses-logical-queue-name")]
    public void DeleteMessage_ConsistentlyNamesItsLogicalQueueParameter()
    {
        Type[] contracts =
        [
            typeof(ClientContext),
            typeof(AmazonSqsClientContext),
            typeof(SharedClientContext),
            typeof(ScopeClientContext),
        ];

        Assert.All(contracts, type =>
        {
            MethodInfo method = type.GetMethod(nameof(ClientContext.DeleteMessageAsync))!;
            Assert.Equal("queueName", method.GetParameters()[0].Name);
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-API", "one-provider-connection-exception")]
    public void PublicApi_ExposesOneUnambiguousConnectionException()
    {
        Assembly product = typeof(AmazonSqsConnectionException).Assembly;

        Assert.NotNull(product.GetType(typeof(AmazonSqsConnectionException).FullName!));
        Assert.Null(product.GetType("ViciOne.ServiceBus.AmazonSqs.AmazonSqsConnectException"));
    }
}
