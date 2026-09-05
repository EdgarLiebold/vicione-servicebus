using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides extension methods for bus factory.
/// </summary>
public static class BusFactoryExtensions
{
    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    /// <param name="busConfiguration">The bus configuration value.</param>
    /// <param name="dependencies">The dependencies value.</param>
    /// <returns>The result of the operation.</returns>
    public static IBusControl Build(this IBusFactory factory, IBusConfiguration busConfiguration, IEnumerable<ISpecification> dependencies)
    {
        return Build(factory, busConfiguration, factory.Validate()
            .Concat(dependencies.SelectMany(x => x.Validate())));
    }

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    /// <param name="busConfiguration">The bus configuration value.</param>
    /// <returns>The result of the operation.</returns>
    public static IBusControl Build(this IBusFactory factory, IBusConfiguration busConfiguration)
    {
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

            var bus = new ViciOneServiceBusBus(host, busConfiguration.BusObservers, busReceiveEndpointConfiguration);

            busConfiguration.BusObservers.PostCreate(bus);

            return bus;
        }
        catch (Exception ex)
        {
            busConfiguration.BusObservers.CreateFaulted(ex);

            throw new ConfigurationException(result, global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Bus Factory Extensions", "unknown", "An exception occurred during bus creation", "Correct the named configuration before starting the host"), ex);
        }
    }
}
