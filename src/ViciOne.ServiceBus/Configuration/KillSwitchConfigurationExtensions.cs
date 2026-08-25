#nullable enable
namespace ViciOne.ServiceBus;

using System;
using System.Collections.Generic;
using Configuration;
using Transports.Components;


public static class KillSwitchConfigurationExtensions
{
    /// <summary>
    /// Monitors every receive endpoint and temporarily pauses delivery when the configured matching-failure ratio is reached.
    /// Configuration is captured once and shared as an immutable snapshot by the endpoint-specific runtime instances.
    /// </summary>
    public static void UseKillSwitch(
        this IBusFactoryConfigurator configurator,
        Action<KillSwitchOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var options = new KillSwitchOptions();
        configure?.Invoke(options);
        KillSwitchSettings settings = options.CreateSettings();

        configurator.ConnectEndpointConfigurationObserver(new EndpointConfigurationObserver(settings));
        configurator.AddPipeSpecification(new KillSwitchSettingsSpecification(settings));
    }

    /// <summary>
    /// Monitors one receive endpoint and temporarily pauses delivery when the configured matching-failure ratio is reached.
    /// Configuration is captured before the runtime observer is installed.
    /// </summary>
    public static void UseKillSwitch(
        this IReceiveEndpointConfigurator configurator,
        Action<KillSwitchOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var options = new KillSwitchOptions();
        configure?.Invoke(options);
        KillSwitchSettings settings = options.CreateSettings();

        var killSwitch = new KillSwitch(settings);
        configurator.ConnectReceiveEndpointObserver(killSwitch);
        configurator.ConnectActivityObserver(killSwitch);
        configurator.AddPipeSpecification(new KillSwitchSettingsSpecification(settings));
    }


    private sealed class EndpointConfigurationObserver(KillSwitchSettings settings) :
        IEndpointConfigurationObserver
    {
        public void EndpointConfigured<T>(T configurator)
            where T : IReceiveEndpointConfigurator
        {
            var killSwitch = new KillSwitch(settings);
            configurator.ConnectReceiveEndpointObserver(killSwitch);
            configurator.ConnectActivityObserver(killSwitch);
        }
    }


    private sealed class KillSwitchSettingsSpecification(KillSwitchSettings settings) :
        IPipeSpecification<ConsumeContext>
    {
        public void Apply(IPipeBuilder<ConsumeContext> builder)
        {
        }

        public IEnumerable<ValidationResult> Validate() => settings.Validate();
    }
}
