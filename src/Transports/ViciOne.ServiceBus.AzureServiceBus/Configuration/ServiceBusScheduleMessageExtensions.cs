using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Enables native Azure Service Bus delayed delivery on consume pipelines.</summary>
public static class ServiceBusScheduleMessageExtensions
{
    /// <summary>
    /// Adds the consume-pipeline payload that schedules future delivery by setting Azure Service Bus
    /// scheduled enqueue time rather than using an external scheduler.
    /// </summary>
    /// <param name="configurator">The bus factory pipeline to configure.</param>
    public static void ConfigureServiceBusMessageScheduler(this IBusFactoryConfigurator configurator)
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var pipeBuilderConfigurator = new ServiceBusMessageSchedulerSpecification();

        configurator.AddPrePipeSpecification(pipeBuilderConfigurator);
    }
}
