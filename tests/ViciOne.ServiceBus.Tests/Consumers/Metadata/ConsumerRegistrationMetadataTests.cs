using ViciOne.ServiceBus.Advanced.Registration;
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

    [Theory]
    [MemberData(nameof(ClassificationCases))]
    [RequirementCoverage("REQ-VSB-REGISTRATION-METADATA", "core-definition-exclusion-and-unrelated-type-matrix")]
    public void ConsumerClassification_SelectsOnlyCoreOwnedConsumerTypes(
        Type type,
        bool isConsumerOrDefinition,
        bool isConsumer,
        bool isExcluded)
    {
        Assert.Equal(isConsumerOrDefinition, ConsumerRegistrationMetadata.IsConsumerOrDefinition(type));
        Assert.Equal(isConsumer, ConsumerRegistrationMetadata.IsConsumer(type));
        Assert.Equal(isExcluded, ConsumerRegistrationMetadata.IsConsumerRegistrationExcluded(type));
    }

    public static TheoryData<Type, bool, bool, bool> ClassificationCases => new()
    {
        { typeof(CoreConsumer), true, true, false },
        { typeof(CoreConsumerDefinition), true, false, false },
        { typeof(ExcludedConsumer), false, false, true },
        { typeof(ExcludedConsumerDefinition), false, false, true },
        { typeof(UnrelatedType), false, false, false },
    };

    public sealed record RegistrationMessage;

    public class CoreConsumer : IConsumer<RegistrationMessage>
    {
        public Task ConsumeAsync(ConsumeContext<RegistrationMessage> context) => Task.CompletedTask;
    }

    public sealed class CoreConsumerDefinition : ConsumerDefinition<CoreConsumer>;

    [ConsumerRegistrationExclusion]
    public interface ICapabilityOwnedConsumerContract;

    public sealed class ExcludedConsumer : CoreConsumer, ICapabilityOwnedConsumerContract;

    public sealed class ExcludedConsumerDefinition :
        ConsumerDefinition<CoreConsumer>,
        ICapabilityOwnedConsumerContract;

    public sealed class UnrelatedType;
}
