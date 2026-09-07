using System;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Diagnostics.Telemetry;
using ViciOne.ServiceBus.Serialization;


namespace Microsoft.Extensions.DependencyInjection;
/// <summary>Registers one validated payload-admission policy for a typed bus.</summary>
public static class PayloadAdmissionServiceCollectionExtensions
{
    /// <summary>
    /// Adds bounded application-body and final-envelope admission to <typeparamref name="TBus"/>.
    /// Both hard limits are required and validated during host startup.
    /// </summary>
    /// <typeparam name="TBus">The bus type.</typeparam>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The service collection produced by the operation.</returns>
    public static IServiceCollection AddViciOnePayloadAdmission<TBus>(
        this IServiceCollection services,
        Action<PayloadAdmissionOptions<TBus>>? configure = null)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddMetrics();
        ViciOne.ServiceBus.Configuration.BusCompositionRegistrations.AddFeature<TBus>(services, "Payload admission");

        OptionsBuilder<PayloadAdmissionOptions<TBus>> options = services.AddOptions<PayloadAdmissionOptions<TBus>>();
        if (configure is not null)
            options.Configure(configure);

        options.ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<PayloadAdmissionOptions<TBus>>, PayloadAdmissionOptionsValidator<TBus>>());

        services.TryAddSingleton<ServiceBusInstrumentation<TBus>>();
        services.TryAddSingleton<PayloadAdmissionPolicyProvider<TBus>>();
        services.TryAddSingleton(provider => new PayloadAdmissionEvaluator<TBus>(
            provider.GetRequiredService<PayloadAdmissionPolicyProvider<TBus>>()));
        services.TryAddSingleton<IPayloadAdmissionEvaluator<TBus>, InstrumentedPayloadAdmissionEvaluator<TBus>>();
        services.TryAddSingleton<PayloadAdmissionRuntime<TBus>>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPayloadAdmissionRuntimeRegistration, PayloadAdmissionRuntimeRegistration<TBus>>());

        return services;
    }

}

internal sealed class PayloadAdmissionOptionsValidator<TBus> : IValidateOptions<PayloadAdmissionOptions<TBus>>
    where TBus : class, IBus
{
    public ValidateOptionsResult Validate(string? name, PayloadAdmissionOptions<TBus> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        string bus = ViciOne.ServiceBus.Configuration.BusRegistrationIdentity.GetKey(typeof(TBus));
        List<string> failures = [];
        if (options.MaximumSerializedBodyBytes is null)
        {
            failures.Add(
                $"Payload admission for bus '{bus}': MaximumSerializedBodyBytes is not declared. Call bus.Limits(...) with an explicit MaxBodyBytes value.");
        }
        if (options.MaximumTransportEnvelopeBytes is null)
        {
            failures.Add(
                $"Payload admission for bus '{bus}': MaximumTransportEnvelopeBytes is not declared. Call bus.Limits(...) with an explicit MaxEnvelopeBytes value.");
        }

        if (failures.Count == 0)
        {
            try
            {
                _ = options.Freeze();
            }
            catch (ArgumentException exception)
            {
                failures.Add(
                    $"Payload admission for bus '{bus}': {exception.Message} Correct the named threshold before starting the host.");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
