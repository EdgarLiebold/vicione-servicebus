using ViciOne.ServiceBus.AzureServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Routes skipped and faulted deliveries to the native Azure Service Bus dead-letter subqueue.</summary>
public static class ServiceBusReceivePipeConfiguratorExtensions
{
    /// <summary>Routes skipped messages to the entity's native dead-letter subqueue instead of a separate skipped queue.</summary>
    /// <param name="configurator">The receive pipeline to configure.</param>
    public static void ConfigureDeadLetterQueueDeadLetterTransport(this IReceivePipelineConfigurator configurator)
    {
        configurator.ConfigureDeadLetter(x => x.UseFilter(new DeadLetterQueueFilter()));
    }

    /// <summary>Publishes a <see cref="ReceiveFault"/> and dead-letters faulted messages on their source queue or subscription.</summary>
    /// <param name="configurator">The receive pipeline to configure.</param>
    public static void ConfigureDeadLetterQueueErrorTransport(this IReceivePipelineConfigurator configurator)
    {
        configurator.ConfigureError(x => x.UseFilter(new GenerateFaultFilter()));
        configurator.ConfigureError(x => x.UseFilter(new DeadLetterQueueExceptionFilter()));
    }
}
