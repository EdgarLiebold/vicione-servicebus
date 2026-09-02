using ViciOne.ServiceBus.AmazonSqsTransport.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqsTransport.Tests;

public sealed class AmazonSqsEntityNameTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-ENTITY-NAME", "queue-service-limit")]
    public void QueueNameBeyondTheServiceLimit_IsRejected()
    {
        Assert.True(AmazonSqsEntityNameValidator.Validator.IsValidEntityName(new string('a', 80)));
        Assert.False(AmazonSqsEntityNameValidator.Validator.IsValidEntityName(new string('a', 81)));
        Assert.Throws<AmazonSqsTransportConfigurationException>(
            () => new AmazonSqsEndpointAddress(new Uri("amazonsqs://eu-central-1"), new string('a', 81)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-ENTITY-NAME", "queue-and-topic-aws-grammar-fifo-suffix")]
    public void QueueAndTopicNames_EnforceAwsGrammarAndFifoSuffix()
    {
        string[] validNames = ["orders", "orders-2026", "orders_eu", "orders.fifo"];
        string[] invalidNames = ["", " ", ".fifo", "orders.FIFO", "orders.topic", "orders:topic", "orders/path", "orders#topic"];

        Assert.All(validNames, name => Assert.True(AmazonSqsEntityNameValidator.Validator.IsValidEntityName(name), name));
        Assert.All(validNames, name => Assert.True(AmazonSnsTopicNameValidator.Validator.IsValidEntityName(name), name));
        Assert.All(invalidNames, name => Assert.False(AmazonSqsEntityNameValidator.Validator.IsValidEntityName(name), name));
        Assert.All(invalidNames, name => Assert.False(AmazonSnsTopicNameValidator.Validator.IsValidEntityName(name), name));

        Assert.True(AmazonSnsTopicNameValidator.Validator.IsValidEntityName(new string('a', 256)));
        Assert.False(AmazonSnsTopicNameValidator.Validator.IsValidEntityName(new string('a', 257)));
        Assert.True(AmazonSqsEntityNameValidator.Validator.IsValidEntityName(new string('a', 75) + ".fifo"));
        Assert.False(AmazonSqsEntityNameValidator.Validator.IsValidEntityName(new string('a', 76) + ".fifo"));
    }
}
