using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Serialization;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures mandatory message-size and JSON-depth boundaries for registered and factory-created buses.</summary>
public static class MessageLimitsConfigurationExtensions
{
    /// <summary>Assigns explicit message limits to a bus created through a transport factory.</summary>
    /// <typeparam name="TConfigurator">The transport-specific factory configurator type.</typeparam>
    /// <param name="configurator">The bus factory configurator to update.</param>
    /// <param name="limits">The body, envelope, and JSON-depth limits owned by this bus.</param>
    /// <returns>The factory configurator for continued configuration.</returns>
    public static TConfigurator Limits<TConfigurator>(
        this TConfigurator configurator,
        MessageLimits limits)
        where TConfigurator : IBusFactoryConfigurator
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(limits);

        if (configurator is not BusFactoryConfigurator factory)
        {
            throw new ConfigurationException(
                "Message limits for bus 'unknown': The selected factory cannot enforce message limits. Choose a factory with message-limit support.");
        }

        factory.ConfigureDirectMessageLimits(limits);
        return configurator;
    }

    internal static bool HasLimits<TBus>(IServiceCollection services)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(services);
        string busKey = BusRegistrationIdentity.GetKey(typeof(TBus));
        return services.Any(descriptor => descriptor.ServiceType == typeof(IMessageLimitsRegistration)
            && descriptor.ImplementationInstance is IMessageLimitsRegistration registration
            && string.Equals(registration.BusKey, busKey, StringComparison.Ordinal));
    }

    /// <summary>Assigns explicit limits to the default bus.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="limits">The limits.</param>
    /// <returns>The registration block for continued configuration.</returns>
    public static IBusRegistrationConfigurator Limits(
        this IBusRegistrationConfigurator configurator,
        MessageLimits limits)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        Register<IBus>(configurator.Services, limits);
        return configurator;
    }

    /// <summary>Assigns explicit limits to a typed bus.</summary>
    /// <typeparam name="TBus">The bus interface that owns the limits.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="limits">The limits.</param>
    /// <returns>The registration block for continued configuration.</returns>
    public static IBusRegistrationConfigurator<TBus> Limits<TBus>(
        this IBusRegistrationConfigurator<TBus> configurator,
        MessageLimits limits)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(configurator);
        Register<TBus>(configurator.Services, limits);
        return configurator;
    }

    static void Register<TBus>(IServiceCollection services, MessageLimits limits)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(limits);
        string busKey = BusRegistrationIdentity.GetKey(typeof(TBus));
        limits.Validate(busKey);

        if (HasLimits<TBus>(services))
        {
            throw new ConfigurationException(
                $"Message limits for bus '{busKey}': Limits is already declared. Configure exactly one Limits policy inside the bus block.");
        }

        services.AddSingleton<IMessageLimitsRegistration>(new MessageLimitsRegistration<TBus>(limits));
        services.AddViciOnePayloadAdmission<TBus>(options =>
        {
            options.WarningBodyBytes = limits.WarnAboveBytes;
            options.MessageDataOffloadThresholdBytes = limits.OffloadToMessageDataAboveBytes;
            options.MaximumSerializedBodyBytes = limits.MaxBodyBytes;
            options.MaximumTransportEnvelopeBytes = limits.MaxEnvelopeBytes;
        });
    }
}

internal interface IMessageLimitsRegistration
{
    string BusKey { get; }

    MessageLimits Limits { get; }
}

internal sealed class MessageLimitsRegistration<TBus> : IMessageLimitsRegistration
    where TBus : class, IBus
{
    public MessageLimitsRegistration(MessageLimits limits)
    {
        Limits = limits ?? throw new ArgumentNullException(nameof(limits));
    }

    public string BusKey { get; } = BusRegistrationIdentity.GetKey(typeof(TBus));

    public MessageLimits Limits { get; }
}

internal interface IMessageLimitsHostConfiguration
{
    MessageLimits? MessageLimits { get; }

    void SetMessageLimits(MessageLimits limits);
}

internal interface IMessageLimitsConfigurator
{
    void SetMessageLimits(MessageLimits limits);
}
