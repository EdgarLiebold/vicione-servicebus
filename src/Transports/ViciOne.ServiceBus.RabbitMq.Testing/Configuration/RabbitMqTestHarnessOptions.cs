using System;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace ViciOne.ServiceBus.RabbitMq.Testing;

/// <summary>Controls how the hosted RabbitMQ test harness prepares its virtual host.</summary>
public sealed class RabbitMqTestHarnessOptions
{
    /// <summary>
    /// Creates a missing non-root virtual host through the RabbitMQ management API before tests start.
    /// Connection and management settings come from <see cref="RabbitMqTransportOptions" />.
    /// </summary>
    public bool CreateVirtualHostIfNotExists { get; set; }

    /// <summary>Deletes all non-system exchanges and queues from the virtual host before the test host starts.</summary>
    public bool CleanVirtualHost { get; set; }

    /// <summary>Allows <see cref="CleanVirtualHost" /> to delete entities from the root virtual host.</summary>
    public bool ForceCleanRootVirtualHost { get; set; }

    /// <summary>
    /// Gets or sets an optional asynchronous callback invoked after creation and cleanup to configure the virtual host through a broker channel.
    /// </summary>
    public Func<IChannel, Task>? ConfigureVirtualHostCallback { get; set; }
}
