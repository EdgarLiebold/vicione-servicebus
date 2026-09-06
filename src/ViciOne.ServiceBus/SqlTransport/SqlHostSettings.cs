using System;
using System.Data;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.SqlTransport.Configuration;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Settings to configure a DbTransport host explicitly without requiring the fluent interface
/// </summary>
public interface SqlHostSettings :
    ISpecification
{
    /// <summary>
    /// Gets the host address value.
    /// </summary>
    Uri HostAddress { get; }

    /// <summary>
    /// Gets the connection tag value.
    /// </summary>
    string? ConnectionTag { get; }

    /// <summary>
    /// Gets the virtual host value.
    /// </summary>
    string? VirtualHost { get; }
    /// <summary>
    /// Gets the area value.
    /// </summary>
    string? Area { get; }

    /// <summary>
    /// Gets the isolation level value.
    /// </summary>
    IsolationLevel IsolationLevel { get; }

    /// <summary>
    /// Gets the connection limit value.
    /// </summary>
    int ConnectionLimit { get; }

    /// <summary>
    /// Gets the maintenance enabled value.
    /// </summary>
    bool MaintenanceEnabled { get; }
    /// <summary>
    /// Gets the maintenance interval value.
    /// </summary>
    TimeSpan MaintenanceInterval { get; }
    /// <summary>
    /// Gets the queue cleanup interval value.
    /// </summary>
    TimeSpan QueueCleanupInterval { get; }
    /// <summary>
    /// Gets the maintenance batch size value.
    /// </summary>
    int MaintenanceBatchSize { get; }

    /// <summary>
    /// Creates connection context factory.
    /// </summary>
    /// <param name="configuration">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    ConnectionContextFactory CreateConnectionContextFactory(ISqlHostConfiguration configuration);

}
