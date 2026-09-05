using System.Reflection;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class NonContainerOptionsValidationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SERVICE-INSTANCE-OPTIONS", "null-formatter-rejected-at-configuration")]
    public void ServiceInstance_RejectsMissingFormatterAtConfigurationBoundary()
    {
        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            new ServiceInstanceOptions().SetEndpointNameFormatter(null!));

        Assert.Contains("EndpointNameFormatter", exception.Message, StringComparison.Ordinal);
        Assert.Contains("for bus 'default':", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SERVICE-INSTANCE-OPTIONS", "default-is-coherent")]
    public void ServiceInstance_DefaultIsCoherent()
    {
        var options = new ServiceInstanceOptions();

        Assert.NotNull(options.EndpointNameFormatter);
    }

    [Theory]
    [InlineData(InvalidOutboxOption.ConsumerId, "ConsumerId")]
    [InlineData(InvalidOutboxOption.ConsumerType, "ConsumerType")]
    [InlineData(InvalidOutboxOption.DeliveryLimit, "MessageDeliveryLimit")]
    [InlineData(InvalidOutboxOption.DeliveryTimeout, "MessageDeliveryTimeout")]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-OPTIONS", "all-generated-invariants-validated-before-pipe-build")]
    public void OutboxConsume_RejectsEveryInvalidInvariant(InvalidOutboxOption invalid, string property)
    {
        OutboxConsumeOptions options = ValidOutbox(invalid);
        MethodInfo validate = typeof(OutboxConsumeOptions).GetMethod("Validate", BindingFlags.Instance | BindingFlags.NonPublic)!;

        TargetInvocationException wrapper = Assert.Throws<TargetInvocationException>(() => validate.Invoke(options, null));
        ConfigurationException exception = Assert.IsType<ConfigurationException>(wrapper.InnerException);

        Assert.Contains(property, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-OPTIONS", "generated-default-is-coherent")]
    public void OutboxConsume_AcceptsCoherentGeneratedValues()
    {
        OutboxConsumeOptions options = ValidOutbox(null);
        MethodInfo validate = typeof(OutboxConsumeOptions).GetMethod("Validate", BindingFlags.Instance | BindingFlags.NonPublic)!;

        validate.Invoke(options, null);
    }

    [Theory]
    [InlineData(InvalidTableOption.Columns)]
    [InlineData(InvalidTableOption.ColumnName)]
    [InlineData(InvalidTableOption.Output)]
    [InlineData(InvalidTableOption.Alignment)]
    [RequirementCoverage("REQ-VSB-TEXT-TABLE-OPTIONS", "all-static-invariants-validated-at-construction")]
    public void TextTable_RejectsEveryInvalidInvariant(InvalidTableOption invalid)
    {
        var options = new TextTableOptions
        {
            Columns = invalid switch
            {
                InvalidTableOption.Columns => null!,
                InvalidTableOption.ColumnName => ["valid", " "],
                _ => ["valid"],
            },
            Out = invalid == InvalidTableOption.Output ? null! : TextWriter.Null,
            NumberAlignment = invalid == InvalidTableOption.Alignment ? (NumberAlignment)42 : NumberAlignment.Left,
        };

        Assert.ThrowsAny<ArgumentException>(() => new TextTable(options));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEXT-TABLE-OPTIONS", "coherent-options-construct")]
    public void TextTable_AcceptsCoherentOptions()
    {
        var options = new TextTableOptions { Columns = ["name"], Out = TextWriter.Null };

        var table = new TextTable(options);

        Assert.Same(options, table.Options);
    }

    static OutboxConsumeOptions ValidOutbox(InvalidOutboxOption? invalid) => new()
    {
        ConsumerId = invalid == InvalidOutboxOption.ConsumerId ? Guid.Empty : Guid.NewGuid(),
        ConsumerType = invalid == InvalidOutboxOption.ConsumerType ? " " : "Consumer",
        MessageDeliveryLimit = invalid == InvalidOutboxOption.DeliveryLimit ? 0 : 10,
        MessageDeliveryTimeout = invalid == InvalidOutboxOption.DeliveryTimeout ? TimeSpan.Zero : TimeSpan.FromSeconds(30),
    };

    public enum InvalidOutboxOption
    {
        ConsumerId,
        ConsumerType,
        DeliveryLimit,
        DeliveryTimeout,
    }

    public enum InvalidTableOption
    {
        Columns,
        ColumnName,
        Output,
        Alignment,
    }
}
