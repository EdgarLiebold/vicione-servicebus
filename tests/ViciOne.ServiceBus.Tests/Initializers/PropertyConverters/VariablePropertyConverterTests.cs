using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Variables;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyConverters;

public sealed class VariablePropertyConverterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-VARIABLES", "shared-id-and-captured-timestamp")]
    public async Task InitializerVariables_ShareTheContextIdAndPreserveTheCapturedTimestampAsync()
    {
        IdVariable correlationId = InVar.CorrelationId;
        IdVariable id = InVar.Id;
        IdVariable stringId = InVar.Id;
        TimestampVariable timestamp = InVar.Timestamp;
        DateTimeOffset expectedTimestamp = timestamp;

        InitializeContext<VariableIntermediate> intermediate = await MessageInitializerCache<VariableIntermediate>.InitializeAsync(
            new
            {
                CorrelationId = correlationId,
                Id = id,
                StringId = stringId,
                Timestamp = timestamp,
            },
            TestContext.Current.CancellationToken);
        InitializeContext<VariableMessage> result = await MessageInitializerCache<VariableMessage>.InitializeAsync(
            intermediate.Message,
            TestContext.Current.CancellationToken);

        Assert.NotEqual(Guid.Empty, result.Message.Id);
        Assert.Equal(result.Message.Id, result.Message.CorrelationId);
        Assert.Equal(result.Message.Id, result.Message.StringId);
        Assert.Equal(expectedTimestamp, result.Message.Timestamp);
    }

    public interface VariableIntermediate
    {
        Guid CorrelationId { get; }

        Guid Id { get; }

        string StringId { get; }

        DateTimeOffset Timestamp { get; }
    }

    public interface VariableMessage
    {
        Guid CorrelationId { get; }

        Guid Id { get; }

        Guid StringId { get; }

        DateTimeOffset? Timestamp { get; }
    }
}
