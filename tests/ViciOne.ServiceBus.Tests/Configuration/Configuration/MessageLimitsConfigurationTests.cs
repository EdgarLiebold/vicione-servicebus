using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration.Configuration;

public sealed class MessageLimitsConfigurationTests
{
    [Theory]
    [InlineData(InvalidLimit.Body, "MaxBodyBytes")]
    [InlineData(InvalidLimit.Envelope, "MaxEnvelopeBytes")]
    [InlineData(InvalidLimit.EnvelopeBelowBody, "MaxEnvelopeBytes")]
    [InlineData(InvalidLimit.JsonDepth, "MaxJsonDepth")]
    [InlineData(InvalidLimit.Warning, "WarnAboveBytes")]
    [InlineData(InvalidLimit.WarningAboveBody, "WarnAboveBytes")]
    [InlineData(InvalidLimit.Offload, "OffloadToMessageDataAboveBytes")]
    [InlineData(InvalidLimit.OffloadAboveBody, "OffloadToMessageDataAboveBytes")]
    [RequirementCoverage("REQ-VSB-MESSAGE-LIMITS", "every-static-invariant-has-causal-diagnostic")]
    public void Limits_RejectEveryInvalidInvariantWithBusPropertyAndFix(
        InvalidLimit invalid,
        string property)
    {
        MessageLimits limits = CreateInvalid(invalid);
        var services = new ServiceCollection();

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            services.AddViciOneServiceBus(bus => bus.Limits(limits)));

        Assert.StartsWith("Message limits for bus 'default': ", exception.Message, StringComparison.Ordinal);
        Assert.Contains(property, exception.Message, StringComparison.Ordinal);
        Assert.EndsWith(".", exception.Message, StringComparison.Ordinal);
        Assert.True(
            exception.Message.Contains("Set ", StringComparison.Ordinal)
            || exception.Message.Contains("Remove it or set ", StringComparison.Ordinal),
            exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-LIMITS", "missing-declaration-fails-before-bus-materialization")]
    public void BusWithoutLimits_FailsWithTheExactActionableMessage()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTextWriterLogger(TextWriter.Null)
            .AddViciOneServiceBus(bus => bus.UsingInMemory(static (_, _) => { }))
            .BuildServiceProvider(validateScopes: true);

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            provider.GetRequiredService<IBus>());

        Assert.Equal(
            "Message limits for bus 'default': MaxBodyBytes is not declared. Call bus.Limits(...) with explicit byte limits.",
            exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-LIMITS", "single-owner-and-payload-admission-binding")]
    public void ExplicitLimits_CreateOneBusOwnedPayloadPolicy()
    {
        var limits = new MessageLimits
        {
            MaxBodyBytes = 512 * 1024,
            MaxEnvelopeBytes = 1024 * 1024,
            MaxJsonDepth = 29,
            WarnAboveBytes = 128 * 1024,
            OffloadToMessageDataAboveBytes = 256 * 1024,
        };
        using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTextWriterLogger(TextWriter.Null)
            .AddViciOneServiceBus(bus =>
            {
                bus.Limits(limits);
                bus.UsingInMemory(static (_, _) => { });
            })
            .BuildServiceProvider(validateScopes: true);

        _ = provider.GetRequiredService<IBus>();
        PayloadAdmissionOptions<IBus> options = provider
            .GetRequiredService<IOptions<PayloadAdmissionOptions<IBus>>>()
            .Value;

        Assert.Equal(limits.WarnAboveBytes, options.WarningBodyBytes);
        Assert.Equal(limits.OffloadToMessageDataAboveBytes, options.MessageDataOffloadThresholdBytes);
        Assert.Equal(limits.MaxBodyBytes, options.MaximumSerializedBodyBytes);
        Assert.Equal(limits.MaxEnvelopeBytes, options.MaximumTransportEnvelopeBytes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-LIMITS", "duplicate-owner-rejected")]
    public void SameBus_RejectsASecondLimitsOwner()
    {
        var services = new ServiceCollection();

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            services.AddViciOneServiceBus(bus =>
            {
                bus.Limits(MessageLimits.Conservative);
                bus.Limits(MessageLimits.Conservative);
            }));

        Assert.Equal(
            "Message limits for bus 'default': Limits is already declared. Configure exactly one Limits policy inside the bus block.",
            exception.Message);
    }

    private static MessageLimits CreateInvalid(InvalidLimit invalid) => invalid switch
    {
        InvalidLimit.Body => Valid() with { MaxBodyBytes = 0 },
        InvalidLimit.Envelope => Valid() with { MaxEnvelopeBytes = 0 },
        InvalidLimit.EnvelopeBelowBody => Valid() with { MaxEnvelopeBytes = 99 },
        InvalidLimit.JsonDepth => Valid() with { MaxJsonDepth = 0 },
        InvalidLimit.Warning => Valid() with { WarnAboveBytes = 0 },
        InvalidLimit.WarningAboveBody => Valid() with { WarnAboveBytes = 101 },
        InvalidLimit.Offload => Valid() with { OffloadToMessageDataAboveBytes = 0 },
        InvalidLimit.OffloadAboveBody => Valid() with { OffloadToMessageDataAboveBytes = 101 },
        _ => throw new ArgumentOutOfRangeException(nameof(invalid), invalid, "Unknown invalid limit."),
    };

    private static MessageLimits Valid() => new()
    {
        MaxBodyBytes = 100,
        MaxEnvelopeBytes = 200,
        MaxJsonDepth = 32,
    };

    public enum InvalidLimit
    {
        Body,
        Envelope,
        EnvelopeBelowBody,
        JsonDepth,
        Warning,
        WarningAboveBody,
        Offload,
        OffloadAboveBody,
    }
}
