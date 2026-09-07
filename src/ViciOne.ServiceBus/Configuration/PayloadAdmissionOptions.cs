using System;

using Microsoft.Extensions.Options;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Mutable startup options frozen into one bus-owned payload-admission policy.</summary>
/// <typeparam name="TBus">The bus to which the thresholds apply.</typeparam>
public sealed class PayloadAdmissionOptions<TBus>
    where TBus : class, IBus
{
    /// <summary>Gets or sets the optional serialized-body size above which a warning is recorded.</summary>
    public int? WarningBodyBytes { get; set; }

    /// <summary>Gets or sets the optional serialized-body size above which MessageData offload is required.</summary>
    public int? MessageDataOffloadThresholdBytes { get; set; }

    /// <summary>Gets or sets the inclusive maximum serialized application-body size.</summary>
    public int? MaximumSerializedBodyBytes { get; set; }

    /// <summary>Gets or sets the inclusive maximum size of the final serialized transport envelope.</summary>
    public int? MaximumTransportEnvelopeBytes { get; set; }

    internal PayloadAdmissionPolicy Freeze()
        => new PayloadAdmissionPolicy
        {
            WarningBodyBytes = WarningBodyBytes,
            MessageDataOffloadThresholdBytes = MessageDataOffloadThresholdBytes,
            MaximumSerializedBodyBytes = MaximumSerializedBodyBytes.GetValueOrDefault(),
            MaximumTransportEnvelopeBytes = MaximumTransportEnvelopeBytes.GetValueOrDefault(),
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
