using System;

using Microsoft.Extensions.Options;


namespace ViciOne.ServiceBus.Configuration;
/// <summary>Mutable startup options frozen into one bus-owned payload-admission policy.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
public sealed class PayloadAdmissionOptions<TBus>
    where TBus : class, IBus
{
    /// <summary>Gets or sets the warning body bytes.</summary>
    public int? WarningBodyBytes { get; set; }

    /// <summary>Gets or sets the message data offload threshold bytes.</summary>
    public int? MessageDataOffloadThresholdBytes { get; set; }

    /// <summary>Gets or sets the maximum serialized body bytes.</summary>
    public int? MaximumSerializedBodyBytes { get; set; }

    /// <summary>Gets or sets the maximum transport envelope bytes.</summary>
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
