using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class PayloadAdmissionEvaluatorTests
{
    [Theory]
    [InlineData(99, false)]
    [InlineData(100, false)]
    [InlineData(101, true)]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-BODY", "maximum-minus-one-exact-plus-one")]
    public void SerializedBody_HardMaximumUsesAnInclusiveExactByteBoundary(int bytes, bool rejected)
    {
        var evaluator = Evaluator(maximumBodyBytes: 100, maximumEnvelopeBytes: 200);

        if (!rejected)
        {
            PayloadAdmissionResult result = evaluator.EvaluateSerializedBody(new byte[bytes], messageDataOffloadObserved: false);
            Assert.Equal(PayloadAdmissionDisposition.Inline, result.Disposition);
            Assert.Equal(bytes, result.SerializedBodyBytes);
            return;
        }

        PayloadAdmissionException exception = Assert.Throws<PayloadAdmissionException>(
            () => evaluator.EvaluateSerializedBody(new byte[bytes], messageDataOffloadObserved: false));
        Assert.Equal(PayloadAdmissionStage.SerializedBody, exception.Stage);
        Assert.Equal(bytes, exception.ActualBytes);
        Assert.Equal(100, exception.ConfiguredLimitBytes);
    }

    [Theory]
    [InlineData(49, false, PayloadAdmissionDisposition.Inline)]
    [InlineData(50, false, PayloadAdmissionDisposition.Inline)]
    [InlineData(51, true, PayloadAdmissionDisposition.OffloadToMessageData)]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-MESSAGE-DATA", "threshold-minus-one-exact-plus-one")]
    public void MessageDataThreshold_IsInclusiveAndRequiresObservedOwnerUse(
        int bytes,
        bool ownerUseObserved,
        PayloadAdmissionDisposition expected)
    {
        var evaluator = Evaluator(
            maximumBodyBytes: 100,
            maximumEnvelopeBytes: 200,
            messageDataThresholdBytes: 50);

        PayloadAdmissionResult result = evaluator.EvaluateSerializedBody(new byte[bytes], ownerUseObserved);

        Assert.Equal(expected, result.Disposition);
        Assert.Equal(bytes, result.SerializedBodyBytes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-MESSAGE-DATA", "missing-owner-fails-loud")]
    public void BodyAboveMessageDataThreshold_WithoutOwnerEvidenceFailsLoudly()
    {
        var evaluator = Evaluator(
            maximumBodyBytes: 100,
            maximumEnvelopeBytes: 200,
            messageDataThresholdBytes: 50);

        PayloadAdmissionException exception = Assert.Throws<PayloadAdmissionException>(
            () => evaluator.EvaluateSerializedBody(new byte[51], messageDataOffloadObserved: false));

        Assert.Equal(PayloadAdmissionStage.MessageData, exception.Stage);
        Assert.Equal(51, exception.ActualBytes);
        Assert.Equal(50, exception.ConfiguredLimitBytes);
        Assert.Contains("no MessageData owner", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-WARNING", "observation-only-threshold")]
    public void WarningThreshold_ReportsWithoutChangingTheAdmissionDecision()
    {
        var evaluator = new PayloadAdmissionEvaluator<IBus>(new PayloadAdmissionPolicy
        {
            WarningBodyBytes = 25,
            MaximumSerializedBodyBytes = 100,
            MaximumTransportEnvelopeBytes = 200,
        });

        PayloadAdmissionResult exact = evaluator.EvaluateSerializedBody(new byte[25], messageDataOffloadObserved: false);
        PayloadAdmissionResult exceeded = evaluator.EvaluateSerializedBody(new byte[26], messageDataOffloadObserved: false);

        Assert.False(exact.WarningThresholdExceeded);
        Assert.True(exceeded.WarningThresholdExceeded);
        Assert.Equal(PayloadAdmissionDisposition.Inline, exceeded.Disposition);
    }

    [Theory]
    [InlineData(199, false)]
    [InlineData(200, false)]
    [InlineData(201, true)]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-ENVELOPE", "maximum-minus-one-exact-plus-one")]
    public void TransportEnvelope_HardMaximumUsesAnInclusiveExactByteBoundary(int bytes, bool rejected)
    {
        var evaluator = Evaluator(maximumBodyBytes: 100, maximumEnvelopeBytes: 200);

        if (!rejected)
        {
            evaluator.ValidateTransportEnvelope(new byte[bytes]);
            return;
        }

        PayloadAdmissionException exception = Assert.Throws<PayloadAdmissionException>(
            () => evaluator.ValidateTransportEnvelope(new byte[bytes]));
        Assert.Equal(PayloadAdmissionStage.TransportEnvelope, exception.Stage);
        Assert.Equal(bytes, exception.ActualBytes);
        Assert.Equal(200, exception.ConfiguredLimitBytes);
    }

    [Theory]
    [InlineData(PayloadAdmissionStage.SerializedBody)]
    [InlineData(PayloadAdmissionStage.TransportEnvelope)]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-BOUNDED-WRITER", "hostile-growth-rejected-before-overrun")]
    public void BoundedWriter_RejectsAHostileGrowthRequestBeforeCommittingBytes(PayloadAdmissionStage stage)
    {
        var evaluator = Evaluator(maximumBodyBytes: 4, maximumEnvelopeBytes: 4);
        IPayloadSerializationBuffer buffer = stage == PayloadAdmissionStage.SerializedBody
            ? evaluator.CreateSerializedBodyBuffer()
            : evaluator.CreateTransportEnvelopeBuffer();

        PayloadAdmissionException exception = Assert.Throws<PayloadAdmissionException>(() => buffer.GetMemory(5));

        Assert.Equal(stage, exception.Stage);
        Assert.Equal(5, exception.ActualBytes);
        Assert.Equal(4, exception.ConfiguredLimitBytes);
        Assert.Equal(0, buffer.WrittenCount);
        Assert.Empty(buffer.WrittenMemory.ToArray());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-BOUNDED-WRITER", "exact-capacity-and-post-capacity-rejection")]
    public void BoundedWriter_AcceptsExactlyItsMaximumThenRejectsTheNextByte()
    {
        var evaluator = Evaluator(maximumBodyBytes: 4, maximumEnvelopeBytes: 8);
        IPayloadSerializationBuffer buffer = evaluator.CreateSerializedBodyBuffer();
        byte[] expected = [3, 1, 4, 1];

        expected.CopyTo(buffer.GetSpan(expected.Length));
        buffer.Advance(expected.Length);

        Assert.Equal(expected.Length, buffer.WrittenCount);
        Assert.Equal(expected, buffer.WrittenMemory.ToArray());
        PayloadAdmissionException exception = Assert.Throws<PayloadAdmissionException>(() => buffer.GetSpan());
        Assert.Equal(5, exception.ActualBytes);
        Assert.Equal(expected, buffer.WrittenMemory.ToArray());
    }

    [Theory]
    [InlineData(0, null, 10, 10, "WarningBodyBytes")]
    [InlineData(-1, null, 10, 10, "WarningBodyBytes")]
    [InlineData(null, 0, 10, 10, "MessageDataOffloadThresholdBytes")]
    [InlineData(null, -1, 10, 10, "MessageDataOffloadThresholdBytes")]
    [InlineData(null, null, 0, 10, "MaximumSerializedBodyBytes")]
    [InlineData(null, null, -1, 10, "MaximumSerializedBodyBytes")]
    [InlineData(null, null, 10, 0, "MaximumTransportEnvelopeBytes")]
    [InlineData(null, null, 10, -1, "MaximumTransportEnvelopeBytes")]
    [InlineData(11, null, 10, 10, "WarningBodyBytes")]
    [InlineData(null, 11, 10, 10, "MessageDataOffloadThresholdBytes")]
    [InlineData(null, null, 11, 10, "MaximumTransportEnvelopeBytes")]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-POLICY", "invalid-values-and-ordering")]
    public void Policy_RejectsNonPositiveAndIncoherentThresholds(
        int? warning,
        int? messageData,
        int maximumBody,
        int maximumEnvelope,
        string expectedParameterName)
    {
        var policy = new PayloadAdmissionPolicy
        {
            WarningBodyBytes = warning,
            MessageDataOffloadThresholdBytes = messageData,
            MaximumSerializedBodyBytes = maximumBody,
            MaximumTransportEnvelopeBytes = maximumEnvelope,
        };

        ArgumentException exception = Assert.ThrowsAny<ArgumentException>(() => policy.Validate());

        Assert.Equal(expectedParameterName, exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-DI", "startup-validation-and-bus-constraint")]
    public void Registration_ValidatesRequiredHardLimitsWhenOptionsAreMaterialized()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddViciOnePayloadAdmission<IBus>(options => options.MaximumSerializedBodyBytes = 1024)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<PayloadAdmissionOptions<IBus>>>().Value);

        Assert.Contains(nameof(PayloadAdmissionOptions<IBus>.MaximumTransportEnvelopeBytes),
            string.Join(" | ", exception.Failures), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0, null, 10, 10, "WarningBodyBytes")]
    [InlineData(-1, null, 10, 10, "WarningBodyBytes")]
    [InlineData(null, 0, 10, 10, "MessageDataOffloadThresholdBytes")]
    [InlineData(null, -1, 10, 10, "MessageDataOffloadThresholdBytes")]
    [InlineData(null, null, 0, 10, "MaximumSerializedBodyBytes")]
    [InlineData(null, null, -1, 10, "MaximumSerializedBodyBytes")]
    [InlineData(null, null, 10, 0, "MaximumTransportEnvelopeBytes")]
    [InlineData(null, null, 10, -1, "MaximumTransportEnvelopeBytes")]
    [InlineData(11, null, 10, 10, "WarningBodyBytes")]
    [InlineData(null, 11, 10, 10, "MessageDataOffloadThresholdBytes")]
    [InlineData(null, null, 11, 10, "MaximumTransportEnvelopeBytes")]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-DI", "every-invalid-threshold-rejected-at-options-boundary")]
    public void Registration_RejectsEveryInvalidThresholdWhenOptionsAreMaterialized(
        int? warning,
        int? messageData,
        int maximumBody,
        int maximumEnvelope,
        string expectedPropertyName)
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddViciOnePayloadAdmission<IBus>(options =>
            {
                options.WarningBodyBytes = warning;
                options.MessageDataOffloadThresholdBytes = messageData;
                options.MaximumSerializedBodyBytes = maximumBody;
                options.MaximumTransportEnvelopeBytes = maximumEnvelope;
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<PayloadAdmissionOptions<IBus>>>().Value);

        Assert.Contains(expectedPropertyName, string.Join(" | ", exception.Failures), StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-FAILURE-CONTRACT", "unknown-stage-rejected")]
    public void PayloadAdmissionException_RejectsAnUnknownStage()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PayloadAdmissionException((PayloadAdmissionStage)int.MaxValue, 1, 1, "Rejected."));

        Assert.Equal("stage", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-FAILURE-CONTRACT", "negative-actual-size-rejected")]
    public void PayloadAdmissionException_RejectsANegativeActualSize()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PayloadAdmissionException(PayloadAdmissionStage.SerializedBody, -1, 1, "Rejected."));

        Assert.Equal("actualBytes", exception.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-FAILURE-CONTRACT", "non-positive-limit-rejected")]
    public void PayloadAdmissionException_RejectsANonPositiveConfiguredLimit(long configuredLimitBytes)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PayloadAdmissionException(PayloadAdmissionStage.SerializedBody, 1, configuredLimitBytes, "Rejected."));

        Assert.Equal("configuredLimitBytes", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-FAILURE-CONTRACT", "actual-size-must-exceed-limit")]
    public void PayloadAdmissionException_RejectsAnActualSizeWithinTheConfiguredLimit(long actualBytes)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PayloadAdmissionException(PayloadAdmissionStage.SerializedBody, actualBytes, 1, "Rejected."));

        Assert.Equal("actualBytes", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-FAILURE-CONTRACT", "missing-description-rejected")]
    public void PayloadAdmissionException_RejectsAMissingDescription(string? message)
    {
        ArgumentException exception = Assert.ThrowsAny<ArgumentException>(() =>
            new PayloadAdmissionException(PayloadAdmissionStage.SerializedBody, 2, 1, message!));

        Assert.Equal("message", exception.ParamName);
    }

    private static PayloadAdmissionEvaluator<IBus> Evaluator(
        int maximumBodyBytes,
        int maximumEnvelopeBytes,
        int? messageDataThresholdBytes = null) =>
        new(new PayloadAdmissionPolicy
        {
            MessageDataOffloadThresholdBytes = messageDataThresholdBytes,
            MaximumSerializedBodyBytes = maximumBodyBytes,
            MaximumTransportEnvelopeBytes = maximumEnvelopeBytes,
        });
}
