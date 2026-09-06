using System;

using Microsoft.Extensions.Options;


namespace ViciOne.ServiceBus.Configuration;
/// <summary>Mutable startup options frozen into one bus-owned payload-admission policy.</summary>
public sealed class PayloadAdmissionOptions<TBus>
    where TBus : class, IBus
{
    /// <summary>Gets or sets the observation-only body warning threshold.</summary>
    public int? WarningBodyBytes { get; set; }

    /// <summary>Gets or sets the threshold above which MessageData is required.</summary>
    public int? MessageDataOffloadThresholdBytes { get; set; }

    /// <summary>Gets or sets the hard serialized application-body maximum.</summary>
    public int? MaximumSerializedBodyBytes { get; set; }

    /// <summary>Gets or sets the hard final transport-envelope maximum.</summary>
    public int? MaximumTransportEnvelopeBytes { get; set; }

    internal PayloadAdmissionPolicy Freeze()
        => new PayloadAdmissionPolicy
        {
            WarningBodyBytes = WarningBodyBytes,
            MessageDataOffloadThresholdBytes = MessageDataOffloadThresholdBytes,
            MaximumSerializedBodyBytes = MaximumSerializedBodyBytes,
            MaximumTransportEnvelopeBytes = MaximumTransportEnvelopeBytes,
        }.Validate();
}

internal sealed class PayloadAdmissionPolicyProvider<TBus>
    where TBus : class, IBus
{
    public PayloadAdmissionPolicyProvider(IOptions<PayloadAdmissionOptions<TBus>> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Value = options.Value.Freeze();
    }

    public PayloadAdmissionPolicy Value { get; }
}
