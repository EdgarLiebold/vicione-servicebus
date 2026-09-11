using ViciOne.ServiceBus.Consumers.Metadata;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers.Metadata;

public sealed class ConsumerRegistrationMetadataTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-REGISTRATION-METADATA", "null-runtime-type")]
    public void ConsumerClassification_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ConsumerRegistrationMetadata.IsConsumerOrDefinition(null!));
        Assert.Throws<ArgumentNullException>(() => ConsumerRegistrationMetadata.IsConsumer(null!));
        Assert.Throws<ArgumentNullException>(() => ConsumerRegistrationMetadata.IsConsumerRegistrationExcluded(null!));
    }
}
