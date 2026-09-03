#nullable enable

namespace Microsoft.Extensions.DependencyInjection;

using System;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Diagnostics;
using ViciOne.ServiceBus.Serialization;

/// <summary>Registers one validated payload-admission policy for a typed bus.</summary>
public static class PayloadAdmissionServiceCollectionExtensions
{
    /// <summary>
    /// Adds bounded application-body and final-envelope admission to <typeparamref name="TBus"/>.
    /// Both hard limits are required and validated during host startup.
    /// </summary>
    public static IServiceCollection AddViciOnePayloadAdmission<TBus>(
        this IServiceCollection services,
        Action<PayloadAdmissionOptions<TBus>>? configure = null)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddMetrics();

        OptionsBuilder<PayloadAdmissionOptions<TBus>> options = services.AddOptions<PayloadAdmissionOptions<TBus>>();
        if (configure is not null)
            options.Configure(configure);

        options
            .Validate(IsValidRuntimePolicy, PayloadAdmissionOptions<TBus>.ValidationFailureMessage)
            .ValidateOnStart();

        services.TryAddSingleton<V5ServiceBusInstrumentation<TBus>>();
        services.TryAddSingleton<PayloadAdmissionPolicyProvider<TBus>>();
        services.TryAddSingleton(provider => new PayloadAdmissionEvaluator<TBus>(
            provider.GetRequiredService<PayloadAdmissionPolicyProvider<TBus>>()));
        services.TryAddSingleton<IPayloadAdmissionEvaluator<TBus>, InstrumentedPayloadAdmissionEvaluator<TBus>>();
        services.TryAddSingleton<PayloadAdmissionRuntime<TBus>>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPayloadAdmissionRuntimeRegistration, PayloadAdmissionRuntimeRegistration<TBus>>());

        return services;
    }

    static bool IsValidRuntimePolicy<TBus>(PayloadAdmissionOptions<TBus> options)
        where TBus : class, IBus
    {
        try
        {
            PayloadAdmissionPolicy policy = options.Freeze();
            return policy.MaximumSerializedBodyBytes.HasValue
                && policy.MaximumTransportEnvelopeBytes.HasValue;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
