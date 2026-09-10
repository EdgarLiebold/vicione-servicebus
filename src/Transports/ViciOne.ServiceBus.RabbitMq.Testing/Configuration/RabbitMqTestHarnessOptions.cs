using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace ViciOne.ServiceBus.RabbitMq.Testing;

/// <summary>Controls RabbitMQ virtual-host preparation performed during test-host startup.</summary>
public sealed class RabbitMqTestHarnessOptions
{
    /// <summary>
    /// Creates a missing non-root virtual host through the RabbitMQ management API before tests start.
    /// Connection and management settings come from <see cref="RabbitMqTransportOptions" />.
    /// </summary>
    public bool CreateVirtualHostIfMissing { get; set; }

    /// <summary>Gets or sets whether startup deletes all non-system exchanges and queues from the virtual host.</summary>
    public bool CleanVirtualHostOnStart { get; set; }

    /// <summary>Gets or sets whether cleanup may delete entities from the root virtual host.</summary>
    public bool AllowRootVirtualHostCleanup { get; set; }

    /// <summary>Gets or sets asynchronous configuration invoked with an open channel after startup cleanup.</summary>
    public Func<IChannel, CancellationToken, Task>? ConfigureVirtualHostAsync { get; set; }
}
