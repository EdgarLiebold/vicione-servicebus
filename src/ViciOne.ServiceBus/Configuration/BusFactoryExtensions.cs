using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds standalone bus runtimes from validated transport configurations.</summary>
public static class BusFactoryExtensions
{
    /// <summary>Builds a bus after validating both its factory and the supplied dependent specifications.</summary>
    /// <param name="factory">The transport factory that creates the bus endpoint.</param>
    /// <param name="busConfiguration">The host, routing, serialization, and observer configuration for the bus.</param>
    /// <param name="dependencies">Additional specifications whose validation must succeed before construction.</param>
    /// <returns>The constructed bus control.</returns>
    public static IBusControl Build(this IBusFactory factory, IBusConfiguration busConfiguration, IEnumerable<ISpecification> dependencies)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(busConfiguration);
        ArgumentNullException.ThrowIfNull(dependencies);

        return Build(factory, busConfiguration, factory.Validate()
            .Concat(dependencies.SelectMany(x => x.Validate())));
    }

    /// <summary>Builds a bus after validating its transport factory.</summary>
    /// <param name="factory">The transport factory that creates the bus endpoint.</param>
    /// <param name="busConfiguration">The host, routing, serialization, and observer configuration for the bus.</param>
    /// <returns>The constructed bus control.</returns>
    public static IBusControl Build(this IBusFactory factory, IBusConfiguration busConfiguration)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(busConfiguration);

        return Build(factory, busConfiguration, factory.Validate());
    }

    static IBusControl Build(IBusFactory factory, IBusConfiguration busConfiguration, IEnumerable<ValidationResult> validationResult)
    {
        if (LogContext.Current == null)
            LogContext.ConfigureCurrentLogContext();

        busConfiguration.HostConfiguration.LogContext = LogContext.Current;

        if (busConfiguration.MessageRoutes is not MessageRouteTable messageRoutes)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Bus Factory Extensions", "unknown", "The bus must own a MessageRouteTable instance.", "Correct the named configuration before starting the host"));

        messageRoutes.Freeze();

        IReadOnlyList<ValidationResult> result = validationResult.ThrowIfContainsFailure("The bus configuration is invalid:");

        try
        {
            var busReceiveEndpointConfiguration = factory.CreateBusEndpointConfiguration(x => x.ConfigureConsumeTopology = false);

            var host = busConfiguration.HostConfiguration.Build();

            var bus = new ServiceBusRuntime(host, busConfiguration.BusObservers, busReceiveEndpointConfiguration);

            busConfiguration.BusObservers.PostCreate(bus);

            return bus;
        }
        catch (Exception ex)
        {
            try
            {
                busConfiguration.BusObservers.CreateFaulted(ex);
            }
            catch (Exception observerException)
            {
                LogContext.Warning?.Log(observerException,
                    "Bus creation-fault observation failed without replacing the construction failure");
            }

            throw new ConfigurationException(result, global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Bus Factory Extensions", "unknown", "An exception occurred during bus creation", "Correct the named configuration before starting the host"), ex);
        }
    }
}
