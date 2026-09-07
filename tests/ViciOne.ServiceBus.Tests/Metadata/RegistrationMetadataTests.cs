using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Metadata;

public sealed class RegistrationMetadataTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-REGISTRATION-METADATA", "null-runtime-type")]
    public void ConsumerClassification_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => RegistrationMetadata.IsConsumerOrDefinition(null!));
        Assert.Throws<ArgumentNullException>(() => RegistrationMetadata.IsConsumer(null!));
        Assert.Throws<ArgumentNullException>(() => RegistrationMetadata.IsConsumerRegistrationExcluded(null!));
    }
}
