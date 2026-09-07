using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Serialization;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures mandatory message-size and JSON-depth boundaries for mediator instances.</summary>
public static class MediatorMessageLimitsConfigurationExtensions
{
    /// <summary>Assigns explicit limits to a dependency-injected mediator.</summary>
    /// <param name="configurator">The dependency-injection mediator registration.</param>
    /// <param name="limits">The maximum body size, envelope size, and JSON depth.</param>
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

    /// <summary>Assigns explicit limits to a directly-created mediator.</summary>
    /// <param name="configurator">The directly created mediator configuration.</param>
    /// <param name="limits">The maximum body size, envelope size, and JSON depth.</param>
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
}

internal sealed record MediatorMessageLimitsRegistration(MessageLimits Limits);
