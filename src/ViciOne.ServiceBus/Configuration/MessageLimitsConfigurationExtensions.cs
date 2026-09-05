using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Serialization;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures mandatory message-size and JSON-depth boundaries inside a bus registration block.
/// </summary>
public static class MessageLimitsConfigurationExtensions
{
    internal static bool HasLimits<TBus>(IServiceCollection services)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(services);
        string busKey = BusRegistrationIdentity.GetKey(typeof(TBus));
        return services.Any(descriptor => descriptor.ServiceType == typeof(IMessageLimitsRegistration)
            && descriptor.ImplementationInstance is IMessageLimitsRegistration registration
            && string.Equals(registration.BusKey, busKey, StringComparison.Ordinal));
    }

    /// <summary>
    /// Assigns explicit limits to the default bus.
    /// </summary>
    /// <param name="configurator">The default-bus registration block.</param>
    /// <param name="limits">The complete immutable limits policy.</param>
    /// <returns>The registration block for continued configuration.</returns>
    public static IBusRegistrationConfigurator Limits(
        this IBusRegistrationConfigurator configurator,
        MessageLimits limits)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        Register<IBus>(configurator.Services, limits);
        return configurator;
    }

    /// <summary>
    /// Assigns explicit limits to a typed bus.
    /// </summary>
    /// <typeparam name="TBus">The bus interface that owns the limits.</typeparam>
    /// <param name="configurator">The typed-bus registration block.</param>
    /// <param name="limits">The complete immutable limits policy.</param>
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

    /// <summary>
    /// Assigns explicit limits to a dependency-injected mediator.
    /// </summary>
    /// <param name="configurator">The mediator registration block.</param>
    /// <param name="limits">The complete immutable limits policy.</param>
    /// <returns>The registration block for continued configuration.</returns>
    public static IMediatorRegistrationConfigurator Limits(
        this IMediatorRegistrationConfigurator configurator,
        MessageLimits limits)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(limits);
        limits.Validate("mediator");

        if (configurator.Services.Any(x => x.ServiceType == typeof(MediatorMessageLimitsRegistration)))
        {
            throw new ConfigurationException(
                "Message limits for bus 'mediator': Limits is already declared. Configure exactly one Limits policy inside the mediator block.");
        }

        configurator.Services.AddSingleton(new MediatorMessageLimitsRegistration(limits));
        return configurator;
    }

    /// <summary>
    /// Assigns explicit limits to a directly-created mediator.
    /// </summary>
    /// <param name="configurator">The mediator configuration.</param>
    /// <param name="limits">The complete immutable limits policy.</param>
    public static void Limits(this IMediatorConfigurator configurator, MessageLimits limits)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(limits);
        limits.Validate("mediator");

        if (configurator is not IMessageLimitsConfigurator target)
        {
            throw new ConfigurationException(
                "Message limits for bus 'mediator': The selected mediator cannot enforce receive limits. Use the built-in mediator configurator.");
        }

        target.SetMessageLimits(limits);
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

internal sealed record MediatorMessageLimitsRegistration(MessageLimits Limits);
